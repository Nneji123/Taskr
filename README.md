# Taskr

A task and project management API. Users own projects, projects contain tasks. JWT auth, rate-limited, owner-scoped, fully documented.

## Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10 (ASP.NET Core) |
| Database | PostgreSQL 16 (via EF Core 10 + Npgsql) |
| Cache | Redis 7 |
| Object storage | S3 / Cloudinary / local filesystem (swappable) |
| Email | SMTP / Resend / ZeptoMail (swappable) |
| API docs | Swashbuckle / OpenAPI 3.0 |
| Observability | Serilog + Sentry |
| Local dev | Docker Compose + Mailpit (catches outbound email) |

## Quick start

```bash
# 1. Copy env template and edit secrets
cp .env.example .env

# 2. Start the stack (Postgres, Redis, Mailpit, API with hot-reload)
make up

# 3. Open the API
make docs              # http://localhost:8080/scalar/v1
make swagger           # http://localhost:8080/swagger (fallback)
make mailpit           # http://localhost:8025 (catches emails)

# 4. Seed an admin user
make seed
```

The API auto-migrates the database on startup.

## Project layout

```
.
├── API/                      # ASP.NET Core 10 web API
│   ├── Common/               # Shared infrastructure (ApiResponse, BaseController, etc.)
│   │   ├── Email/            # Swappable email providers + queue
│   │   ├── Files/            # File upload + storage abstraction
│   │   ├── Storage/          # S3 / Cloudinary / local providers
│   │   ├── Swagger/          # Swagger document filters
│   │   └── Scheduler/        # Base class for hosted background tasks
│   ├── Data/                 # EF Core DbContext, configurations, migrations
│   ├── Features/             # Feature-sliced vertical modules
│   │   ├── Auth/             #   Models, DTOs, controllers, services
│   │   ├── Projects/         #   ...
│   │   └── Tasks/            #   ...
│   ├── Options/              # IOptions<T> binding classes (typed configuration)
│   ├── Cli/                  # Admin CLI commands (run via `dotnet API.dll cli <cmd>`)
│   ├── Properties/           # launchSettings.json
│   ├── Program.cs            # App entry point, DI wiring, middleware pipeline
│   └── appsettings*.json     # Environment-specific config
├── docs/                     # Markdown documentation
├── compose.yml               # Production-shaped compose
├── compose.override.yml      # Local dev: hot-reload + dev env vars
├── Dockerfile                # Multi-stage build → non-root runtime
├── Makefile                  # Dev workflow commands
└── .env.example              # Environment template
```

## Architecture

- **Feature-sliced layout.** Each feature (`Auth`, `Projects`, `Tasks`) owns its `Models/`, `DTOs/`, `Controllers/`, and `Services/` folders. Cross-cutting concerns live under `Common/`.
- **Owner-scoped resources.** Every list/get/update/delete checks `CurrentUser.Id` against the resource's owner. There is no admin role yet.
- **Standard response envelope.** Every response is wrapped in `ApiResponse<T> { success, message, data, errors }`. Validation errors use HTTP 422 with a per-field `errors` list.
- **Rate limiting.** Three policies: `auth-strict` (50 / 5 min per IP), `api-default` (100 / min per user), `write-strict` (30 / min per user).
- **Refresh token rotation.** Refresh tokens are single-use, stored as SHA-256 hashes. Reuse revokes the entire family.
- **Background jobs.** `EmailBackgroundService` consumes the email queue. Other scheduled tasks (e.g. `CleanupExpiredRefreshTokensTask`) implement `BaseScheduledTask` and run on a cron expression.

## API surface

| Tag | Endpoints |
|---|---|
| **Auth** | `POST /v1/auth/register`, `POST /v1/auth/login`, `POST /v1/auth/refresh`, `GET /v1/auth/me`, `POST /v1/auth/password-reset`, `POST /v1/auth/password-reset/confirm`, `POST /v1/auth/change-password` |
| **Projects** | `GET /v1/projects`, `POST /v1/projects`, `GET /v1/projects/{id}`, `PATCH /v1/projects/{id}`, `DELETE /v1/projects/{id}` |
| **Tasks** | `GET /v1/projects/{projectId}/tasks`, `POST /v1/projects/{projectId}/tasks`, `GET /v1/tasks/{id}`, `PATCH /v1/tasks/{id}`, `DELETE /v1/tasks/{id}` |
| **Files** | `POST /v1/files` (multipart), `DELETE /v1/files?url=…` |
| **Health** | `GET /health` |

### API docs

The primary API reference UI is **Scalar** at `/scalar/v1`. Swagger UI is also available at `/swagger` as a fallback.

## Common commands

Run `make help` for the full list. Highlights:

```bash
make up              # Start the stack
make down            # Stop containers (keep data)
make down-v          # Stop + delete volumes (full reset)
make logs-api        # Tail API logs
make rebuild         # Force-rebuild the API image
make psql            # psql shell
make redis-cli       # redis-cli shell
make seed            # Seed default admin
make swagger         # Open Swagger UI
make mailpit         # Open Mailpit
make health          # Curl /health
make spec            # Save OpenAPI spec to ./swagger.json
make up-prod         # Start production-shaped stack (no hot-reload)
```

## Configuration

All configuration is via environment variables (with `.env` in dev). See `.env.example` for the full list. Key ones:

- `DATABASE__CONNECTIONSTRING` — Postgres connection string
- `REDIS__CONNECTIONSTRING` — Redis connection string
- `JWT__SECRET` — HMAC signing key (use `openssl rand -base64 64` to generate)
- `JWT__ISSUER`, `JWT__AUDIENCE` — JWT iss/aud claims
- `STORAGE__PROVIDER` — `local` | `s3` | `cloudinary`
- `EMAIL__PROVIDER` — `smtp` | `resend` | `zeptomail`
- `SENTRY__DSN` — optional, enables Sentry

Use double underscores (`__`) for nested keys, matching ASP.NET Core's env var convention.

## Database migrations

The API auto-migrates on startup. To create a new migration:

```bash
make migration-new name=AddFooField
```

This requires the `dotnet-ef` tool (`dotnet tool install -g dotnet-ef`).

## Documentation

- `docs/running.md` — running the stack locally and in production
- `docs/deployment.md` — production deployment
- `docs/architecture.md` — design decisions and rationale
- `docs/plans/` — design specs for major changes

## License

Proprietary. All rights reserved.
