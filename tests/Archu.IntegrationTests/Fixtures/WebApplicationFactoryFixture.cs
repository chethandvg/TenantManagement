using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

namespace Archu.IntegrationTests.Fixtures;

public class WebApplicationFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;
    private const string TestJwtSecret = "ThisIsATestSecretKeyForJWTTokenGenerationWithAtLeast32Characters";

    // Custom claim types to match the API authorization policies
    private const string PermissionClaimType = "permission";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();

        // Wait for services to be fully configured
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created with migrations
        await dbContext.Database.MigrateAsync();

        // Initialize Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(_connectionString, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = new[] { "dbo" }
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure test JWT settings FIRST before Infrastructure registration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Register test implementations of ICurrentUser and ITimeProvider
            services.RemoveAll<ICurrentUser>();
            services.RemoveAll<ITimeProvider>();
            
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, TestCurrentUser>();
            services.AddSingleton<ITimeProvider, TestTimeProvider>();

            // Add test database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_connectionString);
            });

            // Override JWT Bearer authentication with test configuration
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = Encoding.UTF8.GetBytes(TestJwtSecret);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidIssuer = "TestIssuer",
                    ValidAudience = "TestAudience",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connectionString);
    }

    /// <summary>
    /// Generates a test JWT token for the specified role with appropriate permission claims.
    /// </summary>
    public Task<string> GetJwtTokenAsync(string role = "User", string userId = "test-user-id", string username = "testuser")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestJwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add permission claims based on role to satisfy authorization policies
        AddPermissionClaimsForRole(claims, role);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Task.FromResult(tokenString);
    }

    /// <summary>
    /// Adds permission claims to the claims list based on the user's role.
    /// This mimics the real authorization system where roles grant specific permissions.
    /// </summary>
    private void AddPermissionClaimsForRole(List<Claim> claims, string role)
    {
        // Permission constants matching the API
        const string productsRead = "products:read";
        const string productsCreate = "products:create";
        const string productsUpdate = "products:update";
        const string productsDelete = "products:delete";

        switch (role.ToUpperInvariant())
        {
            case "USER":
                // Users can only read products
                claims.Add(new Claim(PermissionClaimType, productsRead));
                break;

            case "MANAGER":
                // Managers can read, create, and update products
                claims.Add(new Claim(PermissionClaimType, productsRead));
                claims.Add(new Claim(PermissionClaimType, productsCreate));
                claims.Add(new Claim(PermissionClaimType, productsUpdate));
                break;

            case "ADMINISTRATOR":
            case "ADMIN":
            case "SUPERADMIN":
                // Administrators have all product permissions
                claims.Add(new Claim(PermissionClaimType, productsRead));
                claims.Add(new Claim(PermissionClaimType, productsCreate));
                claims.Add(new Claim(PermissionClaimType, productsUpdate));
                claims.Add(new Claim(PermissionClaimType, productsDelete));
                break;

            default:
                // Guest or unknown role - no permissions
                break;
        }
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Test implementation of ICurrentUser for integration tests.
    /// </summary>
    private sealed class TestCurrentUser : ICurrentUser
    {
        private static readonly string[] RoleClaimTypes =
        {
            ClaimTypes.Role,
            "role",
            "roles",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        private static readonly string[] UserIdClaimTypes =
        {
            ClaimTypes.NameIdentifier,
            "sub",
            "oid",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        };

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates a test current user implementation that mirrors the production behavior by reading the active HTTP context.
        /// </summary>
        public TestCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the authenticated user's identifier by inspecting common claim types.
        /// </summary>
        public string? UserId
        {
            get
            {
                var principal = GetPrincipal();
                if (principal?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                foreach (var claimType in UserIdClaimTypes)
                {
                    var value = principal.FindFirst(claimType)?.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return principal.Identity?.Name;
            }
        }

        /// <summary>
        /// Indicates whether the current HTTP context contains an authenticated principal.
        /// </summary>
        public bool IsAuthenticated => GetPrincipal()?.Identity?.IsAuthenticated == true;

        /// <summary>
        /// Determines if the current user belongs to the supplied role name.
        /// </summary>
        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            var principal = GetPrincipal();
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return principal.IsInRole(role) || GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if the current user belongs to any of the provided roles.
        /// </summary>
        public bool HasAnyRole(params string[] roles)
        {
            if (roles is null || roles.Length == 0)
            {
                return false;
            }

            var roleSet = GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);
            return roles.Any(role => !string.IsNullOrWhiteSpace(role) && roleSet.Contains(role));
        }

        /// <summary>
        /// Gets all role values present on the current principal across supported claim types.
        /// </summary>
        public IEnumerable<string> GetRoles()
        {
            var principal = GetPrincipal();
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return Enumerable.Empty<string>();
            }

            return principal.Claims
                .Where(claim => RoleClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Retrieves the current <see cref="ClaimsPrincipal"/> from the HTTP context.
        /// </summary>
        private ClaimsPrincipal? GetPrincipal() => _httpContextAccessor.HttpContext?.User;
    }

    /// <summary>
    /// Test implementation of ITimeProvider for integration tests.
    /// </summary>
    private sealed class TestTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactoryFixture>
{
}
