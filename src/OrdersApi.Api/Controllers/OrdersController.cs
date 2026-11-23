using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.Data;
using OrdersApi.Api.Dtos;
using OrdersApi.Api.Hubs;
using OrdersApi.Api.Models;
using OrdersApi.Api.Services;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db, IHubContext<OrderHub> hub) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "An order needs at least one item." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id) && p.IsActive).ToDictionaryAsync(p => p.Id);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return BadRequest(new { error = $"Product {item.ProductId} is not available." });
            }

            if (product.Stock < item.Quantity)
            {
                return BadRequest(new { error = $"Not enough stock for {product.Name} (left: {product.Stock})." });
            }
        }

        var total = OrderCalculator.Total(
            request.Items.Select(i => (products[i.ProductId].Price, i.Quantity)));

        foreach (var item in request.Items)
        {
            products[item.ProductId].Stock -= item.Quantity;
        }

        var order = new Order
        {
            UserId = userId,
            Total = total,
            Items = request.Items
                .Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = products[i.ProductId].Price,
                })
                .ToList(),
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var dto = ToDto(order, products);
        await hub.Clients
            .Groups($"user-{order.UserId}", "admins")
            .SendAsync("orderCreated", dto, HttpContext.RequestAborted);

        return Created($"/api/orders/{order.Id}", dto);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Mine()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(o => ToDto(o)).ToList());
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PagedResponse<OrderDto>>> All([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Orders.AsNoTracking();
        var total = await query.CountAsync();
        var orders = await query
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<OrderDto>(orders.Select(o => ToDto(o)).ToList(), page, pageSize, total));
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not ("pending" or "paid" or "cancelled"))
        {
            return BadRequest(new { error = "Status must be pending, paid or cancelled." });
        }

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        order.Status = status;
        await db.SaveChangesAsync();

        var dto = ToDto(order);
        await hub.Clients
            .Groups($"user-{order.UserId}", "admins")
            .SendAsync("orderStatusChanged", dto, HttpContext.RequestAborted);

        return Ok(dto);
    }

    private static OrderDto ToDto(Order order, IReadOnlyDictionary<int, Product>? products = null) => new(
        order.Id,
        order.Status,
        order.Total,
        order.CreatedAt,
        order.Items
            .Select(i => new OrderItemDto(
                i.ProductId,
                products is not null && products.TryGetValue(i.ProductId, out var product)
                    ? product.Name
                    : i.Product?.Name ?? "",
                i.Quantity,
                i.UnitPrice))
            .ToList());
}
