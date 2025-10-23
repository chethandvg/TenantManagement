using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Archu.Web;
using Archu.Ui;
using Archu.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Archu.UI (includes MudBlazor services)
builder.Services.AddArchuUi();

// Register application services following clean architecture
builder.Services.AddScoped<IProductService, ProductService>();

await builder.Build().RunAsync();
