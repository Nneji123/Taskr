using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

using Serilog;
using Npgsql;
using API.Data;
using API.Common;
using API.Common.Email;
using API.Options;
using API.Features.Auth.Services;
using API.Features.Projects.Services;
using API.Features.Tasks.Services;
using Scalar.AspNetCore;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, svc, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    // Options
    builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddOptions<DatabaseOptions>().Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddOptions<RedisOptions>().Bind(builder.Configuration.GetSection(RedisOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddOptions<EmailOptions>().Bind(builder.Configuration.GetSection(EmailOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddOptions<StorageOptions>().Bind(builder.Configuration.GetSection(StorageOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

    // Register known email templates so they are eagerly validated
    EmailTemplateRegistry.RegisterTemplates(
        FeatureEmailTemplates.Auth.Welcome,
        FeatureEmailTemplates.Auth.NewLogin,
        FeatureEmailTemplates.Auth.PasswordReset);

    // Data protection + encryption
    builder.Services.AddDataProtection();
    builder.Services.AddSingleton<IDataEncryptor>(sp =>
    {
        var protector = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>().CreateProtector("mercadotnet-pii");
        return new DataProtectionEncryptor(protector);
    });

    // DB
    builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    {
        var dbOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbOptions.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        options.UseNpgsql(dataSource, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName);
            npgsql.EnableRetryOnFailure(3);
        });
    });

    // Cache
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = builder.Configuration.GetSection(RedisOptions.SectionName)["ConnectionString"]);
    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    // Auth
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(jwtOptions =>
        {
            var jwt = builder.Configuration.GetSection(JwtOptions.SectionName);
            jwtOptions.MapInboundClaims = false;
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt["Issuer"],
                ValidateAudience = true, ValidAudience = jwt["Audience"],
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    // DI
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProjectsService, ProjectsService>();
    builder.Services.AddScoped<ITasksService, TasksService>();
    builder.Services.AddScoped(static sp =>
    {
        var emailOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<API.Options.EmailOptions>>().Value;
        return emailOptions.Provider.ToLower() switch
        {
            "resend" => (API.Common.Email.IEmailService)sp.GetRequiredService<API.Common.Email.Providers.ResendEmailService>(),
            "zeptomail" => sp.GetRequiredService<API.Common.Email.Providers.ZeptoMailEmailService>(),
            _ => sp.GetRequiredService<API.Common.Email.Providers.SmtpEmailService>(),
        };
    });
    builder.Services.AddScoped<API.Common.Email.Providers.SmtpEmailService>();
    builder.Services.AddScoped<API.Common.Email.Providers.ResendEmailService>();
    builder.Services.AddScoped<API.Common.Email.Providers.ZeptoMailEmailService>();

    // Background email queue (replaces ad-hoc fire-and-forget Task.Run)
    builder.Services.AddSingleton<API.Common.Email.IEmailQueue, API.Common.Email.EmailQueue>();
    builder.Services.AddHostedService<API.Common.Email.EmailBackgroundService>();
    builder.Services.AddScoped<API.Common.Storage.Providers.LocalStorageService>();
    builder.Services.AddScoped<API.Common.Storage.Providers.S3StorageService>();
    builder.Services.AddScoped<API.Common.Storage.Providers.CloudinaryStorageService>();
    builder.Services.AddScoped<API.Common.Storage.FileURLValidator>();
    builder.Services.AddScoped<API.Common.Storage.IStorageService>(sp =>
    {
        var storageOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<API.Options.StorageOptions>>().Value;
        return storageOptions.Provider.ToLower() switch
        {
            "s3" => sp.GetRequiredService<API.Common.Storage.Providers.S3StorageService>(),
            "cloudinary" => sp.GetRequiredService<API.Common.Storage.Providers.CloudinaryStorageService>(),
            _ => sp.GetRequiredService<API.Common.Storage.Providers.LocalStorageService>(),
        };
    });

    // Scheduled tasks
    builder.Services.AddHostedService<API.Features.Auth.ScheduledTasks.CleanupExpiredRefreshTokensTask>();

    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.AddPolicy("auth-strict", ctx => RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 50, Window = TimeSpan.FromMinutes(5) }));
        options.AddPolicy("api-default", ctx => RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
        options.AddPolicy("write-strict", ctx => RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirst("sub")?.Value ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.StatusCode = 429;
            await ctx.HttpContext.Response.WriteAsJsonAsync(ApiResponse.Fail("Too many requests. Please try again later."), ct);
        };
    });

    // OpenAPI
    builder.Services.AddOpenApi(o =>
    {
        o.AddDocumentTransformer((document, context, ct) =>
        {
            var apiDescription = LoadEmbeddedResource("API.Common.api.md") ?? "Mercadotnet API";
            document.Info = new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "Mercadotnet API",
                Version = "v1.0.0",
                Description = apiDescription,
                Contact = new Microsoft.OpenApi.OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@mercadotnet.com"
                }
            };
            return Task.CompletedTask;
        });
    });

    // Sentry
    var sentryDsn = builder.Configuration["SENTRY__DSN"] ?? builder.Configuration["Sentry:Dsn"];
    if (!string.IsNullOrWhiteSpace(sentryDsn))
        SentrySdk.Init(o => { o.Dsn = sentryDsn; o.TracesSampleRate = 0.2; o.Environment = builder.Environment.EnvironmentName; });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value.ConnectionString)
        .AddRedis(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value.ConnectionString);

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

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(o => o.WithTitle("API API"));
    }

    app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));

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
