using Microsoft.AspNetCore.Components;

namespace TentMan.Ui.Pages.Tenant;

public partial class Dashboard : ComponentBase
{
    private LeaseViewModel? _activeLease;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // TODO: Call API to get tenant's active lease
        await Task.Delay(500);

        // Mock data for now
        _activeLease = new LeaseViewModel
        {
            LeaseNumber = "LSE-2026-001",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            MonthlyRent = 25000,
            SecurityDeposit = 75000,
            RentDueDay = 5,
            UnitNumber = "A-101",
            BuildingName = "Green Meadows Apartments"
        };
    }

    private sealed class LeaseViewModel
    {
        public string LeaseNumber { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
        public byte RentDueDay { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
    }
}
