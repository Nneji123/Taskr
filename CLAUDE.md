# CLAUDE.md — Taskr

Guidance for Claude (and other AI coding agents) working in this repo.

## Project at a glance

- **.NET 10** ASP.NET Core web API, single project under `API/`.
- **Postgres** (EF Core) + **Redis** (cache) + **S3/Cloudinary/local** (storage) + **SMTP/Resend/ZeptoMail** (email) — all swappable via `Options/*` and `Common/Storage/Providers/`, `Common/Email/Providers/`.
- **Feature-sliced layout** under `API/Features/<Name>/{Models,DTOs,Controllers,Services}/`. Cross-cutting code goes under `API/Common/`.
- **Standard response envelope**: every controller returns `ApiResponse<T>` (see `API/Common/ApiResponse.cs`). Use `OkResult` / `CreatedResult` / `DeletedResult` helpers on `BaseController`.

## Build / run

```bash
make up                # Start full stack (Postgres, Redis, Mailpit, API with hot-reload)
make build             # dotnet build API/API.csproj -c Release
make rebuild           # Force-rebuild API image
make spec              # Save OpenAPI spec to ./swagger.json
make docs              # Open http://localhost:8080/scalar/v1 (primary API docs)
make swagger           # Open http://localhost:8080/swagger (fallback)
```

`make help` lists every target. The API auto-migrates the DB on startup.

## Code style

- **No comments unless asked.** Do not add code comments, XML doc comments for new code, or banner comments. The repo has a `<NoWarn>$(NoWarn);1591</NoWarn>` in `API.csproj` precisely to suppress missing-XML-comment warnings — keep it that way unless explicitly adding doc comments for Swagger.
- **Match existing patterns.** Look at neighbouring files before adding new code. Reuse `BaseController` helpers, `ICurrentUser`, `ICacheService`, `ApiResponse<T>`, the rate-limiting policies (`auth-strict`, `api-default`, `write-strict`), and the `[Authorize]` + `[EnableRateLimiting]` attribute pair on controllers.
- **Owner-scoped reads/writes.** All entity access goes through a service that takes `CurrentUser.Id` and checks ownership. Don't add new endpoints that bypass this.
- **DTOs over entities.** Controllers accept/return DTOs from `Features/<X>/DTOs/`, never the EF entity types. Use FluentValidation (`ValidatorsFromAssemblyContaining<Program>()`) for input validation — see `Common/PagedRequest.cs` for the pattern.
- **Use `TypedResults` / `Results.Redirect` / `Results.Json` for minimal endpoints**, but the project mostly uses controllers.

## File locations

| What | Where |
|---|---|
| App entry / DI / middleware | `API/Program.cs` |
| Typed configuration | `API/Options/*Options.cs` |
| EF DbContext, configs, migrations | `API/Data/` |
| Shared base + envelope + helpers | `API/Common/` (incl. `BaseController.cs`, `ApiResponse.cs`, `PagedRequest.cs`, `PagedResult.cs`) |
| Email queue + providers | `API/Common/Email/` |
| File upload + storage providers | `API/Common/Files/`, `API/Common/Storage/` |
| Swagger document filters | `API/Common/Swagger/` |
| Hosted background tasks | `API/Common/Scheduler/`, plus feature-specific tasks in `API/Features/<X>/ScheduledTasks/` |
| CLI commands | `API/Cli/Commands/` and `API/Common/Cli/` |
| Health checks | wired in `Program.cs`; endpoints at `/health` |

## Things to be careful about

- **Don't add `Microsoft.OpenApi` types directly** — Swashbuckle's `AddSwaggerGen` is the source of truth. Modify the generated spec via `IDocumentFilter` / `IOperationFilter`, not by patching the doc at request time.
- **Rate limit policies are named**, not anonymous. Add new policies to the `AddRateLimiter` block in `Program.cs` and reference by name.
- **Storage and email are abstracted.** New providers implement `IStorageService` or `IEmailService` and get registered in the `AddScoped` switch in `Program.cs`.
- **Migrations are checked in.** `API/Data/Migrations/` is committed. New schema changes go through `make migration-new name=...`.
- **Migrations run on container start.** Don't run them manually unless debugging.
- **`API/Common/api.md` is an embedded resource** loaded into the OpenAPI `info.description`. If you add a new top-level section to the API, extend that file.

## Testing / verification

- There is currently **no test project**. Don't add one unless asked.
- To verify a change, run `make build` and `make spec` to inspect the generated OpenAPI for any DTO/controller changes. Reload `http://localhost:8080/swagger` (hard refresh — the spec is now served with `Cache-Control: no-store`) to confirm summaries/descriptions render.

## Out-of-scope unless asked

- Authentication providers (Google, GitHub, etc.)
- Admin role / RBAC
- Webhooks
- Real-time push (SignalR, WebSockets)
- Multi-tenancy
