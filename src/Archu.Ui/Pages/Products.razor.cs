using Archu.ApiClient.Services;
using Archu.Contracts.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Archu.Ui.Pages;

/// <summary>
/// Products page logic that retrieves product listings and reports loading or error states to the UI.
/// </summary>
public partial class Products : ComponentBase
{
    private IEnumerable<ProductDto>? products;
    private bool isLoading = true;
    private string? errorMessage;

    /// <summary>
    /// Gets or sets the API client used to retrieve product data.
    /// </summary>
    [Inject]
    public IProductsApiClient ProductsClient { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service used to notify the user about loading results.
    /// </summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Invoked by the framework when the component is initialized to start the product loading workflow.
    /// </summary>
    /// <returns>A task that completes once product loading has started and finished.</returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadProductsAsync();
    }

    /// <summary>
    /// Retrieves the product list and updates UI state, showing success information or error notifications as needed.
    /// </summary>
    /// <remarks>
    /// Populates the product grid when the API returns data, but records the server-provided message and surfaces an
    /// error snackbar when the request fails or throws, ensuring the user understands why products are unavailable.
    /// </remarks>
    /// <returns>A task that completes when the products have been loaded or an error has been handled.</returns>
    private async Task LoadProductsAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var response = await ProductsClient.GetProductsAsync(pageNumber: 1, pageSize: 50);

            if (response.Success && response.Data != null)
            {
                products = response.Data.Items;
            }
            else
            {
                errorMessage = response.Message ?? "Failed to load products.";
                Snackbar.Add(errorMessage, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading products: {ex.Message}";
            Snackbar.Add(errorMessage, Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}
