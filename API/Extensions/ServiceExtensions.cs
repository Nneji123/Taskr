using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using API.Common;
using API.Common.Email;
using API.Common.Email.Providers;
using API.Common.Storage;
using API.Common.Storage.Providers;
using API.Data;
using API.Features.Auth.ScheduledTasks;
using API.Features.Auth.Services;
using API.Features.Projects.Services;
using API.Features.Tasks.Services;
using API.Options;

namespace API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<RedisOptions>().Bind(configuration.GetSection(RedisOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        // Register known email templates
        EmailTemplateRegistry.RegisterTemplates(
            FeatureEmailTemplates.Auth.Welcome,
            FeatureEmailTemplates.Auth.NewLogin,
            FeatureEmailTemplates.Auth.PasswordReset);

        return services;
    }

    public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
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

        return services;
    }

    public static IServiceCollection AddAppCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"]);
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName);
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

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Data protection + encryption
        services.AddDataProtection();
        services.AddSingleton<IDataEncryptor>(sp =>
        {
            var protector = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>().CreateProtector("taskr-pii");
            return new DataProtectionEncryptor(protector);
        });

        // App Services DI
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProjectsService, ProjectsService>();
        services.AddScoped<ITasksService, TasksService>();

        // Email Services
        services.AddScoped(static sp =>
        {
            var emailOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
            return emailOptions.Provider.ToLower() switch
            {
                "resend" => (IEmailService)sp.GetRequiredService<ResendEmailService>(),
                "zeptomail" => sp.GetRequiredService<ZeptoMailEmailService>(),
                _ => sp.GetRequiredService<SmtpEmailService>(),
            };
        });
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<ResendEmailService>();
        services.AddScoped<ZeptoMailEmailService>();

        // Background email queue
        services.AddSingleton<IEmailQueue, EmailQueue>();
        services.AddHostedService<EmailBackgroundService>();

        // Storage Services
        services.AddScoped<LocalStorageService>();
        services.AddScoped<S3StorageService>();
        services.AddScoped<CloudinaryStorageService>();
        services.AddScoped<FileURLValidator>();
        services.AddScoped<IStorageService>(sp =>
        {
            var storageOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
            return storageOptions.Provider.ToLower() switch
            {
                "s3" => sp.GetRequiredService<S3StorageService>(),
                "cloudinary" => sp.GetRequiredService<CloudinaryStorageService>(),
                _ => sp.GetRequiredService<LocalStorageService>(),
            };
        });

        // Scheduled tasks
        services.AddHostedService<CleanupExpiredRefreshTokensTask>();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    public static IServiceCollection AddAppRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
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

        return services;
    }

    public static IServiceCollection AddAppSwagger(this IServiceCollection services, Func<string, string?> loadEmbeddedResource)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Taskr API",
                Version = "v1.0.0",
                Description = loadEmbeddedResource("API.Common.api.md") ?? "Taskr API",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@taskr.com"
                }
            });

            var xmlFiles = new[]
            {
                $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml",
                $"{typeof(ApiResponse<>).Assembly.GetName().Name}.xml"
            };
            foreach (var xmlFile in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            c.TagActionsBy(api =>
            {
                var controllerName = api.ActionDescriptor?.RouteValues?["controller"];
                return string.IsNullOrEmpty(controllerName) ? new[] { "API" } : new[] { controllerName };
            });

            c.DocumentFilter<API.Common.Swagger.TagDescriptionsDocumentFilter>();
            c.ParameterFilter<API.Common.Swagger.CamelCaseQueryParameterFilter>();
            c.OrderActionsBy(api => api.RelativePath);

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });
            c.OperationFilter<API.Common.Swagger.SecurityRequirementsOperationFilter>();
        });

        return services;
    }

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value.ConnectionString)
            .AddRedis(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value.ConnectionString);

        return services;
    }
}
