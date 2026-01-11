using System.Text;
using Azure.Storage.Blobs;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Abstractions.Storage;
using TentMan.Application.BackgroundJobs;
using TentMan.Domain.ValueObjects;
using TentMan.Infrastructure.Authentication;
using TentMan.Infrastructure.Persistence;
using TentMan.Infrastructure.Repositories;
using TentMan.Infrastructure.Storage;
using TentMan.Infrastructure.Time;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace TentMan.Infrastructure;

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

        // Add repositories
        services.AddRepositories();

        // Add storage services
        services.AddStorageServices(configuration);

        // Add other infrastructure services
        services.AddInfrastructureServices();

        // Add Hangfire for background jobs
        services.AddHangfireServices(configuration);

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
            ?? configuration.GetConnectionString("tentmandb")
            ?? throw new InvalidOperationException("Database connection string not configured");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            // Register audit interceptor
            var currentUser = sp.GetRequiredService<ICurrentUser>();
            var timeProvider = sp.GetRequiredService<ITimeProvider>();
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            var auditInterceptor = new TentMan.Infrastructure.Interceptors.AuditLoggingInterceptor(
                currentUser, 
                timeProvider, 
                httpContextAccessor);

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
                })
                .AddInterceptors(auditInterceptor);
        });

        // Register IApplicationDbContext for services that need to persist changes
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

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
    /// Registers storage services (Azure Blob Storage).
    /// </summary>
    private static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Azure Blob Storage options
        services.Configure<AzureBlobStorageOptions>(
            configuration.GetSection(AzureBlobStorageOptions.SectionName));

        // Register BlobServiceClient
        services.AddSingleton(sp =>
        {
            var options = configuration
                .GetSection(AzureBlobStorageOptions.SectionName)
                .Get<AzureBlobStorageOptions>();

            if (string.IsNullOrWhiteSpace(options?.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is not configured. " +
                    "Please set AzureBlobStorage:ConnectionString in configuration.");
            }

            return new BlobServiceClient(options.ConnectionString);
        });

        // Register file storage service
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        return services;
    }

    /// <summary>
    /// Registers repository implementations.
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ILeaseRepository, LeaseRepository>();
        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<ITenantInviteRepository, TenantInviteRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Billing repositories
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceRunRepository, InvoiceRunRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IChargeTypeRepository, ChargeTypeRepository>();
        services.AddScoped<ILeaseBillingSettingRepository, LeaseBillingSettingRepository>();
        services.AddScoped<ILeaseRecurringChargeRepository, LeaseRecurringChargeRepository>();
        services.AddScoped<IUtilityRatePlanRepository, UtilityRatePlanRepository>();
        services.AddScoped<IUtilityStatementRepository, UtilityStatementRepository>();
        services.AddScoped<INumberSequenceRepository, NumberSequenceRepository>();

        return services;
    }

    /// <summary>
    /// Registers other infrastructure services.
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITimeProvider, SystemTimeProvider>();
        services.AddScoped<TentMan.Application.PropertyManagement.Services.IOwnershipService, TentMan.Application.PropertyManagement.Services.OwnershipService>();
        services.AddScoped<TentMan.Application.Abstractions.Security.IDataMaskingService, TentMan.Infrastructure.Security.DataMaskingService>();
        
        // Billing services
        services.AddScoped<TentMan.Application.Abstractions.Billing.IRentCalculationService, TentMan.Application.Billing.Services.RentCalculationService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IRecurringChargeCalculationService, TentMan.Application.Billing.Services.RecurringChargeCalculationService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IUtilityCalculationService, TentMan.Application.Billing.Services.UtilityCalculationService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IInvoiceNumberGenerator, TentMan.Application.Billing.Services.InvoiceNumberGenerator>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.ICreditNoteNumberGenerator, TentMan.Application.Billing.Services.CreditNoteNumberGenerator>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IInvoiceGenerationService, TentMan.Application.Billing.Services.InvoiceGenerationService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IInvoiceRunService, TentMan.Application.Billing.Services.InvoiceRunService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.IInvoiceManagementService, TentMan.Application.Billing.Services.InvoiceManagementService>();
        services.AddScoped<TentMan.Application.Abstractions.Billing.ICreditNoteService, TentMan.Application.Billing.Services.CreditNoteService>();

        // Background jobs
        services.AddScoped<MonthlyRentGenerationJob>();
        services.AddScoped<UtilityBillingJob>();

        return services;
    }

    /// <summary>
    /// Configures Hangfire for background job processing.
    /// </summary>
    private static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sql")
            ?? configuration.GetConnectionString("tentmandb")
            ?? throw new InvalidOperationException("Database connection string not configured for Hangfire");

        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "Hangfire"
            }));

        // Resolve Hangfire server options from configuration with sensible defaults
        var hangfireSection = configuration.GetSection("Hangfire");
        var workerCount = hangfireSection.GetValue<int?>("WorkerCount") ?? 5;
        var serverName = hangfireSection.GetValue<string>("ServerName");
        if (string.IsNullOrWhiteSpace(serverName))
        {
            serverName = $"{Environment.MachineName}:{Guid.NewGuid()}";
        }

        // Add the processing server as IHostedService
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount; // Number of concurrent job processors
            options.ServerName = serverName;
        });

        return services;
    }
}
