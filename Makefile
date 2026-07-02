# ──────────────────────────────────────────────────────────────────────────────
# Taskr — make targets
# Run `make help` (default) for a list of available targets.
# ──────────────────────────────────────────────────────────────────────────────

SHELL         := /bin/bash
COMPOSE       := docker compose
COMPOSE_PROD  := docker compose -f compose.yml
PROJECT       := taskr
API_SERVICE   := api
API_PROJECT   := API/API.csproj
DB_SERVICE    := postgres

.DEFAULT_GOAL := help

.PHONY: help
help: ## Show this help message
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-18s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

# ── Lifecycle ─────────────────────────────────────────────────────────────────

.PHONY: up
up: ## Start the full stack (uses compose.override.yml for dev with hot-reload)
	$(COMPOSE) up -d
	@$(MAKE) --no-print-directory status

.PHONY: down
down: ## Stop and remove containers (keeps volumes)
	$(COMPOSE) down

.PHONY: down-v
down-v: ## Stop, remove containers, AND delete volumes (full reset)
	$(COMPOSE) down -v

.PHONY: restart
restart: ## Restart the API service
	$(COMPOSE) restart $(API_SERVICE)

.PHONY: rebuild
rebuild: ## Rebuild the API image and recreate its container
	$(COMPOSE) build $(API_SERVICE) --no-cache
	$(COMPOSE) up -d $(API_SERVICE)
	@$(MAKE) --no-print-directory logs-api

.PHONY: status
status: ## Show status of running services
	@docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" --filter "name=$(PROJECT)-"

.PHONY: logs
logs: ## Tail logs from all services
	$(COMPOSE) logs -f --tail=100

.PHONY: logs-api
logs-api: ## Tail logs from the API service only
	$(COMPOSE) logs -f --tail=100 $(API_SERVICE)

.PHONY: clean
clean: down ## Remove containers, networks, and dangling images
	docker image prune -f

.PHONY: nuke
nuke: down-v clean ## Full reset: remove containers, volumes, and dangling images
	docker volume prune -f

# ── Build & Test ──────────────────────────────────────────────────────────────

.PHONY: build
build: ## Build the .NET solution in Release mode
	dotnet build $(API_PROJECT) -c Release

.PHONY: build-debug
build-debug: ## Build the .NET solution in Debug mode
	dotnet build $(API_PROJECT) -c Debug

.PHONY: restore
restore: ## Restore NuGet packages
	dotnet restore $(API_PROJECT)

.PHONY: test
test: ## Run the test suite
	dotnet test --no-build -c Release

.PHONY: format
format: ## Format C# code (requires dotnet-format)
	@if command -v dotnet-format >/dev/null 2>&1; then \
		dotnet format $(API_PROJECT); \
	else \
		echo "dotnet-format not installed. Install with: dotnet tool install -g dotnet-format"; \
	fi

# ── Database ──────────────────────────────────────────────────────────────────

.PHONY: migrate
migrate: ## Apply pending EF migrations (auto-runs on container start)
	$(COMPOSE) exec $(API_SERVICE) dotnet API.dll cli migrate

.PHONY: migration-new
migration-new: ## Create a new EF migration (usage: make migration-new name=AddFoo)
	@if [ -z "$(name)" ]; then echo "Usage: make migration-new name=AddFoo"; exit 1; fi
	dotnet ef migrations add $(name) --project $(API_PROJECT) --startup-project $(API_PROJECT)

.PHONY: psql
psql: ## Open a psql shell in the Postgres container
	$(COMPOSE) exec $(DB_SERVICE) psql -U $$POSTGRES_USER -d $$POSTGRES_DB

.PHONY: db-reset
db-reset: ## Drop and recreate the Postgres volume (destroys all data)
	$(COMPOSE) stop $(API_SERVICE)
	$(COMPOSE) rm -f $(DB_SERVICE)
	docker volume rm $(PROJECT)_pgdata 2>/dev/null || true
	$(COMPOSE) up -d

# ── CLI & Debug ───────────────────────────────────────────────────────────────

.PHONY: seed
seed: ## Seed the default admin user
	$(COMPOSE) exec $(API_SERVICE) dotnet API.dll cli seed:admin

.PHONY: cli
cli: ## Open an interactive shell in the API container
	$(COMPOSE) exec $(API_SERVICE) /bin/bash

.PHONY: cli-list
cli-list: ## List available CLI commands
	$(COMPOSE) exec $(API_SERVICE) dotnet API.dll cli

.PHONY: redis-cli
redis-cli: ## Open a redis-cli shell in the Redis container
	$(COMPOSE) exec redis redis-cli

.PHONY: mailpit
mailpit: ## Open Mailpit (email catcher) in the default browser
	@open http://localhost:8025 2>/dev/null || xdg-open http://localhost:8025 2>/dev/null || echo "Mailpit: http://localhost:8025"

.PHONY: scalar
scalar: ## Open Scalar API Reference in the default browser
	@open http://localhost:5001/scalar/ 2>/dev/null || xdg-open http://localhost:5001/scalar/ 2>/dev/null || echo "Scalar: http://localhost:5001/scalar/"

.PHONY: swagger
swagger: ## Open Swagger UI in the default browser
	@open http://localhost:5001/swagger 2>/dev/null || xdg-open http://localhost:5001/swagger 2>/dev/null || echo "Swagger UI: http://localhost:5001/swagger"

.PHONY: docs
docs: ## Open API docs (Scalar) in the default browser
	@$(MAKE) --no-print-directory scalar

.PHONY: health
health: ## Curl the health endpoint
	@curl -s http://localhost:5001/health && echo

.PHONY: spec
spec: ## Fetch the OpenAPI spec (pretty-printed) and save to ./swagger.json
	@curl -s http://localhost:5001/swagger/v1/swagger.json | python3 -m json.tool > swagger.json
	@echo "Saved to swagger.json"

# ── Production-like ───────────────────────────────────────────────────────────

.PHONY: up-prod
up-prod: ## Start the production stack (no compose.override, no hot-reload)
	$(COMPOSE_PROD) up -d --build
	@$(MAKE) --no-print-directory status

.PHONY: down-prod
down-prod: ## Stop the production stack
	$(COMPOSE_PROD) down

# ── Local .NET (no Docker) ────────────────────────────────────────────────────

.PHONY: run-local
run-local: ## Run the API locally with `dotnet run` (requires local Postgres + Redis)
	ASPNETCORE_ENVIRONMENT=Development dotnet run --project $(API_PROJECT) --launch-profile http
