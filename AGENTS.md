# AGENTS.md — Taskr

Coordination notes for AI coding agents working in parallel on the Taskr API.

## Repo shape (read this first)

Single .NET 10 solution, one project (`API/API.csproj`), all source under `API/`. There is no monorepo of separate services — every feature lives side-by-side and ships in one binary.

```
API/
├── Program.cs              ← composition root
├── API.csproj
├── Common/                 ← cross-cutting (shared, NOT feature-specific)
├── Data/                   ← EF Core, migrations
├── Features/               ← vertical slices
│   ├── Auth/
│   ├── Projects/
│   └── Tasks/
├── Options/                ← typed IOptions<T> classes
└── Cli/                    ← admin CLI commands
```

When you are about to touch a file, `cd` into the right folder and look at the neighbours first. The patterns are tight and consistent — match them.

## Ground rules for parallel work

1. **One agent per feature, one agent per file.** Don't have two agents editing the same controller, service, or DbContext concurrently. If two features both need to extend a shared base class (`BaseController`, `BaseModel`), sequence the changes.
2. **Migrations are serial.** EF Core migrations must be applied in order. Only one agent should run `make migration-new` at a time. The generated file under `API/Data/Migrations/<timestamp>_<Name>.cs` is the source of truth — commit it.
3. **Don't edit `API/Common/` from a feature agent.** Cross-cutting changes belong to the agent that owns `Common/`. If you need a new helper there, request it or do it yourself in a clearly-scoped commit.
4. **DI registration order is not load-bearing** but be tidy: add new `IOptions<>` bindings next to the existing block in `Program.cs`, new services next to their feature's existing registrations, new rate-limit policies inside the `AddRateLimiter` block.

## Feature ownership map

| Agent | Owns | Allowed to touch |
|---|---|---|
| `auth-agent` | `Features/Auth/**` | `Common/BaseController`, `Common/ApiResponse`, `Data/AppDbContext.cs` (for auth-related changes) |
| `projects-agent` | `Features/Projects/**` | `Data/AppDbContext.cs`, `Data/Configurations/EntityConfigurations.cs` |
| `tasks-agent` | `Features/Tasks/**` | `Data/AppDbContext.cs`, `Data/Configurations/EntityConfigurations.cs` |
| `files-agent` | `Common/Files/**`, `Common/Storage/**` | `Features/*/DTOs/*Requests.cs` if a new file field is needed |
| `platform-agent` | `Program.cs`, `Common/Scheduler/**`, `Common/Cli/**`, `Common/Email/**`, `Options/**`, `Data/Migrations/**` | any |

If you need to break ownership, document why in the commit message.

## Safe boundaries

These files are read-mostly — read them, but coordinate before changing them:

- `API/Program.cs` — every agent may add a registration here. No agent should remove or rename existing ones without checking.
- `API/API.csproj` — only `platform-agent` should add/remove package references. Announce it.
- `API/Common/ApiResponse.cs` — only `platform-agent` changes the envelope shape.
- `API/Common/api.md` — content is loaded into the OpenAPI `info.description`. Multiple agents can edit it; resolve merge conflicts by hand.
- `compose.yml`, `compose.override.yml`, `Dockerfile` — `platform-agent` only.

## Conventions every agent should know

- **No comments unless asked.** The repo suppresses CS1591 in `API.csproj` for a reason. If you need a doc comment for Swagger to render it, that's the one exception — keep it `<summary>` + `<remarks>` and nothing else.
- **DTOs go in `Features/<X>/DTOs/*Requests.cs` and `*Responses.cs`.** One file per direction per feature is the rule. `*ListQuery` types share a base via `PagedRequest`.
- **Services are interface-first.** A feature has `IXxxService` in `Services/` and `XxxService` next to it. Inject the interface in the controller constructor.
- **Owner-scoping is mandatory.** Every read/write path through a service must take `Guid userId` (or `CurrentUser.Id`) and check ownership. No service should be callable without a user.
- **All responses are `ApiResponse<T>`.** Use `OkResult`/`CreatedResult`/`DeletedResult` from `BaseController`. Don't return raw `Ok(...)` / `NotFound(...)`.
- **Rate limit every controller.** `[EnableRateLimiting("api-default")]` on the class for reads, `"write-strict"` on `POST`/`PATCH`/`DELETE` actions, `"auth-strict"` on `AuthController`.
- **XML doc comments on public action methods and DTO properties** are encouraged — they become Swagger UI summaries/descriptions. This is the only comment type that's actively used.

## Build / verify loop

After your change, before reporting done:

```bash
make build                # must succeed
make spec                 # dump swagger.json — sanity-check for missing fields, bad routes
# (optional) curl -s http://localhost:5001/swagger/v1/swagger.json | python3 -m json.tool | head
make health               # API still up
```

If you changed a DTO or controller, the diff in `swagger.json` should be small and reviewable.

## Common parallelisable tasks

- Adding a new endpoint to an existing feature: 1 file (controller) + 0–1 new DTO file + a `ProducesResponseType` line. No cross-feature coordination needed.
- Adding a new optional field to an existing DTO: 1 file. No migration. Safe to parallelise across features.
- Adding a new feature: feature-agent gets a new folder under `Features/`, plus DI registration in `Program.cs` (ask `platform-agent` for the exact insertion point).
- Adding a new migration: serial. Coordinate via the migration filename timestamp.

## When you finish

1. `make build` clean
2. No stray comments or commented-out code
3. If you touched the OpenAPI surface, confirm `make spec` looks right
4. Don't commit unless explicitly asked
