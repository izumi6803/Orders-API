using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Api.Dtos;

public record ProductDto(int Id, string Name, string Sku, decimal Price, int Stock);

public record ProductRequest(
    [Required, MinLength(2), MaxLength(120)] string Name,
    [Required, MinLength(2), MaxLength(40)] string Sku,
    [Range(0, 1_000_000_000)] decimal Price,
    [Range(0, 1_000_000)] int Stock);

public record PagedResponse<T>(IReadOnlyList<T> Data, int Page, int PageSize, int Total);
