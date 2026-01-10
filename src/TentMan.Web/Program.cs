using TentMan.ApiClient.Extensions;
using TentMan.Ui;
using TentMan.Web;
using TentMan.Domain.Constants;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL from Aspire service discovery or configuration
var apiUrl = builder.Configuration["services:api:https:0"]
             ?? builder.Configuration["services:api:http:0"]
             ?? builder.Configuration["ApiClient:BaseUrl"]
             ?? "https://localhost:7123";

// Register API Client with Authentication for Blazor WebAssembly
builder.Services.AddApiClientForWasm(options =>
{
    options.BaseUrl = apiUrl;
    options.TimeoutSeconds = 30;
    options.RetryCount = 3;
    options.EnableDetailedLogging = true;
    options.EnableCircuitBreaker = true;
    options.EnableRetryPolicy = true;
}, authOptions =>
{
    authOptions.AutoAttachToken = true;
    authOptions.UseBrowserStorage = true; // Store tokens in browser localStorage
    authOptions.AutoRefreshToken = true;
});

// Add authorization for Blazor
builder.Services.AddAuthorizationCore(options =>
{
    // Tenant Portal policy - allow Tenant role or explicit permission
    options.AddPolicy("CanViewTenantPortal", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(RoleNames.Tenant) ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == PermissionNames.TenantPortal.View));
    });

    // Property Management policy - allow Admin, Manager, User roles or explicit permission
    options.AddPolicy("CanViewPropertyManagement", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(RoleNames.Administrator) ||
            context.User.IsInRole(RoleNames.Manager) ||
            context.User.IsInRole(RoleNames.User) ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == PermissionNames.PropertyManagement.View));
    });

    // Buildings policy
    options.AddPolicy("CanViewBuildings", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(RoleNames.Administrator) ||
            context.User.IsInRole(RoleNames.Manager) ||
            context.User.IsInRole(RoleNames.User) ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == PermissionNames.Buildings.Read));
    });

    // Tenants policy
    options.AddPolicy("CanViewTenants", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(RoleNames.Administrator) ||
            context.User.IsInRole(RoleNames.Manager) ||
            context.User.IsInRole(RoleNames.User) ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == PermissionNames.Tenants.Read));
    });

    // Leases policy
    options.AddPolicy("CanViewLeases", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(RoleNames.Administrator) ||
            context.User.IsInRole(RoleNames.Manager) ||
            context.User.IsInRole(RoleNames.User) ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == PermissionNames.Leases.Read));
    });
});

// Add shared UI component services from TentMan.Ui
builder.Services.AddTentManUi();

await builder.Build().RunAsync();
