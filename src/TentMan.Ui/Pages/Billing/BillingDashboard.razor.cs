using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Billing dashboard page showing invoice statistics and recent invoices.
/// </summary>
public partial class BillingDashboard : ComponentBase
{
    private InvoiceStats? _invoiceStats;
    private IEnumerable<InvoiceDto>? _recentInvoices;

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
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading dashboard data...");
        UiState.Busy.ClearError();
        _invoiceStats = null;
        _recentInvoices = null;

        try
        {
            // Load all invoices to calculate stats
            var response = await BillingClient.GetInvoicesAsync(OrgId);

            if (response.Success && response.Data != null)
            {
                var allInvoices = response.Data.ToList();
                
                // Calculate statistics
                var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
                var firstDayOfMonth = new DateOnly(currentMonth.Year, currentMonth.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                _invoiceStats = new InvoiceStats
                {
                    TotalCount = allInvoices.Count,
                    DraftCount = allInvoices.Count(i => i.Status == InvoiceStatus.Draft),
                    TotalDueThisMonth = allInvoices.Count(i => 
                        i.DueDate >= firstDayOfMonth && i.DueDate <= lastDayOfMonth && 
                        (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) &&
                        i.BalanceAmount > 0),
                    TotalAmountDueThisMonth = allInvoices
                        .Where(i => i.DueDate >= firstDayOfMonth && i.DueDate <= lastDayOfMonth && 
                                    (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) &&
                                    i.BalanceAmount > 0)
                        .Sum(i => i.BalanceAmount),
                    OverdueCount = allInvoices.Count(i => 
                        i.DueDate < currentMonth && 
                        (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) && 
                        i.BalanceAmount > 0),
                    OverdueAmount = allInvoices
                        .Where(i => i.DueDate < currentMonth && 
                                    (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) && 
                                    i.BalanceAmount > 0)
                        .Sum(i => i.BalanceAmount)
                };

                // Get recent invoices (last 10)
                _recentInvoices = allInvoices
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(10)
                    .ToList();

                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load dashboard data.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading dashboard data: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task GenerateMonthlyInvoicesAsync()
    {
        using var busyScope = UiState.Busy.Begin("Generating monthly invoices...");
        
        try
        {
            var periodStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            var response = await BillingClient.CreateMonthlyInvoiceRunAsync(OrgId, periodStart);

            if (response.Success && response.Data != null)
            {
                Snackbar.Add($"Invoice run completed. {response.Data.SuccessCount} invoices generated.", Severity.Success);
                Navigation.NavigateTo($"/billing/invoice-runs/{response.Data.Id}");
            }
            else
            {
                var message = response.Message ?? "Failed to generate invoices.";
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating invoices: {ex.Message}", Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceStatus status)
    {
        return BillingStatusHelper.GetInvoiceStatusChip(status);
    }

    private class InvoiceStats
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int TotalDueThisMonth { get; set; }
        public decimal TotalAmountDueThisMonth { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueAmount { get; set; }
    }
}
