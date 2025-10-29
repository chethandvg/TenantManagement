# Busy and Error Workflow

The `BusyBoundary` component and accompanying `UiState`/`BusyState` services provide a consistent way to surface loading and error feedback across pages. The boundary wraps MudBlazor primitives (progress indicator, alert, retry button) so application code can focus on business logic.

## Register Services

`BusyBoundary` relies on the `UiState` container, which is registered automatically when you call `AddArchuUi`:

```csharp
builder.Services.AddArchuUi();
```

## Basic Usage

Wrap your page content with `<BusyBoundary>` and trigger busy/error transitions through the injected `UiState` service:

```razor
@page "/products"
@inject UiState UiState
@inject IProductsApiClient ProductsClient

<PageTitle>Products</PageTitle>

<MudText Typo="Typo.h3" GutterBottom="true">Products</MudText>

<BusyBoundary Retry="LoadProductsAsync">
    @if (products == null || !products.Any())
    {
        <MudAlert Severity="Severity.Info">No products found.</MudAlert>
    }
    else
    {
        <!-- product markup -->
    }
</BusyBoundary>
```

```csharp
private async Task LoadProductsAsync()
{
    using var busyScope = UiState.Busy.Begin("Loading products...");
    UiState.Busy.ClearError();

    try
    {
        var response = await ProductsClient.GetProductsAsync(pageNumber: 1, pageSize: 50);

        if (response.Success && response.Data is { Items: { } items })
        {
            products = items;
            UiState.Busy.ClearError();
        }
        else
        {
            UiState.Busy.SetError(response.Message ?? "Failed to load products.");
        }
    }
    catch (Exception ex)
    {
        UiState.Busy.SetError($"Error loading products: {ex.Message}");
    }
}
```

When `BusyState.IsBusy` is true, the boundary shows a `MudProgressCircular` spinner and optional message. If `BusyState.ErrorMessage` is populated, an error alert with a retry button appears. Otherwise the child content renders normally.

## Custom Error Layouts

Provide a custom template via the `ErrorContent` parameter to override the default alert:

```razor
<BusyBoundary Retry="LoadProductsAsync" ErrorContent="@(message =>
    <MudPaper Class="pa-4" Elevation="1">
        <MudText Color="Color.Error" Typo="Typo.h6">@message</MudText>
        <MudButton Color="Color.Primary" OnClick="LoadProductsAsync">Try Again</MudButton>
    </MudPaper>)">
    @* Child content *@
</BusyBoundary>
```

Because the `UiState` instance is provided as a cascading value, nested components can also consume the same busy context if they need to react to loading transitions.
