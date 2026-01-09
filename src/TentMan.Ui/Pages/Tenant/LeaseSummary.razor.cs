using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.TenantPortal;

namespace TentMan.Ui.Pages.Tenant;

public partial class LeaseSummary : ComponentBase
{
    [Inject] private ITenantPortalApiClient TenantPortalApiClient { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private TenantLeaseSummaryResponse? _leaseSummary;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var response = await TenantPortalApiClient.GetLeaseSummaryAsync();
            
            if (response.Success && response.Data != null)
            {
                _leaseSummary = response.Data;
            }
            else
            {
                _leaseSummary = null;
                if (response.Errors?.Any() == true)
                {
                    Snackbar.Add(string.Join(", ", response.Errors), Severity.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading lease summary: {ex.Message}", Severity.Error);
        }
    }

    private static Color GetStatusColor(LeaseStatus status)
    {
        return status switch
        {
            LeaseStatus.Active => Color.Success,
            LeaseStatus.NoticeGiven => Color.Warning,
            LeaseStatus.Draft => Color.Info,
            LeaseStatus.Ended => Color.Default,
            LeaseStatus.Cancelled => Color.Error,
            _ => Color.Default
        };
    }
}
