using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.Data;
using OrdersApi.Api.Dtos;
using OrdersApi.Api.Models;
using OrdersApi.Api.Services;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { error = "Email is already registered." });
        }

        var user = new User
        {
            Email = email,
            Name = request.Name.Trim(),
            Role = "customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created($"/api/auth/me", new AuthResponse(tokens.Create(user), ToDto(user)));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        return Ok(new AuthResponse(tokens.Create(user), ToDto(user)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await db.Users.FindAsync(userId);
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.Name, user.Role);
}
