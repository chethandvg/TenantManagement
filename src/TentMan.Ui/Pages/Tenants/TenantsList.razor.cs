using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Tenants;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Tenants;

/// <summary>
/// Tenants list page with search and add/edit functionality.
/// </summary>
public partial class TenantsList : ComponentBase
{
    private IEnumerable<TenantListDto>? _tenants;
    private IEnumerable<TenantListDto>? _filteredTenants;
    private string _searchText = string.Empty;
    private bool _showTenantDialog = false;
    private bool _isEditMode = false;
    private TenantFormModel _tenantModel = new();
    private Guid? _editingTenantId;

    [Inject]
    public ITenantsApiClient TenantsClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Replace with OrgId from authenticated user context before production use.
    // WARNING: This placeholder MUST be replaced with the actual organization identifier.
    private Guid OrgId => Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantsAsync();
    }

    private async Task LoadTenantsAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading tenants...");
        UiState.Busy.ClearError();
        _tenants = null;
        _filteredTenants = null;

        try
        {
            var response = await TenantsClient.GetTenantsAsync(OrgId, _searchText);

            if (response.Success && response.Data != null)
            {
                _tenants = response.Data;
                ApplyFilters();
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load tenants.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading tenants: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private void ApplyFilters()
    {
        if (_tenants == null)
        {
            _filteredTenants = null;
            return;
        }

        // Note: API already filters by search text, so we just assign the results directly
        _filteredTenants = _tenants.ToList();
        StateHasChanged();
    }

    private void ShowAddTenantDialog()
    {
        _tenantModel = new TenantFormModel();
        _isEditMode = false;
        _editingTenantId = null;
        _showTenantDialog = true;
    }

    private void ShowEditTenantDialog(TenantListDto tenant)
    {
        _tenantModel = new TenantFormModel
        {
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email
        };
        _isEditMode = true;
        _editingTenantId = tenant.Id;
        _showTenantDialog = true;
    }

    private void CloseTenantDialog()
    {
        _showTenantDialog = false;
        _tenantModel = new();
        _editingTenantId = null;
    }

    private async Task SaveTenant()
    {
        if (string.IsNullOrWhiteSpace(_tenantModel.FullName))
        {
            Snackbar.Add("Full name is required.", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_tenantModel.Phone))
        {
            Snackbar.Add("Phone is required.", Severity.Error);
            return;
        }

        try
        {
            if (_isEditMode && _editingTenantId.HasValue)
            {
                // TODO: Implement update tenant when API supports it
                Snackbar.Add("Update functionality coming soon!", Severity.Info);
            }
            else
            {
                var request = new CreateTenantRequest
                {
                    FullName = _tenantModel.FullName,
                    Phone = _tenantModel.Phone,
                    Email = _tenantModel.Email,
                    DateOfBirth = _tenantModel.DateOfBirth.HasValue 
                        ? DateOnly.FromDateTime(_tenantModel.DateOfBirth.Value) 
                        : null,
                    Gender = _tenantModel.Gender
                };

                var response = await TenantsClient.CreateTenantAsync(OrgId, request);
                if (response.Success)
                {
                    Snackbar.Add("Tenant created successfully!", Severity.Success);
                    CloseTenantDialog();
                    await LoadTenantsAsync();
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to create tenant.", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving tenant: {ex.Message}", Severity.Error);
        }
    }
}

/// <summary>
/// Model for tenant form.
/// </summary>
public class TenantFormModel
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
}
