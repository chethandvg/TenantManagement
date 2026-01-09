using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TentMan.Ui.Pages.Tenant;

public partial class MoveInHandover : ComponentBase
{
    private HandoverViewModel? _handover;
    private bool _isSubmitting;
    private bool _isSubmitted;
    private string? _error;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // TODO: Call API to get handover checklist
        await Task.Delay(500);

        // Mock data
        _handover = new HandoverViewModel
        {
            UnitNumber = "A-101",
            Date = DateOnly.FromDateTime(DateTime.Today),
            TenantSignature = "",
            ChecklistItems = new List<ChecklistItemViewModel>
            {
                new() { ItemName = "Living Room - Walls", Condition = "Good", IsConfirmed = false, Notes = "" },
                new() { ItemName = "Living Room - Flooring", Condition = "Good", IsConfirmed = false, Notes = "" },
                new() { ItemName = "Kitchen - Appliances", Condition = "Good", IsConfirmed = false, Notes = "" },
                new() { ItemName = "Bathroom - Fixtures", Condition = "Good", IsConfirmed = false, Notes = "" },
                new() { ItemName = "Bedroom - Windows", Condition = "Good", IsConfirmed = false, Notes = "" },
            }
        };
    }

    private async Task SubmitHandover()
    {
        if (string.IsNullOrWhiteSpace(_handover?.TenantSignature))
        {
            _error = "Please sign by typing your full name";
            return;
        }

        var unconfirmedItems = _handover.ChecklistItems.Count(x => !x.IsConfirmed);
        if (unconfirmedItems > 0)
        {
            _error = $"Please confirm all checklist items ({unconfirmedItems} remaining)";
            return;
        }

        _isSubmitting = true;
        _error = null;

        try
        {
            // TODO: Call API to submit handover
            await Task.Delay(1000);

            _isSubmitted = true;
            Snackbar.Add("Handover checklist submitted successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private sealed class HandoverViewModel
    {
        public string UnitNumber { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string TenantSignature { get; set; } = string.Empty;
        public List<ChecklistItemViewModel> ChecklistItems { get; set; } = new();
    }

    private sealed class ChecklistItemViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
