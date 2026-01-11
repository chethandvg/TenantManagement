using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice runs list page showing all invoice batch runs.
/// </summary>
public partial class InvoiceRuns : ComponentBase
{
    private IEnumerable<InvoiceRunDto>? _invoiceRuns;
    private InvoiceRunStatus? _statusFilter;
    private bool _showRunMonthlyDialog;
    private DateTime? _runPeriodStart = DateTime.Today;
    private readonly DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceRunsAsync();
    }

    private async Task LoadInvoiceRunsAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading invoice runs...");
        UiState.Busy.ClearError();
        _invoiceRuns = null;

        try
        {
            var response = await BillingClient.GetInvoiceRunsAsync(OrgId, _statusFilter);

            if (response.Success && response.Data != null)
            {
                _invoiceRuns = response.Data.OrderByDescending(r => r.StartedAtUtc);
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load invoice runs.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading invoice runs: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await LoadInvoiceRunsAsync();
    }

    private Task ShowRunMonthlyDialogAsync()
    {
        _runPeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _showRunMonthlyDialog = true;
        return Task.CompletedTask;
    }

    private void CloseRunMonthlyDialog()
    {
        _showRunMonthlyDialog = false;
    }

    private async Task RunMonthlyGenerationAsync()
    {
        if (!_runPeriodStart.HasValue)
        {
            Snackbar.Add("Please select a billing period start date.", Severity.Warning);
            return;
        }

        using var busyScope = UiState.Busy.Begin("Running monthly invoice generation...");
        CloseRunMonthlyDialog();

        try
        {
            var periodStart = DateOnly.FromDateTime(_runPeriodStart.Value);
            var response = await BillingClient.CreateMonthlyInvoiceRunAsync(OrgId, periodStart);

            if (response.Success && response.Data != null)
            {
                Snackbar.Add($"Invoice run completed. {response.Data.SuccessCount} invoices generated.", Severity.Success);
                Navigation.NavigateTo($"/billing/invoice-runs/{response.Data.Id}");
            }
            else
            {
                var message = response.Message ?? "Failed to run invoice generation.";
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error running invoice generation: {ex.Message}", Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceRunStatus status)
    {
        return BillingStatusHelper.GetInvoiceRunStatusChip(status);
    }
}
