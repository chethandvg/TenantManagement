using Microsoft.AspNetCore.Components;
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
/// Create Building wizard page with 5 steps: Info, Address, Units, Ownership, Documents.
/// </summary>
public partial class CreateBuilding : ComponentBase
{
    private int _activeStep = 0;
    private bool _isSubmitting = false;

    // Step 1: Building Info
    private CreateBuildingModel _buildingRequest = new();

    // Step 2: Address
    private AddressFormModel _addressRequest = new();

    // Step 3: Units
    private List<UnitFormModel> _units = new();
    private bool _showAddUnitDialog = false;
    private UnitFormModel _newUnit = new();

    // Bulk add
    private bool _showBulkAddDialog = false;
    private string _bulkPrefix = string.Empty;
    private int _bulkStart = 1;
    private int _bulkEnd = 10;
    private int _bulkDefaultFloor = 1;
    private UnitType _bulkDefaultType = UnitType.OneBHK;

    // Step 4: Ownership
    private List<OwnershipShareModel> _ownershipShares = new();
    private DateTime? _ownershipEffectiveDate = DateTime.Today;
    private IEnumerable<OwnerDto> _owners = Enumerable.Empty<OwnerDto>();

    // Step 5: Documents
    private List<FileUploadModel> _uploadedFiles = new();

    // Created building ID
    private Guid? _createdBuildingId;

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
        await LoadOwnersAsync();
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

    private void PreviousStep()
    {
        if (_activeStep > 0)
        {
            _activeStep--;
        }
    }

    private void NextStep()
    {
        if (ValidateCurrentStep())
        {
            _activeStep++;
        }
    }

    private bool ValidateCurrentStep()
    {
        return _activeStep switch
        {
            0 => ValidateBuildingInfo(),
            1 => ValidateAddress(),
            2 => true, // Units are optional
            3 => ValidateOwnership(),
            4 => true, // Documents are optional
            _ => true
        };
    }

    private bool ValidateBuildingInfo()
    {
        if (string.IsNullOrWhiteSpace(_buildingRequest.BuildingCode))
        {
            Snackbar.Add("Building code is required.", Severity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_buildingRequest.Name))
        {
            Snackbar.Add("Building name is required.", Severity.Error);
            return false;
        }

        return true;
    }

    private bool ValidateAddress()
    {
        if (string.IsNullOrWhiteSpace(_addressRequest.Line1))
        {
            Snackbar.Add("Address line 1 is required.", Severity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_addressRequest.City))
        {
            Snackbar.Add("City is required.", Severity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_addressRequest.State))
        {
            Snackbar.Add("State is required.", Severity.Error);
            return false;
        }

        return true;
    }

    private bool ValidateOwnership()
    {
        if (!_ownershipShares.Any())
        {
            return true; // Ownership is optional
        }

        var total = _ownershipShares.Sum(s => s.SharePercent);
        if (Math.Abs(total - 100) > 0.01M)
        {
            Snackbar.Add($"Ownership shares must total 100%. Current total: {total:F2}%", Severity.Error);
            return false;
        }

        var duplicates = _ownershipShares.GroupBy(s => s.OwnerId).Where(g => g.Count() > 1).ToList();
        if (duplicates.Any())
        {
            Snackbar.Add("Each owner can only appear once.", Severity.Error);
            return false;
        }

        return true;
    }

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

    private void AddUnit()
    {
        if (string.IsNullOrWhiteSpace(_newUnit.UnitNumber))
        {
            Snackbar.Add("Unit number is required.", Severity.Error);
            return;
        }

        if (_units.Any(u => u.UnitNumber == _newUnit.UnitNumber))
        {
            Snackbar.Add("Unit number already exists.", Severity.Error);
            return;
        }

        _units.Add(_newUnit);
        _showAddUnitDialog = false;
        StateHasChanged();
    }

    private void RemoveUnit(UnitFormModel unit)
    {
        _units.Remove(unit);
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

    private void BulkAddUnits()
    {
        if (_bulkEnd < _bulkStart)
        {
            Snackbar.Add("End must be greater than or equal to Start.", Severity.Error);
            return;
        }

        for (var i = _bulkStart; i <= _bulkEnd; i++)
        {
            var unitNumber = $"{_bulkPrefix}{i}";
            if (!_units.Any(u => u.UnitNumber == unitNumber))
            {
                _units.Add(new UnitFormModel
                {
                    UnitNumber = unitNumber,
                    Floor = _bulkDefaultFloor,
                    UnitType = _bulkDefaultType,
                    Furnishing = Furnishing.Unfurnished
                });
            }
        }

        _showBulkAddDialog = false;
        Snackbar.Add($"Added {_bulkEnd - _bulkStart + 1} units.", Severity.Success);
    }

    private void AddNewOwnerShare()
    {
        var firstAvailable = _owners.FirstOrDefault(o => !_ownershipShares.Any(s => s.OwnerId == o.Id));
        _ownershipShares.Add(new OwnershipShareModel
        {
            OwnerId = firstAvailable?.Id ?? Guid.Empty,
            SharePercent = 0
        });
    }

    private async Task SubmitBuildingAsync()
    {
        if (!ValidateCurrentStep())
        {
            return;
        }

        _isSubmitting = true;

        try
        {
            // Step 1: Create building
            var buildingRequest = new CreateBuildingRequest
            {
                OrgId = OrgId,
                BuildingCode = _buildingRequest.BuildingCode,
                Name = _buildingRequest.Name,
                PropertyType = _buildingRequest.PropertyType,
                TotalFloors = _buildingRequest.TotalFloors,
                HasLift = _buildingRequest.HasLift,
                Notes = _buildingRequest.Notes
            };

            var buildingResponse = await BuildingsClient.CreateBuildingAsync(buildingRequest);
            if (!buildingResponse.Success || buildingResponse.Data == null)
            {
                Snackbar.Add(buildingResponse.Message ?? "Failed to create building.", Severity.Error);
                return;
            }

            _createdBuildingId = buildingResponse.Data.Id;

            // Step 2: Set address
            var addressRequest = new SetBuildingAddressRequest
            {
                Line1 = _addressRequest.Line1,
                Line2 = _addressRequest.Line2,
                Locality = _addressRequest.Locality,
                City = _addressRequest.City,
                District = _addressRequest.District,
                State = _addressRequest.State,
                PostalCode = _addressRequest.PostalCode,
                Landmark = _addressRequest.Landmark,
                Latitude = _addressRequest.Latitude,
                Longitude = _addressRequest.Longitude
            };
            var addressResponse = await BuildingsClient.SetBuildingAddressAsync(_createdBuildingId.Value, addressRequest);
            if (!addressResponse.Success)
            {
                Snackbar.Add(addressResponse.Message ?? "Failed to set address.", Severity.Warning);
            }

            // Step 3: Create units
            if (_units.Any())
            {
                var bulkRequest = new BulkCreateUnitsRequest
                {
                    BuildingId = _createdBuildingId.Value,
                    Units = _units.Select(u => new CreateUnitRequest
                    {
                        BuildingId = _createdBuildingId.Value,
                        UnitNumber = u.UnitNumber,
                        Floor = u.Floor,
                        UnitType = u.UnitType,
                        AreaSqFt = u.AreaSqFt,
                        Bedrooms = u.Bedrooms,
                        Bathrooms = u.Bathrooms,
                        Furnishing = u.Furnishing,
                        ParkingSlots = u.ParkingSlots
                    }).ToList()
                };

                var unitsResponse = await BuildingsClient.BulkCreateUnitsAsync(_createdBuildingId.Value, bulkRequest);
                if (!unitsResponse.Success)
                {
                    Snackbar.Add(unitsResponse.Message ?? "Failed to create some units.", Severity.Warning);
                }
            }

            // Step 4: Set ownership
            if (_ownershipShares.Any())
            {
                var ownershipRequest = new SetBuildingOwnershipRequest
                {
                    Shares = _ownershipShares.Select(s => new OwnershipShareRequest
                    {
                        OwnerId = s.OwnerId,
                        SharePercent = s.SharePercent
                    }).ToList(),
                    EffectiveFrom = _ownershipEffectiveDate ?? DateTime.UtcNow
                };

                var ownershipResponse = await BuildingsClient.SetBuildingOwnershipAsync(_createdBuildingId.Value, ownershipRequest);
                if (!ownershipResponse.Success)
                {
                    Snackbar.Add(ownershipResponse.Message ?? "Failed to set ownership.", Severity.Warning);
                }
            }

            // Step 5: Handle file uploads would be done here via file storage service
            // For now, we'll skip this step

            Snackbar.Add("Building created successfully!", Severity.Success);
            Navigation.NavigateTo($"/buildings/{_createdBuildingId}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating building: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}

/// <summary>
/// Model for building creation form.
/// </summary>
public class CreateBuildingModel
{
    public string BuildingCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; } = PropertyType.Apartment;
    public int TotalFloors { get; set; } = 1;
    public bool HasLift { get; set; }
    public string? Notes { get; set; }
}
