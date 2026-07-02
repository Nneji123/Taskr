FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY API/API.csproj API/
RUN dotnet restore API/API.csproj
COPY API/ API/
RUN dotnet publish API/API.csproj -c Release -o /app /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for HEALTHCHECK (not present in base image), then clean up apt cache
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# The base image already provides a non-root `app` user. Just create the
# DataProtection keys directory and chown it.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app /app

COPY --from=build --chown=app:app /app .

USER app

ENV ASPNETCORE_HTTP_PORTS=5001
ENV DOTNET_EnableDiagnostics=0

EXPOSE 5001

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:5001/health || exit 1

ENTRYPOINT ["dotnet", "API.dll"]
