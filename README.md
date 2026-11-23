# Orders API

A small but complete ASP.NET Core REST API for an ordering/inventory flow (coworking products: day passes, meeting rooms, offices, coffee). Built as a portfolio project to practise the stack used in .NET outsource work: **ASP.NET Core, EF Core, SQL Server, JWT auth, Swagger, Docker and CI**.

## Features

- **JWT auth** — register/login, 7-day tokens, BCrypt password hashing, role-based access (`admin` / `customer`).
- **Products** — paged list with search, public read; create/update/delete restricted to admins; delete is a soft delete so order history stays valid.
- **Orders** — create an order from product ids; stock is validated and decremented inside a DB transaction; customers see their own orders, admins see all (paged).
- **Validation** — data annotations on all request DTOs; stock and product checks return clear 400/409 responses.
- **Swagger UI** with a Bearer token button at `/swagger`.
- **Health check** at `/health` (process + database).
- **Tests** — 6 xUnit tests covering order totals and rounding.
- **CI** — GitHub Actions: restore, build, test on every push/PR.
- **Docker** — multi-stage build producing a runtime image on port 8080.

## Stack

| Layer | Choice |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core |
| ORM | EF Core 10 (SQLite for local dev, SQL Server by configuration) |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), BCrypt.Net |
| Docs | Swashbuckle (Swagger UI) |
| Tests | xUnit |
| Container | Docker (SDK + ASP.NET runtime images) |

## Run locally

```bash
dotnet run --project src/OrdersApi.Api
```

- API: http://localhost:5000 (or the port printed on startup)
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health

The database (`orders.db`, SQLite) is created and seeded on first start.

**Demo accounts**

| Role | Email | Password |
| --- | --- | --- |
| admin | `admin@example.com` | `Admin123!` |
| customer | `customer@example.com` | `Customer123!` |

## Endpoints

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | — | Create an account, returns a JWT |
| POST | `/api/auth/login` | — | Log in, returns a JWT |
| GET | `/api/auth/me` | user | Current user |
| GET | `/api/products?q=&page=&pageSize=` | — | Paged product list with search |
| GET | `/api/products/{id}` | — | Product detail |
| POST | `/api/products` | admin | Create a product |
| PUT | `/api/products/{id}` | admin | Update a product |
| DELETE | `/api/products/{id}` | admin | Soft-delete a product |
| POST | `/api/orders` | user | Create an order (validates and decrements stock) |
| GET | `/api/orders/my` | user | My orders |
| GET | `/api/orders?page=&pageSize=` | admin | All orders |
| GET | `/health` | — | Health check |

## Configuration

| Setting | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:Sqlite` | `Data Source=orders.db` | Used when no SQL Server string is set |
| `ConnectionStrings:SqlServer` | *(empty)* | If set, the app uses the SQL Server provider instead |
| `Jwt:Key` | dev key in `appsettings.json` | **Replace** in any real deployment |
| `Jwt:Issuer` / `Jwt:Audience` | `OrdersApi` | Token validation |

Use user-secrets or environment variables for real secrets:

```bash
dotnet user-secrets set "Jwt:Key" "a-long-random-secret"
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=...;Database=Orders;..."
```

## Docker

```bash
docker build -t orders-api .
docker run --rm -p 8080:8080 -e Jwt__Key="a-long-random-secret" orders-api
```

## Design decisions and tradeoffs

- **SQLite by default, SQL Server by config.** A reviewer can clone and run without installing anything, while the same EF Core model switches to SQL Server with one connection string. Tradeoff: no provider-specific tuning committed.
- **`EnsureCreated` instead of migrations.** Keeps the demo zero-step; migrations would be the next step for a real, evolving schema.
- **Single transaction for order creation.** Stock is checked and decremented in one transaction so a failure does not consume stock. Tradeoff: two simultaneous orders can still oversell because the check-then-update is not row-locked; production would use optimistic concurrency (`rowversion`) or a conditional `UPDATE ... WHERE Stock >= @qty`.
- **Soft delete for products.** Orders keep valid references and history. Tradeoff: queries must filter `IsActive`.
- **7-day JWT, no refresh tokens.** Simple for a demo; a real product would add short-lived access tokens plus refresh.
- **CORS allow-all.** Convenient for trying the API from any frontend; should be restricted per origin in production.

## Tests

```bash
dotnet test
```

6 tests cover order totals, rounding and fractional/zero lines.
