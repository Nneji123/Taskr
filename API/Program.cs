using Serilog;
using Scalar.AspNetCore;
using API.Data;
using API.Common;
using API.Extensions;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, svc, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    // Register Services via Extensions
    builder.Services
        .AddAppOptions(builder.Configuration)
        .AddAppDbContext(builder.Configuration)
        .AddAppCache(builder.Configuration)
        .AddAppAuth(builder.Configuration)
        .AddAppServices()
        .AddAppRateLimiter()
        .AddAppSwagger(LoadEmbeddedResource)
        .AddAppHealthChecks(builder.Configuration);

    // Sentry
    var sentryDsn = builder.Configuration["SENTRY__DSN"] ?? builder.Configuration["Sentry:Dsn"];
    if (!string.IsNullOrWhiteSpace(sentryDsn))
        SentrySdk.Init(o => { o.Dsn = sentryDsn; o.TracesSampleRate = 0.2; o.Environment = builder.Environment.EnvironmentName; });

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState.Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new { field = e.Key, message = e.Value?.Errors.FirstOrDefault()?.ErrorMessage });
            return new Microsoft.AspNetCore.Mvc.ObjectResult(ApiResponse.Fail("Validation failed", errors)) { StatusCode = 422 };
        };
    });

    // CLI mode: dotnet run -- cli <command> [args...]
    // Must be checked BEFORE builder.Build() to avoid starting Kestrel.
    var cliArgs = args.Where(a => !a.StartsWith("--")).ToArray();
    if (cliArgs.Length >= 1 && cliArgs[0] == "cli")
    {
        var sp = builder.Services.BuildServiceProvider();
        using var cliScope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var cliDispatcher = new API.Common.Cli.CliDispatcher(cliScope.ServiceProvider);
        cliDispatcher.RegisterFromAssembly(typeof(Program).Assembly);
        var exitCode = await cliDispatcher.RunAsync(cliArgs[1..]);
        await Log.CloseAndFlushAsync();
        Environment.Exit(exitCode);
        return;
    }

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/swagger"))
        {
            ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            ctx.Response.Headers["Pragma"] = "no-cache";
            ctx.Response.Headers["Expires"] = "0";
        }
        await next();
    });
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Taskr API v1");
        c.DocumentTitle = "Taskr API — Swagger UI";
        c.DisplayRequestDuration();
    });
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Taskr API")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });

    app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapGet("/", () => Results.Redirect("/scalar/"));

    // Auto-migrate on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    Log.Information("API started");
    await app.RunAsync();
}
catch (Exception ex) { Log.Fatal(ex, "Application terminated unexpectedly"); }
finally { await Log.CloseAndFlushAsync(); }

static string? LoadEmbeddedResource(string resourceName)
{
    var assembly = typeof(Program).Assembly;
    using var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream is null) return null;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
