# Mercadotnet API — Architecture

## Overview

Mercadotnet is a **production-grade task tracker API** built with **ASP.NET Core 10** (.NET 10). It follows a single-project, feature-folder layout with a vertical-slice architecture. Each feature (Auth, Projects, Tasks) contains its own Models, DTOs, Services, and Controllers — no cross-feature coupling.

```
┌─────────────────────────────────────────────────────────┐
│                    API (ASP.NET Core 10)                 │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │   Auth   │  │ Projects │  │  Tasks   │              │
│  │  Feature │  │  Feature │  │  Feature │              │
│  │          │  │          │  │          │              │
│  │ /register│  │ POST     │  │ POST     │              │
│  │ /login   │  │ GET      │  │ GET      │              │
│  │ /me      │  │ PATCH    │  │ PATCH    │              │
│  │ /refresh │  │ DELETE   │  │ DELETE   │              │
│  │ /password│  │          │  │          │              │
│  │  -reset  │  │          │  │          │              │
│  │ /change- │  │          │  │          │              │
│  │  password│  │          │  │          │              │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘              │
│       │             │             │                     │
│  ┌────▼─────────────▼─────────────▼──────────────────┐  │
│  │              Common Infrastructure                 │  │
│  │  ApiResponse  BaseController  PagedResult         │  │
│  │  CacheService CurrentUser    Email  Storage       │  │
│  │  Scheduler    CLI            Encryption           │  │
│  └──────────────────────┬───────────────────────────┘  │
│                         │                               │
│  ┌──────────────────────▼───────────────────────────┐  │
│  │              Data / EF Core                       │  │
│  │  AppDbContext  Migrations  Entity Configurations  │  │
│  └──────────────────────┬───────────────────────────┘  │
└─────────────────────────┼───────────────────────────────┘
                          │
              ┌───────────┴───────────┐
              │                       │
        ┌─────▼─────┐          ┌──────▼──────┐
        │ PostgreSQL │          │   Redis     │
        │ (data)     │          │ (cache/OTP) │
        └───────────┘          └─────────────┘
```

## Service Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 10 (ASP.NET Core) |
| **Database** | PostgreSQL 16 via EF Core |
| **Cache** | Redis 7 via `IDistributedCache` |
| **Auth** | JWT Bearer (access + refresh token rotation) |
| **Validation** | FluentValidation |
| **Serialization** | System.Text.Json |
| **API Docs** | Microsoft.AspNetCore.OpenApi + Scalar |
| **Logging** | Serilog |
| **Error Tracking** | Sentry (optional) |
| **Email** | SMTP / Resend / ZeptoMail (switchable) |
| **Storage** | S3 / Cloudinary / Local (switchable) |
| **Rate Limiting** | Built-in `Microsoft.AspNetCore.RateLimiting` |
| **Background Work** | Channel<T> + BackgroundService |
| **Scheduling** | Cronos-based cron tasks |

## Request Lifecycle

```
HTTP Request
    │
    ▼
┌──────────────────┐
│  Rate Limiter    │  ← auth-strict / api-default / write-strict policies
└──────┬───────────┘
       ▼
┌──────────────────┐
│  Exception       │  ← GlobalExceptionHandler catches all unhandled
│  Handler         │    errors and wraps them in ApiResponse envelope
└──────┬───────────┘
       ▼
┌──────────────────┐
│  Authentication  │  ← JWT Bearer (MapInboundClaims = false)
│  + Authorization │
└──────┬───────────┘
       ▼
┌──────────────────┐
│  Controller      │  ← Inherits from BaseController<T>
│  Action          │
└──────┬───────────┘
       ▼
┌──────────────────┐
│  Service Layer   │  ← Business logic, DB queries, cache
└──────┬───────────┘
       ▼
┌──────────────────┐
│  EF Core /       │
│  PostgreSQL      │
└──────────────────┘
```

## Auth Flow

```
┌────────┐     ┌────────┐     ┌─────────┐     ┌──────────┐
│ Client │     │  API   │     │  Redis  │     │ Postgres │
└───┬────┘     └───┬────┘     └────┬────┘     └────┬─────┘
    │  POST /register  │            │              │
    │─────────────────>│            │              │
    │                  │  Create user             │
    │                  │─────────────────────────>│
    │                  │  Send welcome email       │
    │  { user }       │  (background queue)       │
    │<─────────────────│            │              │
    │                  │            │              │
    │  POST /login     │            │              │
    │─────────────────>│            │              │
    │                  │  Verify credentials      │
    │                  │─────────────────────────>│
    │                  │  Create refresh token    │
    │                  │─────────────────────────>│
    │                  │  Send "new login" email   │
    │  { accessToken,  │  (background queue)       │
    │    refreshToken }│            │              │
    │<─────────────────│            │              │
    │                  │            │              │
    │  POST /password  │            │              │
    │  -reset          │            │              │
    │─────────────────>│            │              │
    │                  │  Generate 6-digit OTP    │
    │                  │  ────────>               │
    │                  │   (hash & store with TTL) │
    │                  │  Send OTP email           │
    │  { success }    │  (background queue)       │
    │<─────────────────│            │              │
    │                  │            │              │
    │  POST /password  │            │              │
    │  -reset/confirm  │            │              │
    │  { email, otp,   │            │              │
    │    newPassword } │            │              │
    │─────────────────>│            │              │
    │                  │  Verify OTP              │
    │                  │  ────────>               │
    │                  │  Update password         │
    │                  │─────────────────────────>│
    │  { success }    │  Delete OTP from cache    │
    │<─────────────────│  ────────>               │
    │                  │                          │
```

## Email Background Queue

```
┌──────────────┐    Channel<T>    ┌───────────────────┐
│  AuthService │ ────────────────> │  EmailBackground  │
│  (producer)  │     bounded      │  Service          │
│              │     queue (200)  │  (consumer)        │
└──────────────┘                  └────────┬──────────┘
                                           │
                                    ┌──────▼──────┐
                                    │  IEmailService │
                                    │  (provider)    │
                                    │  smtp/resend/  │
                                    │  zeptomail     │
                                    └───────────────┘
```

This replaces the old fire-and-forget `Task.Run` pattern. The `EmailQueue` uses a bounded `Channel<EmailQueueEntry>` with `DropOldest` policy when the queue exceeds 200 items — the application continues to function under load without back-pressure crashes.

## Storage

Storage is provider-switchable via the `STORAGE__PROVIDER` environment variable:

```
STORAGE__PROVIDER=s3 | cloudinary | local
```

| Provider | Best For | Notes |
|---|---|---|
| `local` | Development | Files written to `uploads/` directory |
| `s3` | Production | AWS S3 with public-read ACL |
| `cloudinary` | Production | Cloudinary CDN with transformations |

Files are uploaded per the straqa/gefe pattern: client uploads → service returns URL → URL is stored on the entity. See `docs/running.md` for the upload flow.

## Scheduled Tasks

Scheduled tasks use the `BaseScheduledTask` abstract class with Cronos cron expressions:

| Task | Schedule | Purpose |
|---|---|---|
| `CleanupExpiredRefreshTokensTask` | Daily 3am (`0 3 * * *`) | Remove expired refresh tokens from DB |

New tasks extend `BaseScheduledTask`, provide a cron expression, and register with `AddHostedService<T>()`.

## CLI Commands

CLI commands use the `CliCommand` base class with `[Command]` and `[CommandGroup]` attributes, mirroring errandigo's `nestjs-command` pattern:

```
docker exec <container> dotnet API.dll cli seed:admin
```

The CLI dispatcher discovers commands via reflection at runtime. The command runs in a scoped DI container pulled from the app's service provider, giving it access to all infrastructure (DbContext, Redis, etc.) without starting Kestrel.

## Directory Structure

```
API/
├── API.csproj                 # Project file
├── Program.cs                 # Composition root (DI, middleware, pipeline)
├── appsettings*.json          # Configuration
│
├── Common/                    # Shared infrastructure — NOT a library/framework
│   ├── ApiResponse.cs         # Generic envelope: ApiResponse<T>
│   ├── BaseController.cs      # Base class with OkResult/CreatedResult helpers
│   ├── CacheService.cs        # Redis-backed IDistributedCache wrapper
│   ├── CurrentUser.cs         # Extracts user ID from JWT claims
│   ├── Email/                 # IEmailService, EmailQueue, providers
│   │   ├── IEmailService.cs   # Interface + EmailRenderer + FeatureEmailTemplates
│   │   ├── EmailQueue.cs      # Channel<T> producer/consumer queue
│   │   ├── EmailBackgroundService.cs
│   │   ├── Providers/         # SmtpEmailService, ResendEmailService, ZeptoMailEmailService
│   │   └── Templates/         # Auth/*.mjml (per-feature template registration)
│   ├── Encryption.cs          # IDataEncryptor (DataProtection-based)
│   ├── Errors.cs              # ApiException, NotFoundException, etc.
│   ├── GlobalExceptionHandler.cs
│   ├── PagedRequest.cs        # Paginated query DTO with date filtering
│   ├── PagedResult.cs         # Paginated response envelope
│   ├── ResourceOwnerHandler.cs
│   ├── Scheduler/             # BaseScheduledTask (cron-based BackgroundService)
│   ├── Storage/               # IStorageService, S3/Cloudinary/Local providers
│   └── Cli/                   # CliCommand base, CliDispatcher
│
├── Data/                      # EF Core setup
│   ├── AppDbContext.cs
│   ├── AppDbContextFactory.cs
│   ├── Configurations/        # Entity type configurations
│   └── Migrations/            # EF Core migrations
│
├── Features/                  # Vertical slices
│   ├── Auth/
│   │   ├── Controllers/       # AuthController
│   │   ├── DTOs/              # RegisterRequest, LoginRequest, etc.
│   │   ├── Models/            # User, RefreshToken
│   │   ├── Services/          # AuthService, JwtTokenService, PasswordHasher
│   │   ├── Validators/        # FluentValidation validators
│   │   └── ScheduledTasks/    # CleanupExpiredRefreshTokensTask
│   ├── Projects/              # Same structure
│   └── Tasks/                 # Same structure (with Redis cache invalidation)
│
├── Options/                   # Strongly-typed config classes
│   ├── CorsOptions.cs
│   ├── DatabaseOptions.cs
│   ├── EmailOptions.cs
│   ├── JwtOptions.cs
│   ├── RedisOptions.cs
│   └── StorageOptions.cs
│
└── Cli/Commands/              # CLI command implementations
    └── SeedAdminCommand.cs
```

## Key Design Decisions

1. **Single project, not multi-project clean architecture** — Faster iteration. Features are isolated by folder, not assembly. Prevents premature over-engineering.

2. **Feature folders over module folders** — Each feature is self-contained. If we need to extract a feature into its own microservice later, we literally move the folder.

3. **No service interfaces file per file** — Each feature's services implement an interface at the top of the same file. Clean and discoverable. Following the errandigo pattern.

4. **Email provider-switchable at startup** — Configured via `Email:Provider` env var. Same pattern as storage. Providers are registered as scoped services and selected by a factory lambda.

5. **Redis for both cache and OTP** — `ICacheService` wraps `IDistributedCache`. OTP for password resets is stored in Redis with a 10-minute TTL. The OTP is BCrypt-hashed before storage.

6. **Channel<T> for background work** — In-process, reliable, no external broker dependency. If you need persistence across restarts, swap in RabbitMQ/BullMQ later.

7. **Per-feature template registration** — Templates live in `Common/Email/Templates/{Feature}/{Template}.mjml`. Features register their templates via `FeatureEmailTemplates` constants. Template validation is eager (at startup).

## Common Patterns

### Response Envelope
Every endpoint returns `{ success, message, data, errors }`. Paginated endpoints return `data: { items, page, pageSize, totalCount, totalPages, hasNext, hasPrevious }`.

### BaseController
All controllers extend `BaseController(ICurrentUser)` which provides:
- `CurrentUser.Id` — the authenticated user's GUID
- `OkResult<T>()`, `CreatedResult<T>()`, `DeletedResult()` — typed response helpers

### Exception Handling
Business logic throws typed exceptions (`NotFoundException`, `ConflictException`, `UnauthorizedException`). The `GlobalExceptionHandler` catches these and serializes them into the standard error envelope.
