using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(2), MaxLength(80)] string Name,
    [Required, MinLength(6), MaxLength(100)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record UserDto(int Id, string Email, string Name, string Role);

public record AuthResponse(string Token, UserDto User);
