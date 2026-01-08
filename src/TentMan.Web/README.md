# TentMan.Web

The Blazor Web host application that serves the TentMan user interface.

---

## 📁 Folder Structure

```
TentMan.Web/
├── App.razor                  # Root component
├── Program.cs                 # Application entry point
├── Properties/
│   └── launchSettings.json
├── wwwroot/                   # Static assets
│   ├── css/
│   └── favicon.ico
├── _Imports.razor             # Global using statements
└── TentMan.Web.csproj
```

---

## 🎯 Purpose

The Web project:
- Hosts the Blazor application
- References TentMan.Ui component library
- Configures authentication state
- Sets up API client integration

---

## 📋 Coding Guidelines

### Program.cs Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add UI components
builder.Services.AddTentManUi();

// Add API client
builder.Services.AddApiClientForServer(builder.Configuration);

// Add Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
```

### File Size Limits

| File Type | Limit | Action |
|-----------|-------|--------|
| Program.cs | 300 lines max | Extract to extensions |
| App.razor | 50 lines max | Keep minimal |

---

## 🎨 Component Guidelines

This project hosts components from **TentMan.Ui**. When adding new pages or components:

1. **Prefer adding to TentMan.Ui** for reusability
2. **Use code-behind pattern** for all components with logic
3. **Keep markup under 200 lines**

### Page Structure

```razor
@page "/my-page"
@attribute [Authorize]
@inherits MyPageBase

<PageTitle>My Page</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">My Page</MudText>
    
    @if (IsLoading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MyComponent Data="@Data" />
    }
</MudContainer>
```

### Code-Behind

```csharp
namespace TentMan.Web.Pages;

public partial class MyPage : ComponentBase
{
    [Inject] private IMyService MyService { get; set; } = null!;
    
    private bool IsLoading { get; set; }
    private MyData? Data { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        Data = await MyService.GetDataAsync();
        IsLoading = false;
    }
}
```

---

## 🔗 Dependencies

- **TentMan.Ui**: UI component library
- **TentMan.ApiClient**: API client
- **TentMan.ServiceDefaults**: Aspire defaults
- **MudBlazor**: UI framework

---

## 🚀 Running the Application

```bash
cd src/TentMan.Web
dotnet run
```

Or with Aspire:
```bash
cd src/TentMan.AppHost
dotnet run
```

---

## ✅ Checklist for New Pages

- [ ] Add to TentMan.Ui if reusable, otherwise here
- [ ] Use code-behind pattern
- [ ] Keep markup under 200 lines
- [ ] Add proper authorization
- [ ] Follow MudBlazor patterns

---

**Last Updated**: 2026-01-08  
**Maintainer**: TentMan Development Team
