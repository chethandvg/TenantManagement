using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.Pages.Billing;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Tenant;

/// <summary>
/// Tenant invoice detail page showing full invoice information.
/// </summary>
public partial class TenantInvoiceDetail : ComponentBase
{
    private InvoiceDto? _invoice;

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public ITenantPortalApiClient TenantPortalClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading bill details...");
        UiState.Busy.ClearError();
        _invoice = null;

        try
        {
            var response = await TenantPortalClient.GetInvoiceAsync(Id);

            if (response.Success && response.Data != null)
            {
                _invoice = response.Data;
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load bill details.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading bill details: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceStatus status)
    {
        return BillingStatusHelper.GetInvoiceStatusChip(status);
    }
}
