using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Enums;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Recurring charges management page.
/// </summary>
public partial class RecurringCharges : ComponentBase
{
    private IEnumerable<LeaseRecurringChargeDto>? _charges;
    private IEnumerable<ChargeTypeDto>? _chargeTypes;
    private bool _showChargeDialog;
    private bool _showDeleteDialog;
    private LeaseRecurringChargeDto? _editingCharge;
    private LeaseRecurringChargeDto? _deleteTarget;
    private ChargeFormModel _chargeForm = new();
    private DateTime? _chargeFormStartDate;
    private DateTime? _chargeFormEndDate;
    private readonly DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

    [Parameter]
    public Guid? LeaseId { get; set; }

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
        if (LeaseId.HasValue)
        {
            await LoadChargeTypesAsync();
            await LoadRecurringChargesAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // Only reload if LeaseId has actually changed to avoid double loading on initialization
        if (LeaseId.HasValue && _charges == null)
        {
            await LoadChargeTypesAsync();
            await LoadRecurringChargesAsync();
        }
    }

    private async Task LoadChargeTypesAsync()
    {
        try
        {
            var response = await BillingClient.GetChargeTypesAsync(OrgId);
            if (response.Success && response.Data != null)
            {
                _chargeTypes = response.Data;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading charge types: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadRecurringChargesAsync()
    {
        if (!LeaseId.HasValue) return;

        using var busyScope = UiState.Busy.Begin("Loading recurring charges...");
        UiState.Busy.ClearError();
        _charges = null;

        try
        {
            var response = await BillingClient.GetRecurringChargesAsync(LeaseId.Value);

            if (response.Success && response.Data != null)
            {
                _charges = response.Data.OrderBy(c => c.StartDate);
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load recurring charges.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading recurring charges: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private void ShowAddChargeDialog()
    {
        _editingCharge = null;
        _chargeForm = new ChargeFormModel
        {
            IsActive = true,
            Frequency = BillingFrequency.Monthly
        };
        _chargeFormStartDate = DateTime.Today;
        _chargeFormEndDate = null;
        _showChargeDialog = true;
    }

    private void ShowEditChargeDialog(LeaseRecurringChargeDto charge)
    {
        _editingCharge = charge;
        _chargeForm = new ChargeFormModel
        {
            ChargeTypeId = charge.ChargeTypeId,
            Description = charge.Description,
            Amount = charge.Amount,
            Frequency = charge.Frequency,
            IsActive = charge.IsActive,
            Notes = charge.Notes
        };
        _chargeFormStartDate = charge.StartDate.ToDateTime(TimeOnly.MinValue);
        _chargeFormEndDate = charge.EndDate?.ToDateTime(TimeOnly.MinValue);
        _showChargeDialog = true;
    }

    private void CloseChargeDialog()
    {
        _showChargeDialog = false;
        _editingCharge = null;
    }

    private async Task SaveChargeAsync()
    {
        if (!LeaseId.HasValue) return;

        // Validate required fields
        if (!_chargeForm.ChargeTypeId.HasValue)
        {
            Snackbar.Add("Please select a charge type.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_chargeForm.Description))
        {
            Snackbar.Add("Please enter a description.", Severity.Warning);
            return;
        }

        if (_chargeForm.Amount <= 0)
        {
            Snackbar.Add("Amount must be greater than zero.", Severity.Warning);
            return;
        }

        if (!_chargeFormStartDate.HasValue)
        {
            Snackbar.Add("Please select a start date.", Severity.Warning);
            return;
        }

        // Validate end date is after start date
        if (_chargeFormEndDate.HasValue && _chargeFormEndDate.Value < _chargeFormStartDate.Value)
        {
            Snackbar.Add("End date must be after or equal to start date.", Severity.Warning);
            return;
        }

        using var busyScope = UiState.Busy.Begin(_editingCharge == null ? "Creating charge..." : "Updating charge...");

        try
        {
            if (_editingCharge == null)
            {
                // Create new charge
                var createRequest = new CreateRecurringChargeRequest
                {
                    ChargeTypeId = _chargeForm.ChargeTypeId.Value,
                    Description = _chargeForm.Description,
                    Amount = _chargeForm.Amount,
                    Frequency = _chargeForm.Frequency,
                    StartDate = DateOnly.FromDateTime(_chargeFormStartDate.Value),
                    EndDate = _chargeFormEndDate.HasValue ? DateOnly.FromDateTime(_chargeFormEndDate.Value) : null,
                    IsActive = _chargeForm.IsActive,
                    Notes = _chargeForm.Notes
                };

                var response = await BillingClient.CreateRecurringChargeAsync(LeaseId.Value, createRequest);

                if (response.Success)
                {
                    Snackbar.Add("Recurring charge created successfully.", Severity.Success);
                    CloseChargeDialog();
                    await LoadRecurringChargesAsync();
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to create charge.", Severity.Error);
                }
            }
            else
            {
                // Update existing charge
                var updateRequest = new UpdateRecurringChargeRequest
                {
                    Description = _chargeForm.Description,
                    Amount = _chargeForm.Amount,
                    Frequency = _chargeForm.Frequency,
                    StartDate = DateOnly.FromDateTime(_chargeFormStartDate.Value),
                    EndDate = _chargeFormEndDate.HasValue ? DateOnly.FromDateTime(_chargeFormEndDate.Value) : null,
                    IsActive = _chargeForm.IsActive,
                    Notes = _chargeForm.Notes,
                    RowVersion = _editingCharge.RowVersion
                };

                var response = await BillingClient.UpdateRecurringChargeAsync(_editingCharge.Id, updateRequest);

                if (response.Success)
                {
                    Snackbar.Add("Recurring charge updated successfully.", Severity.Success);
                    CloseChargeDialog();
                    await LoadRecurringChargesAsync();
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to update charge.", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving charge: {ex.Message}", Severity.Error);
        }
    }

    private void ShowDeleteConfirmDialog(LeaseRecurringChargeDto charge)
    {
        _deleteTarget = charge;
        _showDeleteDialog = true;
    }

    private void CloseDeleteDialog()
    {
        _showDeleteDialog = false;
        _deleteTarget = null;
    }

    private async Task DeleteChargeAsync()
    {
        if (_deleteTarget == null) return;

        using var busyScope = UiState.Busy.Begin("Deleting charge...");
        CloseDeleteDialog();

        try
        {
            var response = await BillingClient.DeleteRecurringChargeAsync(_deleteTarget.Id);

            if (response.Success)
            {
                Snackbar.Add("Recurring charge deleted successfully.", Severity.Success);
                await LoadRecurringChargesAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to delete charge.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting charge: {ex.Message}", Severity.Error);
        }
    }

    private static string GetFrequencyText(BillingFrequency frequency) => frequency switch
    {
        BillingFrequency.OneTime => "One-Time",
        BillingFrequency.Monthly => "Monthly",
        BillingFrequency.Quarterly => "Quarterly",
        BillingFrequency.Yearly => "Yearly",
        _ => frequency.ToString()
    };

    private class ChargeFormModel
    {
        public Guid? ChargeTypeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public BillingFrequency Frequency { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}
