using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Owners;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Owners;

/// <summary>
/// Owners list page with search and add/edit functionality.
/// </summary>
public partial class OwnersList : ComponentBase
{
    private IEnumerable<OwnerDto>? _owners;
    private IEnumerable<OwnerDto>? _filteredOwners;
    private string _searchText = string.Empty;
    private bool _showOwnerDialog = false;
    private bool _isEditMode = false;
    private OwnerModel _ownerModel = new();
    private Guid? _editingOwnerId;

    [Inject]
    public IOwnersApiClient OwnersClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        await LoadOwnersAsync();
    }

    private async Task LoadOwnersAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading owners...");
        UiState.Busy.ClearError();
        _owners = null;
        _filteredOwners = null;

        try
        {
            var response = await OwnersClient.GetOwnersAsync(OrgId);

            if (response.Success && response.Data != null)
            {
                _owners = response.Data;
                ApplyFilters();
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load owners.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading owners: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private void ApplyFilters()
    {
        if (_owners == null)
        {
            _filteredOwners = null;
            return;
        }

        var filtered = _owners.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(o =>
                (!string.IsNullOrEmpty(o.DisplayName) && o.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(o.Email) && o.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(o.Phone) && o.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        _filteredOwners = filtered.ToList();
        StateHasChanged();
    }

    private Color GetOwnerTypeColor(OwnerType ownerType) => ownerType switch
    {
        OwnerType.Individual => Color.Primary,
        OwnerType.Company => Color.Warning,
        _ => Color.Default
    };

    private void ShowAddOwnerDialog()
    {
        _ownerModel = new OwnerModel { OwnerType = OwnerType.Individual };
        _isEditMode = false;
        _editingOwnerId = null;
        _showOwnerDialog = true;
    }

    private void ShowEditOwnerDialog(OwnerDto owner)
    {
        _ownerModel = new OwnerModel
        {
            DisplayName = owner.DisplayName,
            OwnerType = owner.OwnerType,
            Phone = owner.Phone,
            Email = owner.Email,
            Pan = owner.Pan,
            Gstin = owner.Gstin
        };
        _isEditMode = true;
        _editingOwnerId = owner.Id;
        _showOwnerDialog = true;
    }

    private void CloseOwnerDialog()
    {
        _showOwnerDialog = false;
        _ownerModel = new();
        _editingOwnerId = null;
    }

    private async Task SaveOwner()
    {
        if (string.IsNullOrWhiteSpace(_ownerModel.DisplayName))
        {
            Snackbar.Add("Display name is required.", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_ownerModel.Email))
        {
            Snackbar.Add("Email is required.", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_ownerModel.Phone))
        {
            Snackbar.Add("Phone is required.", Severity.Error);
            return;
        }

        try
        {
            if (_isEditMode && _editingOwnerId.HasValue)
            {
                // TODO: Implement update owner when API supports it
                Snackbar.Add("Update functionality coming soon!", Severity.Info);
            }
            else
            {
                var request = new CreateOwnerRequest
                {
                    OrgId = OrgId,
                    DisplayName = _ownerModel.DisplayName,
                    OwnerType = _ownerModel.OwnerType,
                    Phone = _ownerModel.Phone,
                    Email = _ownerModel.Email,
                    Pan = _ownerModel.Pan,
                    Gstin = _ownerModel.Gstin
                };

                var response = await OwnersClient.CreateOwnerAsync(OrgId, request);
                if (response.Success)
                {
                    Snackbar.Add("Owner created successfully!", Severity.Success);
                    CloseOwnerDialog();
                    await LoadOwnersAsync();
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to create owner.", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving owner: {ex.Message}", Severity.Error);
        }
    }
}

/// <summary>
/// Model for owner form.
/// </summary>
public class OwnerModel
{
    public string DisplayName { get; set; } = string.Empty;
    public OwnerType OwnerType { get; set; } = OwnerType.Individual;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Pan { get; set; }
    public string? Gstin { get; set; }
}
