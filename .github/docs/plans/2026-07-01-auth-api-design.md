# taskr — Auth + Task Tracker API Design

**Date:** 2026-07-01
**Status:** Approved
**Target:** ASP.NET Core 10 / .NET 10

## 1. Purpose

A production-shaped, showcase-grade authentication and task-tracker API built with
ASP.NET Core 10. The project is a learning artifact for C# and the ASP.NET Core
ecosystem, but it follows the same architectural patterns used in three of the
author's existing production projects (Bounti/Django, straqa/Django, errandigo/NestJS):

- bounded-context feature folders
- layered (Controller → Service → DbContext)
- uniform response envelope
- global exception handler
- configuration via env vars + appsettings
- multi-stage Docker + non-root user + healthcheck
- per-endpoint rate limiting
- encrypted PII at rest
- Sentry for error observability
- Redis-backed distributed caching
- structured logging via Serilog

## 2. Scope

### In scope (v1)

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `GET  /api/v1/auth/me` (bearer)
- `GET  /api/v1/projects` (bearer, paginated)
- `POST /api/v1/projects` (bearer)
- `GET  /api/v1/projects/{id}` (bearer, owner-only)
- `PATCH /api/v1/projects/{id}` (bearer, owner-only)
- `DELETE /api/v1/projects/{id}` (bearer, owner-only)
- `GET  /api/v1/projects/{projectId}/tasks` (bearer, paginated, cached)
- `POST /api/v1/projects/{projectId}/tasks` (bearer)
- `GET  /api/v1/tasks/{id}` (bearer, owner-only via project)
- `PATCH /api/v1/tasks/{id}` (bearer, owner-only)
- `DELETE /api/v1/tasks/{id}` (bearer, owner-only)
- `GET  /health` (no auth, reports DB + Redis)

### Out of scope (YAGNI — defer to v2)

- OAuth / social login (Google, Facebook)
- Email verification flow
- Password reset / OTP
- RBAC, project membership / collaborators
- Audit log (django-simple-history equivalent)
- Soft delete (using hard delete + `UpdatedAt` for v1)
- Full-text search (using `ILIKE` for v1)
- Encrypted PII beyond `User.PhoneNumber`
- Push notifications
- xUnit tests (user opted out for v1)
- Multi-tenant / organisation scoping

## 3. Stack

| Layer | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 | Already in `API.csproj` |
| Web | ASP.NET Core 10 (controllers) | `--use-controllers` |
| ORM | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` | |
| Validation | `FluentValidation.AspNetCore` | Closest to NestJS `class-validator` |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` + `BCrypt.Net-Next` | HS256, refresh rotation w/ reuse-detection |
| Response shape | `{ success, message, data, errors? }` via `IAsyncResultFilter` | Matches all three reference projects |
| OpenAPI | Built-in `Microsoft.AspNetCore.OpenApi` (already in csproj) + `Swashbuckle.AspNetCore` for the Swagger UI | |
| Logging | `Serilog.AspNetCore` (console JSON sink) | |
| Errors | `Sentry.AspNetCore` | DSN from `Sentry:Dsn` |
| Rate limiting | Built-in `Microsoft.AspNetCore.RateLimiting` | Sliding-window policies |
| Email | `MailKit` + `IEmailService` abstraction; `mailpit` in compose for dev, real SMTP in prod | Switchable via env vars |
| Caching | `IDistributedCache` + `Microsoft.Extensions.Caching.StackExchangeRedis` | `redis:7-alpine` in compose |
| Healthchecks | `AspNetCore.HealthChecks.NpgSql` + `AspNetCore.HealthChecks.Redis` | |
| Migrations | `dotnet ef migrations add ...` | Applied on container start |

## 4. Solution Layout

```
taskr/
├── API.sln
├── docker-compose.yml
├── docker-compose.override.yml           # dev-only: bind mounts, mailpit exposed
├── Dockerfile
├── .dockerignore
├── .env.example
├── .editorconfig
├── README.md
├── docs/
│   └── plans/
│       └── 2026-07-01-auth-api-design.md   # ← this file
├── db/
│   └── init.sql                          # optional: create db/user on first run
└── src/
    └── API/
        ├── API.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── API.http
        ├── Common/
        │   ├── ApiResponse.cs              # envelope + PagedResult<T>
        │   ├── ApiResponseFilter.cs        # wraps successful results
        │   ├── GlobalExceptionHandler.cs   # uniform 4xx/5xx envelope
        │   ├── ApiControllerBase.cs
        │   ├── Pagination/
        │   │   ├── PagedRequest.cs
        │   │   ├── PagedResult.cs
        │   │   └── PagedRequestValidator.cs
        │   ├── Options/
        │   │   ├── JwtOptions.cs
        │   │   ├── DatabaseOptions.cs
        │   │   ├── RedisOptions.cs
        │   │   ├── EmailOptions.cs
        │   │   ├── RateLimitOptions.cs
        │   │   └── CorsOptions.cs
        │   ├── Extensions/
        │   │   ├── ServiceCollectionExtensions.cs
        │   │   ├── WebApplicationExtensions.cs
        │   │   ├── SwaggerExtensions.cs
        │   │   ├── SentryExtensions.cs
        │   │   ├── RateLimitingExtensions.cs
        │   │   ├── CachingExtensions.cs
        │   │   ├── EmailServiceExtensions.cs
        │   │   ├── EncryptionExtensions.cs
        │   │   └── DatabaseExtensions.cs
        │   ├── Encryption/
        │   │   ├── IDataEncryptor.cs
        │   │   ├── DataProtectionEncryptor.cs
        │   │   ├── EncryptedPersonalDataAttribute.cs
        │   │   ├── EncryptedStringConverter.cs
        │   │   └── EncryptedPersonalDataConvention.cs
        │   ├── Email/
        │   │   ├── IEmailService.cs
        │   │   ├── SmtpEmailService.cs
        │   │   ├── EmailMessage.cs
        │   │   └── Templates/
        │   │       ├── WelcomeEmail.cshtml
        │   │       └── NewLoginEmail.cshtml
        │   ├── Caching/
        │   │   ├── ICacheService.cs
        │   │   └── RedisCacheService.cs
        │   ├── Authorization/
        │   │   ├── ResourceOwnerRequirement.cs
        │   │   └── ResourceOwnerHandler.cs
        │   ├── CurrentUser/
        │   │   ├── ICurrentUser.cs
        │   │   └── CurrentUser.cs
        │   └── Errors/
        │       ├── ApiException.cs
        │       ├── NotFoundException.cs
        │       ├── ConflictException.cs
        │       ├── ForbiddenException.cs
        │       └── UnauthorizedException.cs
        ├── Features/
        │   ├── Auth/
        │   │   ├── AuthController.cs
        │   │   ├── IAuthService.cs / AuthService.cs
        │   │   ├── IJwtTokenService.cs / JwtTokenService.cs
        │   │   ├── IPasswordHasher.cs / PasswordHasher.cs
        │   │   ├── DTOs/{Register,Login,Refresh,User,AuthTokens}Request|Response.cs
        │   │   ├── Validators/{Register,Login,Refresh}RequestValidator.cs
        │   │   └── Entities/{User,RefreshToken}.cs
        │   ├── Projects/
        │   │   ├── ProjectsController.cs
        │   │   ├── IProjectsService.cs / ProjectsService.cs
        │   │   ├── DTOs/{Create,Update}ProjectRequest.cs + ProjectResponse.cs + ProjectListQuery.cs
        │   │   ├── Validators/{Create,Update}ProjectRequestValidator.cs
        │   │   └── Entities/Project.cs
        │   └── Tasks/
        │       ├── TasksController.cs
        │       ├── ITasksService.cs / TasksService.cs
        │       ├── DTOs/{Create,Update}TaskRequest.cs + TaskResponse.cs + TaskListQuery.cs
        │       ├── Validators/{Create,Update}TaskRequestValidator.cs
        │       └── Entities/{TaskItem,TaskStatus,TaskPriority}.cs
        └── Infrastructure/
            └── Persistence/
                ├── AppDbContext.cs
                ├── AppDbContextFactory.cs
                ├── Configurations/{User,RefreshToken,Project,TaskItem}Configuration.cs
                ├── HealthChecks/{Database,Redis}HealthCheck.cs
                └── Migrations/                   # generated by `dotnet ef`
```

### Reference project mapping

| Reference concept | This project |
|---|---|
| `apps/accounts/` (Bounti/Straqa) | `Features/Auth/` |
| `apps/accounts/services.py` | `Features/Auth/AuthService.cs` |
| `apps/accounts/serializers/auth.py` | `Features/Auth/DTOs/` + `Features/Auth/Validators/` |
| `apps/core/mixins.py` (ResponseMixin) | `Common/ApiResponseFilter.cs` |
| `apps/core/middlewares.py` (exception handler) | `Common/GlobalExceptionHandler.cs` |
| `apps/accounts/migrations/` | `Features/Auth/Migrations/` (or `Infrastructure/Persistence/Migrations/`) |
| `compose.yml` | `docker-compose.yml` (root) |
| `env.example` | `.env.example` |

## 5. Data Model

```csharp
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;          // unique, normalized lowercase
    public string PasswordHash { get; set; } = default!;   // BCrypt
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;

    [EncryptedPersonalData]
    public string? PhoneNumber { get; set; }               // encrypted at rest

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<TaskItem> AssignedTasks { get; set; } = [];
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = default!;      // SHA-256(raw)
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }       // chain to successor
    public string? RevokedReason { get; set; }            // "Rotated" | "Reuse detected" | "Logout"
}

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TaskItem> Tasks { get; set; } = [];
}

public class TaskItem                      // named TaskItem to avoid
{                                          // System.Threading.Tasks.Task collision
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum TaskStatus { Todo, InProgress, InReview, Done, Archived }
public enum TaskPriority { Low, Medium, High, Urgent }
```

**Indexes (defined in entity configurations):**

- `User.Email` — unique
- `RefreshToken.TokenHash` — unique
- `RefreshToken.UserId`
- `Project.OwnerId`
- `TaskItem.ProjectId`
- `TaskItem.AssigneeId`
- `TaskItem.Status`
- `TaskItem.DueDate`

**Cascade behaviour:** deleting a Project cascades to its Tasks via `OnDelete(DeleteBehavior.Cascade)` in `TaskItemConfiguration`.

## 6. API Surface

All endpoints under `/api/v1`. All requests/responses are JSON. All responses use
the standard envelope (§8). Auth endpoints are rate-limited more strictly than
resource endpoints.

| Method | Path | Auth | Rate-limit | Notes |
|---|---|---|---|---|
| `POST` | `/auth/register` | none | `auth-strict` | Creates user, sends welcome email |
| `POST` | `/auth/login` | none | `auth-strict` | Returns tokens, sends "new login" email |
| `POST` | `/auth/refresh` | none | `auth-strict` | Rotates refresh, reuse-detection revokes family |
| `GET` | `/auth/me` | bearer | `api-default` | Current user |
| `GET` | `/projects` | bearer | `api-default` | Paginated, filter by name search |
| `POST` | `/projects` | bearer | `write-strict` | Create owned project |
| `GET` | `/projects/{id}` | bearer | `api-default` | Owner-only |
| `PATCH` | `/projects/{id}` | bearer | `write-strict` | Owner-only |
| `DELETE` | `/projects/{id}` | bearer | `write-strict` | Owner-only (cascades) |
| `GET` | `/projects/{projectId}/tasks` | bearer | `api-default` | Paginated, cached 60s, filter by status/priority/search/sort |
| `POST` | `/projects/{projectId}/tasks` | bearer | `write-strict` | Invalidates list cache |
| `GET` | `/tasks/{id}` | bearer | `api-default` | Owner-only via project |
| `PATCH` | `/tasks/{id}` | bearer | `write-strict` | Owner-only, invalidates list cache |
| `DELETE` | `/tasks/{id}` | bearer | `write-strict` | Owner-only, invalidates list cache |
| `GET` | `/health` | none | n/a | DB + Redis health |

## 7. Data Flow (representative — task list with cache)

1. Client calls `GET /api/v1/projects/{projectId}/tasks?page=1&pageSize=20&status=Todo`
2. `[Authorize]` validates bearer JWT
3. `[EnableRateLimiting("api-default")]` partitions by `userId`
4. `TasksController.List(projectId, query)` runs `PagedRequestValidator` on `query`
5. Controller calls `TasksService.ListAsync(currentUserId, projectId, query)`
6. Service checks ownership: `project.OwnerId == currentUserId` (else `ForbiddenException`)
7. Service computes cache key `tasks:project:{projectId}:p=1:s=20:st=Todo:...`
8. Service calls `ICacheService.GetAsync<PagedResult<TaskResponse>>(key)`
   - If hit: return cached
   - If miss: query DB via EF Core, project to `PagedResult<TaskResponse>`, cache 60s, return
9. `ApiResponseFilter` wraps result in `{ success, message, data }`
10. Client receives 200 with envelope

On write (`POST/PATCH/DELETE /tasks/{id}`):
1. … same auth + rate-limit pipeline
2. Service mutates DB, then `ICacheService.RemoveByPatternAsync($"tasks:project:{projectId}:*")` + `tasks:{taskId}` removed

## 8. Response Envelope

```jsonc
// Success (single resource)
{
  "success": true,
  "message": "Operation successful",
  "data": { "id": "...", "email": "...", ... }
}

// Success (paginated)
{
  "success": true,
  "message": "Operation successful",
  "data": {
    "items": [ { ... }, { ... } ],
    "page": 1, "pageSize": 20, "totalCount": 137, "totalPages": 7,
    "hasNext": true, "hasPrevious": false
  }
}

// Error (validation)
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    { "field": "email", "message": "Email is required" }
  ]
}

// Error (server)
{
  "success": false,
  "message": "An unexpected error occurred.",
  "errorId": "0HMVFE8R7G6QS:00000001"  // Sentry event id
}
```

## 9. Auth & JWT

- **Algorithm:** HS256, secret from `JWT__SECRET` (≥32 bytes; `openssl rand -base64 64`)
- **Access token:** 15 min; claims `sub`, `email`, `jti`, `iat`, `exp`, `iss`, `aud`
- **Refresh token:** 7 days, 32 random bytes base64url-encoded; **stored as SHA-256 hash only** (raw never persisted)
- **Refresh rotation:** every `/auth/refresh` issues a new refresh, marks the old one `RevokedAt = now`, sets `ReplacedByTokenHash`
- **Reuse detection:** if a `RevokedAt != null` refresh is presented, revoke the entire token family for that user and force re-login
- **`AddJwtBearer` config:** `ValidateIssuer = ValidateAudience = ValidateLifetime = ValidateIssuerSigningKey = true`; `ClockSkew = TimeSpan.FromSeconds(30)`; `MapInboundClaims = false`
- **Password hashing:** `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`

## 10. Encryption (Data Protection)

- `services.AddDataProtection().SetApplicationName("taskr-api").PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))`
- Docker named volume `dpapi-keys` mounted at that path
- `EncryptedPersonalDataAttribute` — marker attribute on entity properties
- `EncryptedStringConverter` — EF Core `ValueConverter<string, string>` that calls `IDataEncryptor` on write / read
- `EncryptedPersonalDataConvention` — `IModelFinalizingConvention` that scans the model for the attribute and applies the converter
- v1: only `User.PhoneNumber` is marked

## 11. Rate Limiting

Built-in `Microsoft.AspNetCore.RateLimiting` (no extra package).

| Policy | Partition | Limit | Applied to |
|---|---|---|---|
| `auth-strict` | IP | 5 req / 5 min | `POST /auth/*` |
| `api-default` | user id (bearer) / IP (anon) | 100 req / min | all other endpoints |
| `write-strict` | user id | 30 req / min | `POST/PATCH/DELETE` on resources |

Apply with `[EnableRateLimiting("policy-name")]` attributes. `OnRejected` returns the standard envelope with HTTP 429.

## 12. Caching

- `IDistributedCache` → Redis (`redis:7-alpine` in compose)
- **Task list cache key:** `tasks:project:{projectId}:p={page}:s={pageSize}:st={status}:pr={priority}:q={search}:sort={sort}`, TTL 60s
- **Single task cache key:** `tasks:{taskId}`, TTL 60s
- **Invalidation:** on task create/update/delete, `TasksService` calls `ICacheService.RemoveByPatternAsync($"tasks:project:{projectId}:*")` and removes `tasks:{taskId}`
- **User cache key:** `users:{id}`, TTL 5 min, invalidated on user update
- `RedisCacheService.RemoveByPatternAsync` uses `SCAN` (not `KEYS`) to avoid blocking Redis on large keyspaces

## 13. Email

- `IEmailService` + `SmtpEmailService` (MailKit) — single implementation, target host changes
- **Config (env-driven):**
  - `EMAIL__SMTP__HOST` — `mailpit` in dev, real SMTP in prod
  - `EMAIL__SMTP__PORT` — `1025` in dev
  - `EMAIL__SMTP__USETLS` — `false` in dev
  - `EMAIL__SMTP__USERNAME` / `PASSWORD` — empty in dev
  - `EMAIL__SMTP__FROM` — `noreply@taskr.local`
- **Dev:** `mailpit` service in `docker-compose.yml` (`axllent/mailpit:latest`), exposed on `localhost:8025` for UI
- **Triggers:**
  - `POST /auth/register` → Welcome email
  - `POST /auth/login` → "New login from {ip} at {time}" email (security notification)
- Email send failures are **logged but never break the auth flow** — a failed welcome email must not fail registration

## 14. Sentry

- `Sentry.AspNetCore` initialized in `Program.cs` with DSN from `SENTRY__DSN`
- Captures unhandled exceptions automatically
- `SentrySdk.CaptureException(ex)` called from `GlobalExceptionHandler` for 5xx errors only
- `tracesSampleRate = 0.2`
- DSN is **optional** — if empty, Sentry is not initialized (dev convenience)

## 15. Configuration

Every `Options` class uses:
```csharp
services.AddOptions<T>()
    .Bind(builder.Configuration.GetSection(sectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`appsettings.json` carries non-secret defaults. **All real secrets come from env vars** (read automatically by `AddEnvironmentVariables()` with `__` as the section separator).

```env
# .env.example
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5001

# Database
POSTGRES_USER=taskr
POSTGRES_PASSWORD=changeme
POSTGRES_DB=taskr
DATABASE__CONNECTIONSTRING=Host=postgres;Port=5432;Database=taskr;Username=taskr;Password=changeme

# Redis
REDIS__CONNECTIONSTRING=redis:6379

# JWT
JWT__SECRET=replace-with-output-of-openssl-rand-base64-64
JWT__ISSUER=taskr-api
JWT__AUDIENCE=taskr-clients
JWT__ACCESSTOKENLIFETIMEMINUTES=15
JWT__REFRESHTOKENLIFETIMEDAYS=7

# CORS
CORS__ALLOWEDORIGINS__0=http://localhost:3000

# Sentry (optional - leave empty to disable in dev)
SENTRY__DSN=

# Email (mailpit in dev; real SMTP in prod)
EMAIL__SMTP__HOST=mailpit
EMAIL__SMTP__PORT=1025
EMAIL__SMTP__USETLS=false
EMAIL__SMTP__USERNAME=
EMAIL__SMTP__PASSWORD=
EMAIL__SMTP__FROM=noreply@taskr.local
```

## 16. Docker

`Dockerfile` (multi-stage, non-root, healthcheck):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/API/API.csproj src/API/
RUN dotnet restore src/API/API.csproj
COPY . .
RUN dotnet publish src/API/API.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd --gid 1000 app && useradd --uid 1000 --gid app --shell /bin/bash --create-home app
COPY --from=build --chown=app:app /app .
USER app
EXPOSE 5001
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:5001/health || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
```

`docker-compose.yml` (4 services):

| Service | Image | Port | Purpose |
|---|---|---|---|
| `api` | built from `Dockerfile` | 5001 | The API |
| `postgres` | `postgres:16-alpine` | 5432 | Primary DB |
| `redis` | `redis:7-alpine` | (internal) | Cache |
| `mailpit` | `axllent/mailpit:latest` | 8025 (UI), 1025 (SMTP, internal) | Dev email catcher |

`depends_on` uses `condition: service_healthy` on every dependent service. Migrations are applied on container start via `app.MigrateAsync()` in `Program.cs` (idempotent). A named volume `dpapi-keys` is mounted at `/home/app/.aspnet/DataProtection-Keys` so encrypted PII stays decryptable across container restarts.

`docker-compose.override.yml` (dev only): bind-mount the source tree into `/src` and use `dotnet watch run` for hot reload.

## 17. Implementation Order

1. Scaffold solution + project + add NuGet packages
2. Options classes + validation
3. Common errors + envelope + filters + exception handler
4. Encryption helpers (DataProtection + EF converter + convention)
5. Email service abstraction
6. Caching service (Redis wrapper with pattern delete)
7. CurrentUser + authorization handlers
8. DbContext + entity configurations
9. **Auth feature:** entities → DTOs → validators → service → controller
10. **Projects feature:** entity → DTOs → validators → service → controller
11. **Tasks feature:** entity → DTOs → validators → service (with cache) → controller
12. Wire up `Program.cs`: DI, Sentry, JWT, Swagger, Serilog, Rate Limiting, CORS, Healthchecks, migrations
13. `dotnet ef migrations add InitialCreate`
14. Dockerfile + docker-compose + .env.example + .dockerignore
15. Smoke test (compose up → register → login → /me → projects → tasks)
16. README with endpoint table + sample `.http` calls

## 18. Open Questions / Risks

- **Data Protection keys in Docker:** the named volume approach is correct for a single-host setup. For multi-host (swarm/k8s) the key ring would need a shared secret store. **Documented limitation, deferred.**
- **Refresh token reuse detection** is implemented in v1 because the surface area is small. Family revocation cascades correctly.
- **Email send is fire-and-forget** in the request path — a `Task.Run` is used to send the email, exceptions are logged, and the user-facing flow is not blocked. Acceptable for a showcase; v2 could use a hosted service with a queue.

## 19. References

- Bounti backend (Django) — `apps/accounts/`, `apps/core/mixins.py`, `apps/core/middlewares.py`
- errandigo backend (NestJS) — `src/shared/services/auth.service.ts`, `src/swagger.ts`, `src/database/ormconfig.ts`
- straqa-api (Django) — `apps/accounts/mixins/login.py`, `apps/core/responses.py`, `compose.yml`
- ASP.NET Core best practices — <https://learn.microsoft.com/aspnet/core/fundamentals/best-practices>
- JWT bearer authentication in ASP.NET Core — <https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication>
- Docker for .NET — <https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/building-net-docker-images>
- EF Core CLI tools — <https://learn.microsoft.com/ef/core/cli/dotnet>
- Swashbuckle / OpenAPI — <https://learn.microsoft.com/aspnet/core/tutorials/web-api-help-pages-using-swagger>
- eShopOnContainers (DDD/CQRS reference) — <https://github.com/dotnet-architecture/eShopOnContainers>
