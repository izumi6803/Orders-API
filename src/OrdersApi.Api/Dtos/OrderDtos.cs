using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Api.Dtos;

public record OrderItemRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, 100)] int Quantity);

public record CreateOrderRequest(
    [Required, MinLength(1)] IReadOnlyList<OrderItemRequest> Items);

public record UpdateOrderStatusRequest(
    [Required] string Status);

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record OrderDto(int Id, string Status, decimal Total, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);
