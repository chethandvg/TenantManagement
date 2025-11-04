using System.Text;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Abstractions.Repositories;
using Archu.Domain.ValueObjects;
using Archu.Infrastructure.Authentication;
using Archu.Infrastructure.Contentful;
using Archu.Infrastructure.Persistence;
using Archu.Infrastructure.Repositories;
using Archu.Infrastructure.Time;
using Contentful.Core;
using Contentful.Core.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Archu.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Follows Clean Architecture principles with dependency inversion.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Add database context
        services.AddDatabase(configuration);

        // Add authentication services
        services.AddAuthenticationServices(configuration, environment);

        // Add Contentful services
        services.AddContentfulServices(configuration);

        // Add repositories
        services.AddRepositories();

        // Add other infrastructure services
        services.AddInfrastructureServices();

        return services;
    }

    /// <summary>
    /// Configures the database context with SQL Server.
    /// </summary>
    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sql")
            ?? configuration.GetConnectionString("archudb")
            ?? throw new InvalidOperationException("Database connection string not configured");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                }));

        return services;
    }

    /// <summary>
    /// Configures JWT authentication services.
    /// </summary>
    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Register authentication services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register password validator
        services.AddScoped<IPasswordValidator, PasswordValidator>();

        // Configure JWT options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Configure password policy options
        services.Configure<PasswordPolicyOptions>(options =>
        {
            configuration.GetSection(PasswordPolicyOptions.SectionName).Bind(options);
            options.Validate();
        });

        // Validate JWT options on startup
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing");

        jwtOptions.Validate();

        // Configure JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);

            options.SaveToken = true;
            options.RequireHttpsMetadata = !environment.IsDevelopment(); // Allow HTTP in development

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5), // Allow 5-minute tolerance for clock skew

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            // Configure event handlers for better logging and debugging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtTokenService>>();

                    logger.LogWarning(
                        context.Exception,
                        "JWT authentication failed: {Message}",
                        context.Exception.Message);

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtTokenService>>();

                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    logger.LogDebug("JWT token validated successfully for user: {UserId}", userId);

                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtTokenService>>();

                    logger.LogWarning(
                        "JWT authentication challenge: {Error} - {ErrorDescription}",
                        context.Error,
                        context.ErrorDescription);

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Registers repository implementations.
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers other infrastructure services.
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITimeProvider, SystemTimeProvider>();

        return services;
    }

    /// <summary>
    /// Configures Contentful CMS services.
    /// </summary>
    private static IServiceCollection AddContentfulServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Contentful options
        services.Configure<ContentfulSettings>(configuration.GetSection(ContentfulSettings.SectionName));

        // Get Contentful options for validation
        var contentfulSettings = configuration
            .GetSection(ContentfulSettings.SectionName)
            .Get<ContentfulSettings>();

        // Only configure Contentful if credentials are provided
        // This allows the app to run without Contentful configured
        if (contentfulSettings != null &&
            !string.IsNullOrWhiteSpace(contentfulSettings.SpaceId) &&
            !string.IsNullOrWhiteSpace(contentfulSettings.DeliveryApiKey))
        {
            contentfulSettings.Validate();

            // Create HttpClient for Contentful
            var httpClient = new HttpClient();

            // Configure Contentful client options
            var contentfulClientOptions = new ContentfulOptions
            {
                DeliveryApiKey = contentfulSettings.DeliveryApiKey,
                SpaceId = contentfulSettings.SpaceId,
                Environment = contentfulSettings.Environment,
                UsePreviewApi = false
            };

            // Register ContentfulClient as singleton
            services.AddSingleton<IContentfulClient>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ContentfulClient>>();
                logger.LogInformation(
                    "Initializing Contentful client for Space: {SpaceId}, Environment: {Environment}",
                    contentfulSettings.SpaceId,
                    contentfulSettings.Environment);

                return new ContentfulClient(httpClient, contentfulClientOptions);
            });

            // Register Contentful service
            services.AddScoped<IContentfulService, ContentfulService>();
        }
        else
        {
            // Register a null object implementation if Contentful is not configured
            // This allows the app to run without Contentful configured
            services.AddScoped<IContentfulService, NullContentfulService>();
        }

        return services;
    }
}
