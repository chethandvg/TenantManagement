using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice run detail page showing run results and per-lease items.
/// </summary>
public partial class InvoiceRunDetail : ComponentBase
{
    private InvoiceRunDto? _invoiceRun;

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceRunAsync();
    }

    private async Task LoadInvoiceRunAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading invoice run...");
        UiState.Busy.ClearError();
        _invoiceRun = null;

        try
        {
            var response = await BillingClient.GetInvoiceRunAsync(Id);

            if (response.Success && response.Data != null)
            {
                _invoiceRun = response.Data;
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load invoice run.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading invoice run: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceRunStatus status)
    {
        return BillingStatusHelper.GetInvoiceRunStatusChip(status);
    }
}
