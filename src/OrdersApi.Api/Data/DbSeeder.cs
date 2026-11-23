using OrdersApi.Api.Models;

namespace OrdersApi.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        db.Users.AddRange(
            new User
            {
                Email = "admin@example.com",
                Name = "Demo Admin",
                Role = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            },
            new User
            {
                Email = "customer@example.com",
                Name = "Demo Customer",
                Role = "customer",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
            });

        db.Products.AddRange(
            new Product { Name = "Hot Desk — Day Pass", Sku = "DESK-DAY", Price = 250_000m, Stock = 100 },
            new Product { Name = "Meeting Room — 1 hour", Sku = "ROOM-1H", Price = 450_000m, Stock = 40 },
            new Product { Name = "Private Office — 1 month", Sku = "OFFICE-M", Price = 9_500_000m, Stock = 6 },
            new Product { Name = "Coffee Package — 10 cups", Sku = "COFFEE-10", Price = 350_000m, Stock = 75 });

        db.SaveChanges();
    }
}
