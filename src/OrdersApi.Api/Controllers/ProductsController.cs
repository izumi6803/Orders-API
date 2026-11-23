using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.Data;
using OrdersApi.Api.Dtos;
using OrdersApi.Api.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ProductDto>>> List(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Price, p.Stock))
            .ToListAsync();

        return Ok(new PagedResponse<ProductDto>(items, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.IsActive);
        return product is null
            ? NotFound()
            : Ok(new ProductDto(product.Id, product.Name, product.Sku, product.Price, product.Stock));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Sku == sku))
        {
            return Conflict(new { error = "SKU already exists." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = sku,
            Price = request.Price,
            Stock = request.Stock,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var dto = new ProductDto(product.Id, product.Name, product.Sku, product.Price, product.Stock);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Id != id && p.Sku == sku))
        {
            return Conflict(new { error = "SKU already exists." });
        }

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.Price = request.Price;
        product.Stock = request.Stock;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
