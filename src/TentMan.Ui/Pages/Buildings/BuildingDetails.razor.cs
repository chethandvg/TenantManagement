using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Owners;
using TentMan.Contracts.Units;
using TentMan.Ui.Components.Common;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Buildings;

/// <summary>
/// Building details page with tabbed interface for Units, Ownership, and Documents.
/// </summary>
public partial class BuildingDetails : ComponentBase
{
    private BuildingDetailDto? _building;
    private IEnumerable<UnitListDto>? _units;
    private IEnumerable<OwnerDto> _owners = Enumerable.Empty<OwnerDto>();
    private int _activeTab = 0;

    // Breadcrumbs
    private List<BreadcrumbItem> _breadcrumbs = new();

    // Add unit
    private bool _showAddUnitDialog = false;
    private UnitFormModel _newUnit = new();

    // Bulk add
    private bool _showBulkAddDialog = false;
    private string _bulkPrefix = string.Empty;
    private int _bulkStart = 1;
    private int _bulkEnd = 10;
    private int _bulkDefaultFloor = 1;
    private UnitType _bulkDefaultType = UnitType.OneBHK;

    // Ownership
    private List<OwnershipShareModel> _buildingOwnershipShares = new();
    private List<OwnershipShareModel> _originalOwnershipShares = new();
    private DateTime? _ownershipEffectiveDate = DateTime.Today;
    private bool _ownershipChanged = false;

    // Documents
    private List<FileUploadModel> _uploadedFiles = new();

    [Parameter]
    public Guid BuildingId { get; set; }

    [Inject]
    public IBuildingsApiClient BuildingsClient { get; set; } = default!;

    [Inject]
    public IOwnersApiClient OwnersClient { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        await LoadBuildingAsync();
        await LoadOwnersAsync();
    }

    private async Task LoadBuildingAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading building...");
        UiState.Busy.ClearError();

        try
        {
            var buildingResponse = await BuildingsClient.GetBuildingAsync(BuildingId);
            if (buildingResponse.Success && buildingResponse.Data != null)
            {
                _building = buildingResponse.Data;
                InitializeBreadcrumbs();
                InitializeOwnershipShares();
            }
            else
            {
                UiState.Busy.SetError(buildingResponse.Message ?? "Building not found.");
                return;
            }

            await LoadUnitsAsync();
        }
        catch (Exception ex)
        {
            UiState.Busy.SetError($"Error loading building: {ex.Message}");
        }
    }

    private async Task LoadUnitsAsync()
    {
        try
        {
            var unitsResponse = await BuildingsClient.GetUnitsAsync(BuildingId);
            if (unitsResponse.Success && unitsResponse.Data != null)
            {
                _units = unitsResponse.Data;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load units: {ex.Message}", Severity.Warning);
        }
    }

    private async Task LoadOwnersAsync()
    {
        try
        {
            var response = await OwnersClient.GetOwnersAsync(OrgId);
            if (response.Success && response.Data != null)
            {
                _owners = response.Data;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load owners: {ex.Message}", Severity.Warning);
        }
    }

    private void InitializeBreadcrumbs()
    {
        _breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Buildings", "/buildings"),
            new BreadcrumbItem(_building?.Name ?? "Details", null, disabled: true)
        };
    }

    private void InitializeOwnershipShares()
    {
        if (_building?.CurrentOwners != null)
        {
            _buildingOwnershipShares = _building.CurrentOwners
                .Select(o => new OwnershipShareModel
                {
                    OwnerId = o.OwnerId,
                    SharePercent = o.SharePercent
                })
                .ToList();

            _originalOwnershipShares = _buildingOwnershipShares
                .Select(s => new OwnershipShareModel
                {
                    OwnerId = s.OwnerId,
                    SharePercent = s.SharePercent
                })
                .ToList();
        }
    }

    private Color GetPropertyTypeColor(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Apartment => Color.Primary,
        PropertyType.IndependentHouse => Color.Success,
        PropertyType.Commercial => Color.Warning,
        PropertyType.MixedUse => Color.Info,
        _ => Color.Default
    };

    private Color GetOccupancyColor(OccupancyStatus status) => status switch
    {
        OccupancyStatus.Vacant => Color.Success,
        OccupancyStatus.Occupied => Color.Primary,
        OccupancyStatus.Blocked => Color.Error,
        _ => Color.Default
    };

    private void ShowEditDialog()
    {
        // TODO: Implement edit dialog
        Snackbar.Add("Edit functionality coming soon!", Severity.Info);
    }

    private Task OnUnitRowClick(DataGridRowClickEventArgs<UnitListDto> args)
    {
        // TODO: Navigate to unit details or show modal
        Snackbar.Add($"Selected unit: {args.Item.UnitNumber}", Severity.Info);
        return Task.CompletedTask;
    }

    #region Units

    private void ShowAddUnitDialog()
    {
        _newUnit = new UnitFormModel
        {
            Floor = 1,
            UnitType = UnitType.OneBHK,
            Furnishing = Furnishing.Unfurnished
        };
        _showAddUnitDialog = true;
    }

    private async Task AddUnit()
    {
        if (string.IsNullOrWhiteSpace(_newUnit.UnitNumber))
        {
            Snackbar.Add("Unit number is required.", Severity.Error);
            return;
        }

        try
        {
            var request = new CreateUnitRequest
            {
                BuildingId = BuildingId,
                UnitNumber = _newUnit.UnitNumber,
                Floor = _newUnit.Floor,
                UnitType = _newUnit.UnitType,
                AreaSqFt = _newUnit.AreaSqFt,
                Bedrooms = _newUnit.Bedrooms,
                Bathrooms = _newUnit.Bathrooms,
                Furnishing = _newUnit.Furnishing,
                ParkingSlots = _newUnit.ParkingSlots
            };

            var response = await BuildingsClient.CreateUnitAsync(BuildingId, request);
            if (response.Success)
            {
                Snackbar.Add("Unit created successfully!", Severity.Success);
                _showAddUnitDialog = false;
                await LoadUnitsAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to create unit.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating unit: {ex.Message}", Severity.Error);
        }
    }

    private void ShowBulkAddDialog()
    {
        _bulkPrefix = string.Empty;
        _bulkStart = 1;
        _bulkEnd = 10;
        _bulkDefaultFloor = 1;
        _bulkDefaultType = UnitType.OneBHK;
        _showBulkAddDialog = true;
    }

    private string GetBulkPreview()
    {
        if (_bulkEnd < _bulkStart)
        {
            return "Invalid range";
        }

        var count = _bulkEnd - _bulkStart + 1;
        var first = $"{_bulkPrefix}{_bulkStart}";
        var last = $"{_bulkPrefix}{_bulkEnd}";
        return $"{count} units: {first} to {last}";
    }

    private async Task BulkAddUnits()
    {
        if (_bulkEnd < _bulkStart)
        {
            Snackbar.Add("End must be greater than or equal to Start.", Severity.Error);
            return;
        }

        try
        {
            var units = new List<CreateUnitRequest>();
            for (var i = _bulkStart; i <= _bulkEnd; i++)
            {
                units.Add(new CreateUnitRequest
                {
                    BuildingId = BuildingId,
                    UnitNumber = $"{_bulkPrefix}{i}",
                    Floor = _bulkDefaultFloor,
                    UnitType = _bulkDefaultType,
                    Furnishing = Furnishing.Unfurnished
                });
            }

            var request = new BulkCreateUnitsRequest
            {
                BuildingId = BuildingId,
                Units = units
            };

            var response = await BuildingsClient.BulkCreateUnitsAsync(BuildingId, request);
            if (response.Success)
            {
                Snackbar.Add($"Created {units.Count} units successfully!", Severity.Success);
                _showBulkAddDialog = false;
                await LoadUnitsAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to create units.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating units: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Ownership

    private void OnOwnershipSharesChanged(List<OwnershipShareModel> shares)
    {
        _buildingOwnershipShares = shares;
        _ownershipChanged = HasOwnershipChanged();
    }

    private bool HasOwnershipChanged()
    {
        if (_buildingOwnershipShares.Count != _originalOwnershipShares.Count)
        {
            return true;
        }

        foreach (var share in _buildingOwnershipShares)
        {
            var original = _originalOwnershipShares.FirstOrDefault(s => s.OwnerId == share.OwnerId);
            if (original == null || original.SharePercent != share.SharePercent)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOwnershipValid()
    {
        if (!_buildingOwnershipShares.Any())
        {
            return true;
        }

        var total = _buildingOwnershipShares.Sum(s => s.SharePercent);
        if (Math.Abs(total - 100) > 0.01M)
        {
            return false;
        }

        var duplicates = _buildingOwnershipShares.GroupBy(s => s.OwnerId).Any(g => g.Count() > 1);
        return !duplicates;
    }

    private void AddNewOwnerShare()
    {
        var firstAvailable = _owners.FirstOrDefault(o => !_buildingOwnershipShares.Any(s => s.OwnerId == o.Id));
        _buildingOwnershipShares.Add(new OwnershipShareModel
        {
            OwnerId = firstAvailable?.Id ?? Guid.Empty,
            SharePercent = 0
        });
        _ownershipChanged = true;
    }

    private void ResetOwnership()
    {
        _buildingOwnershipShares = _originalOwnershipShares
            .Select(s => new OwnershipShareModel
            {
                OwnerId = s.OwnerId,
                SharePercent = s.SharePercent
            })
            .ToList();
        _ownershipChanged = false;
    }

    private async Task SaveOwnership()
    {
        if (!IsOwnershipValid())
        {
            Snackbar.Add("Please fix ownership validation errors.", Severity.Error);
            return;
        }

        try
        {
            var request = new SetBuildingOwnershipRequest
            {
                Shares = _buildingOwnershipShares.Select(s => new OwnershipShareRequest
                {
                    OwnerId = s.OwnerId,
                    SharePercent = s.SharePercent
                }).ToList(),
                EffectiveFrom = _ownershipEffectiveDate ?? DateTime.UtcNow
            };

            var response = await BuildingsClient.SetBuildingOwnershipAsync(BuildingId, request);
            if (response.Success)
            {
                Snackbar.Add("Ownership saved successfully!", Severity.Success);
                _originalOwnershipShares = _buildingOwnershipShares
                    .Select(s => new OwnershipShareModel
                    {
                        OwnerId = s.OwnerId,
                        SharePercent = s.SharePercent
                    })
                    .ToList();
                _ownershipChanged = false;
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to save ownership.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving ownership: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Documents

    private void OnFilesChanged(List<FileUploadModel> files)
    {
        _uploadedFiles = files;
    }

    private Task OnFileUploaded(IBrowserFile file)
    {
        // TODO: Implement actual file upload to storage service
        Snackbar.Add($"File {file.Name} ready for upload.", Severity.Info);
        return Task.CompletedTask;
    }

    private string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    #endregion
}
