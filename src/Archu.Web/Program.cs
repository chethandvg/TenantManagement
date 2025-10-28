using Archu.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Archu.ApiClient.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Archu.Ui;

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
builder.Services.AddAuthorizationCore();

// Add shared UI component services from Archu.Ui
builder.Services.AddArchuUi();

await builder.Build().RunAsync();
