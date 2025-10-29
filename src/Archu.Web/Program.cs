using Archu.ApiClient.Configuration;
using Archu.ApiClient.Extensions;
using Archu.Ui;
using Archu.Ui.Services;
using Archu.Web;
using Archu.Web.Configuration;
using Archu.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register strongly-typed options and enable post-configuration fallbacks for Aspire service discovery
builder.Services.AddTransient<IPostConfigureOptions<ApiClientOptions>, ApiClientOptionsPostConfigure>();

// Register API Client with Authentication for Blazor WebAssembly using configuration binding
builder.Services.AddApiClientForWasm(builder.Configuration);

// Add authorization for Blazor
builder.Services.AddAuthorizationCore();

// Register feature management to power client-side feature toggles
builder.Services.AddFeatureManagement();
builder.Services.AddScoped<IClientFeatureService, ClientFeatureService>();

// Add shared UI component services from Archu.Ui
builder.Services.AddArchuUi();

await builder.Build().RunAsync();
