# Mercadotnet API — Development Guide

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [OrbStack](https://orbstack.dev/)
- (macOS only) [OrbStack](https://orbstack.dev/) recommended for Docker

## Quick Start

```bash
# 1. Clone and navigate
git clone <repo> mercadotnet
cd mercadotnet

# 2. Copy environment file (edit as needed)
cp .env.example .env

# 3. Start all services (PostgreSQL, Redis, Mailpit, API)
docker compose up -d --build

# 4. Verify health
curl http://localhost:8080/health

# 5. Open the API reference
open http://localhost:8080/scalar/v1
```

## Running Without Docker

```bash
# Start infrastructure
docker compose up -d postgres redis mailpit

# Run the API
cd API
ASPNETCORE_ENVIRONMENT=Development dotnet run

# OpenAPI: http://localhost:5000/scalar/v1
```

## Project Structure

```
API/
├── API.csproj
├── Program.cs                  # Composition root
├── appsettings*.json
├── Common/                     # Shared infrastructure
│   ├── ApiResponse.cs          # Response envelope
│   ├── BaseController.cs       # Base controller class
│   ├── CacheService.cs
│   ├── Cli/                    # CLI command system
│   ├── CurrentUser.cs
│   ├── Email/                  # Email templates + providers
│   ├── Encryption.cs
│   ├── Errors.cs
│   ├── GlobalExceptionHandler.cs
│   ├── PagedRequest.cs         # Pagination DTO with date filtering
│   ├── PagedResult.cs
│   ├── ResourceOwnerHandler.cs
│   └── Scheduler/              # Cron task base class
├── Data/
│   ├── AppDbContext.cs
│   ├── AppDbContextFactory.cs
│   ├── Configurations/
│   └── Migrations/
├── Features/                   # Domain features
│   ├── Auth/                   # Register, login, refresh, me
│   ├── Projects/               # CRUD with owner-scoping
│   └── Tasks/                  # CRUD with cache invalidation
├── Options/                    # Strongly-typed config
└── Cli/Commands/               # CLI command implementations
```

## Database Migrations

Migrations are applied automatically on container startup.

**Create a new migration:**
```bash
cd API
dotnet ef migrations add <Name> -o Data/Migrations
```

**Manually apply:**
```bash
docker exec mercadotnet-api-1 dotnet API.dll
# Migrations run automatically in the web host startup
```

## CLI Commands

Run administrative commands against a running container:

```bash
docker exec mercadotnet-api-1 dotnet API.dll cli
docker exec mercadotnet-api-1 dotnet API.dll cli seed:admin
```

## Email Testing

Mailpit runs at http://localhost:8025 for catching test emails.

## Environment Variables

| Variable | Required | Description |
|---|---|---|
| `JWT__SECRET` | Yes | 32+ byte base64-encoded secret |
| `DATABASE__CONNECTIONSTRING` | Yes | PostgreSQL connection string |
| `REDIS__CONNECTIONSTRING` | Yes | Redis connection string |
| `STORAGE__PROVIDER` | No | `local` (default), `s3`, or `cloudinary` |
| `EMAIL__PROVIDER` | No | `smtp` (default), `resend`, or `zeptomail` |

## Testing

```bash
# Register
curl -X POST http://localhost:8080/v1/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"test@example.com","password":"Test1234!","firstName":"Jane","lastName":"Doe"}'

# Login
curl -X POST http://localhost:8080/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"test@example.com","password":"Test1234!"}'
```
