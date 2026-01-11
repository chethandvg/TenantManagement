using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Enums;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

public partial class UtilityStatements : ComponentBase
{
    private IEnumerable<UtilityStatementDto>? _statements;
    private bool _showCreateDialog;
    private StatementForm _form = new();
    private DateTime? _formPeriodStart;
    private DateTime? _formPeriodEnd;
    private readonly DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

    [Parameter]
    public Guid? LeaseId { get; set; }

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        if (LeaseId.HasValue)
        {
            await LoadStatementsAsync();
        }
    }

    private async Task LoadStatementsAsync()
    {
        if (!LeaseId.HasValue) return;

        using var busyScope = UiState.Busy.Begin("Loading statements...");
        try
        {
            var response = await BillingClient.GetUtilityStatementsAsync(OrgId);
            if (response.Success && response.Data != null)
            {
                _statements = response.Data.Where(s => s.LeaseId == LeaseId.Value);
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to load statements.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private void ShowCreateDialog()
    {
        _form = new StatementForm { UtilityType = UtilityType.Electricity };
        _formPeriodStart = DateTime.Today.AddMonths(-1);
        _formPeriodEnd = DateTime.Today;
        _showCreateDialog = true;
    }

    private void CloseCreateDialog()
    {
        _showCreateDialog = false;
    }

    private async Task CreateStatementAsync()
    {
        if (!LeaseId.HasValue || !_formPeriodStart.HasValue || !_formPeriodEnd.HasValue)
        {
            Snackbar.Add("Please fill in all required fields.", Severity.Warning);
            return;
        }

        // Validate period dates
        if (_formPeriodEnd.Value < _formPeriodStart.Value)
        {
            Snackbar.Add("Period end date must be after or equal to period start date.", Severity.Warning);
            return;
        }

        // Validate meter-based fields
        if (_form.IsMeterBased)
        {
            if (!_form.PreviousReading.HasValue || !_form.CurrentReading.HasValue)
            {
                Snackbar.Add("Please provide both previous and current readings for meter-based statements.", Severity.Warning);
                return;
            }

            if (_form.CurrentReading.Value < _form.PreviousReading.Value)
            {
                Snackbar.Add("Current reading must be greater than or equal to previous reading.", Severity.Warning);
                return;
            }
        }
        else
        {
            // Validate amount-based fields
            if (!_form.DirectBillAmount.HasValue || _form.DirectBillAmount.Value <= 0)
            {
                Snackbar.Add("Please provide a valid amount for amount-based statements.", Severity.Warning);
                return;
            }
        }

        using var busyScope = UiState.Busy.Begin("Creating statement...");
        try
        {
            var request = new CreateUtilityStatementRequest
            {
                LeaseId = LeaseId.Value,
                UtilityType = _form.UtilityType,
                BillingPeriodStart = DateOnly.FromDateTime(_formPeriodStart.Value),
                BillingPeriodEnd = DateOnly.FromDateTime(_formPeriodEnd.Value),
                IsMeterBased = _form.IsMeterBased,
                PreviousReading = _form.PreviousReading,
                CurrentReading = _form.CurrentReading,
                DirectBillAmount = _form.DirectBillAmount,
                Notes = _form.Notes
            };

            var response = await BillingClient.CreateUtilityStatementAsync(request);
            if (response.Success)
            {
                Snackbar.Add("Statement created successfully.", Severity.Success);
                CloseCreateDialog();
                await LoadStatementsAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to create statement.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private class StatementForm
    {
        public UtilityType UtilityType { get; set; }
        public bool IsMeterBased { get; set; }
        public decimal? PreviousReading { get; set; }
        public decimal? CurrentReading { get; set; }
        public decimal? DirectBillAmount { get; set; }
        public string? Notes { get; set; }
    }
}
