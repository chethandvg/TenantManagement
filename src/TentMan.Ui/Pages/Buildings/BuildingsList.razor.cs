using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Enums;
using TentMan.Ui.Components.Common;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Buildings;

/// <summary>
/// Buildings list page with search, filter, and card/grid view options.
/// </summary>
public partial class BuildingsList : ComponentBase
{
    private IEnumerable<BuildingListDto>? _buildings;
    private IEnumerable<BuildingListDto>? _filteredBuildings;
    private string _searchText = string.Empty;
    private string _activeFilter = string.Empty;
    private bool _showGrid = true;
    private List<FilterOption> _filterOptions = new();

    [Inject]
    public IBuildingsApiClient BuildingsClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override void OnInitialized()
    {
        InitializeFilters();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadBuildingsAsync();
    }

    private void InitializeFilters()
    {
        _filterOptions = new List<FilterOption>
        {
            new FilterOption
            {
                Label = "Property Type",
                Options = Enum.GetValues<PropertyType>()
                    .Select(pt => new FilterChoice { Text = pt.ToString(), Value = pt.ToString() })
                    .ToList()
            }
        };
    }

    private async Task LoadBuildingsAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading buildings...");
        UiState.Busy.ClearError();
        _buildings = null;
        _filteredBuildings = null;

        try
        {
            var response = await BuildingsClient.GetBuildingsAsync(OrgId);

            if (response.Success && response.Data != null)
            {
                _buildings = response.Data;
                ApplyFilters();
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load buildings.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading buildings: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private void ApplyFilters()
    {
        if (_buildings == null)
        {
            _filteredBuildings = null;
            return;
        }

        var filtered = _buildings.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(b =>
                (!string.IsNullOrEmpty(b.Name) && b.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(b.BuildingCode) && b.BuildingCode.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(b.City) && b.City.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply property type filter
        var propertyTypeFilter = _filterOptions.FirstOrDefault(f => f.Label == "Property Type");
        if (propertyTypeFilter != null
            && !string.IsNullOrEmpty(propertyTypeFilter.SelectedValue)
            && Enum.TryParse<PropertyType>(propertyTypeFilter.SelectedValue, out var propertyType))
        {
            filtered = filtered.Where(b => b.PropertyType == propertyType);
        }

        _filteredBuildings = filtered.ToList();
        StateHasChanged();
    }

    private Color GetPropertyTypeColor(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Apartment => Color.Primary,
        PropertyType.IndependentHouse => Color.Success,
        PropertyType.Commercial => Color.Warning,
        PropertyType.MixedUse => Color.Info,
        _ => Color.Default
    };
}
