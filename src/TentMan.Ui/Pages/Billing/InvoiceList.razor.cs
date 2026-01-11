using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice list page with filtering capabilities.
/// </summary>
public partial class InvoiceList : ComponentBase
{
    private IEnumerable<InvoiceDto>? _invoices;
    private InvoiceStatus? _statusFilter;

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading invoices...");
        UiState.Busy.ClearError();
        _invoices = null;

        try
        {
            var response = await BillingClient.GetInvoicesAsync(OrgId, _statusFilter);

            if (response.Success && response.Data != null)
            {
                _invoices = response.Data;
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load invoices.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading invoices: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await LoadInvoicesAsync();
    }

    private RenderFragment GetStatusChip(InvoiceStatus status)
    {
        return BillingStatusHelper.GetInvoiceStatusChip(status);
    }
}
