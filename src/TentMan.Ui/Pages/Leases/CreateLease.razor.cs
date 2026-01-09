using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.Units;
using TentMan.Ui.Components.Common;
using TentMan.Ui.Pages.Tenants;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Leases;

/// <summary>
/// Lease creation wizard with a multi-step form.
/// </summary>
public partial class CreateLease : ComponentBase
{
    private int _activeStepIndex = 0;
    private bool _isActivating = false;
    private List<string> _validationErrors = new();

    // Step 1: Unit selection
    private IEnumerable<UnitListDto>? _units;
    private UnitListDto? _selectedUnit => _units?.FirstOrDefault(u => u.Id == _leaseModel.UnitId);

    // Step 2: Lease dates & rules
    private LeaseFormModel _leaseModel = new();

    // Step 3: Parties
    private List<LeasePartyModel> _parties = new();
    private bool _showSearchTenantDialog = false;
    private bool _showCreateTenantDialog = false;
    private string _tenantSearchText = string.Empty;
    private IEnumerable<TenantListDto>? _searchResults;
    private TenantFormModel _newTenantModel = new();

    // Step 4: Financial terms
    private LeaseTermModel _termModel = new();

    // Step 5: Documents
    private List<FileUploadModel> _leaseDocuments = new();

    // Step 6: Handover
    private List<ChecklistItemModel> _checklistItems = new();
    private List<MeterReadingModel> _meterReadings = new();
    private List<FileUploadModel> _handoverPhotos = new();
    private bool _showChecklistDialog = false;
    private ChecklistItemModel _newChecklistItem = new();

    // Breadcrumbs
    private List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Buildings", "/buildings", false, Icons.Material.Filled.Apartment),
        new("Create Lease", null, true)
    };

    [Parameter]
    public Guid? UnitId { get; set; }

    [Inject]
    public IBuildingsApiClient BuildingsClient { get; set; } = default!;

    [Inject]
    public ITenantsApiClient TenantsClient { get; set; } = default!;

    [Inject]
    public ILeasesApiClient LeasesClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    // TODO: Get from authenticated user context
    private Guid OrgId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        // Initialize default meter readings
        _meterReadings = new List<MeterReadingModel>
        {
            new() { MeterType = MeterType.Electricity },
            new() { MeterType = MeterType.Water },
            new() { MeterType = MeterType.Gas }
        };

        // Initialize some default checklist items
        _checklistItems = new List<ChecklistItemModel>
        {
            new() { Category = "Electrical", ItemName = "Light fixtures", Condition = ItemCondition.Good },
            new() { Category = "Electrical", ItemName = "Power outlets", Condition = ItemCondition.Good },
            new() { Category = "Plumbing", ItemName = "Faucets", Condition = ItemCondition.Good },
            new() { Category = "Plumbing", ItemName = "Toilets", Condition = ItemCondition.Good },
            new() { Category = "Doors & Windows", ItemName = "Main door lock", Condition = ItemCondition.Good },
            new() { Category = "Walls & Flooring", ItemName = "Wall condition", Condition = ItemCondition.Good },
        };

        // Set default values
        _leaseModel.StartDate = DateTime.Today;
        _leaseModel.RentDueDay = 1;
        _leaseModel.GraceDays = 5;
        _leaseModel.NoticePeriodDays = 30;

        if (UnitId.HasValue)
        {
            _leaseModel.UnitId = UnitId.Value;
        }

        await LoadUnitsAsync();
    }

    private async Task LoadUnitsAsync()
    {
        try
        {
            // First, load all buildings for the organization
            var buildingsResponse = await BuildingsClient.GetBuildingsAsync(OrgId);
            if (!buildingsResponse.Success || buildingsResponse.Data == null)
            {
                Snackbar.Add(buildingsResponse.Message ?? "Failed to load buildings.", Severity.Error);
                _units = new List<UnitListDto>();
                return;
            }

            // Then, load units from all buildings
            var allUnits = new List<UnitListDto>();
            foreach (var building in buildingsResponse.Data)
            {
                var unitsResponse = await BuildingsClient.GetUnitsAsync(building.Id);
                if (unitsResponse.Success && unitsResponse.Data != null)
                {
                    allUnits.AddRange(unitsResponse.Data);
                }
            }

            _units = allUnits;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading units: {ex.Message}", Severity.Error);
            _units = new List<UnitListDto>();
        }
    }

    private void GoToPreviousStep()
    {
        if (_activeStepIndex > 0)
        {
            _activeStepIndex--;
        }
    }

    private void GoToNextStep()
    {
        // Validate current step
        if (!ValidateCurrentStep())
        {
            return;
        }

        if (_activeStepIndex < 6)
        {
            _activeStepIndex++;
        }

        // Run full validation on final step
        if (_activeStepIndex == 6)
        {
            ValidateAll();
        }
    }

    private bool ValidateCurrentStep()
    {
        switch (_activeStepIndex)
        {
            case 0: // Select Unit
                if (!_leaseModel.UnitId.HasValue)
                {
                    Snackbar.Add("Please select a unit.", Severity.Warning);
                    return false;
                }
                break;

            case 1: // Dates & Rules
                if (!_leaseModel.StartDate.HasValue)
                {
                    Snackbar.Add("Please set the start date.", Severity.Warning);
                    return false;
                }
                break;

            case 2: // Parties
                if (!_parties.Any())
                {
                    Snackbar.Add("Please add at least one tenant.", Severity.Warning);
                    return false;
                }
                if (!_parties.Any(p => p.IsResponsibleForPayment))
                {
                    Snackbar.Add("At least one party must be responsible for payment.", Severity.Warning);
                    return false;
                }
                break;

            case 3: // Financial Terms
                if (_termModel.MonthlyRent <= 0)
                {
                    Snackbar.Add("Please set the monthly rent.", Severity.Warning);
                    return false;
                }
                break;
        }

        return true;
    }

    private void ValidateAll()
    {
        _validationErrors.Clear();

        if (!_leaseModel.UnitId.HasValue)
        {
            _validationErrors.Add("Unit is not selected.");
        }

        if (!_leaseModel.StartDate.HasValue)
        {
            _validationErrors.Add("Start date is not set.");
        }

        if (!_parties.Any())
        {
            _validationErrors.Add("No parties have been added to the lease.");
        }

        if (!_parties.Any(p => p.Role == LeasePartyRole.PrimaryTenant))
        {
            _validationErrors.Add("At least one party must be a Primary Tenant.");
        }

        if (!_parties.Any(p => p.IsResponsibleForPayment))
        {
            _validationErrors.Add("At least one party must be responsible for payment.");
        }

        if (_termModel.MonthlyRent <= 0)
        {
            _validationErrors.Add("Monthly rent must be greater than zero.");
        }
    }

    private async Task ActivateLease()
    {
        ValidateAll();
        if (_validationErrors.Any())
        {
            return;
        }

        _isActivating = true;
        try
        {
            // Step 1: Create the lease
            var createLeaseRequest = new CreateLeaseRequest
            {
                UnitId = _leaseModel.UnitId!.Value,
                StartDate = DateOnly.FromDateTime(_leaseModel.StartDate!.Value),
                EndDate = _leaseModel.EndDate.HasValue ? DateOnly.FromDateTime(_leaseModel.EndDate.Value) : null,
                RentDueDay = _leaseModel.RentDueDay,
                GraceDays = _leaseModel.GraceDays,
                NoticePeriodDays = _leaseModel.NoticePeriodDays,
                LateFeeType = _leaseModel.LateFeeType,
                LateFeeValue = _leaseModel.LateFeeValue,
                IsAutoRenew = _leaseModel.IsAutoRenew
            };

            var leaseResponse = await LeasesClient.CreateLeaseAsync(OrgId, createLeaseRequest);
            if (!leaseResponse.Success || leaseResponse.Data == null)
            {
                Snackbar.Add(leaseResponse.Message ?? "Failed to create lease.", Severity.Error);
                return;
            }

            var leaseId = leaseResponse.Data.Id;

            // Step 2: Add parties
            foreach (var party in _parties)
            {
                var addPartyRequest = new AddLeasePartyRequest
                {
                    TenantId = party.TenantId,
                    Role = party.Role,
                    IsResponsibleForPayment = party.IsResponsibleForPayment,
                    MoveInDate = _leaseModel.StartDate.HasValue ? DateOnly.FromDateTime(_leaseModel.StartDate.Value) : null
                };

                await LeasesClient.AddLeasePartyAsync(leaseId, addPartyRequest);
            }

            // Step 3: Add financial terms
            var addTermRequest = new AddLeaseTermRequest
            {
                EffectiveFrom = DateOnly.FromDateTime(_leaseModel.StartDate!.Value),
                MonthlyRent = _termModel.MonthlyRent,
                SecurityDeposit = _termModel.SecurityDeposit,
                MaintenanceCharge = _termModel.MaintenanceCharge,
                OtherFixedCharge = _termModel.OtherFixedCharge,
                EscalationType = _termModel.EscalationType,
                EscalationValue = _termModel.EscalationValue,
                EscalationEveryMonths = _termModel.EscalationEveryMonths,
                Notes = _termModel.Notes
            };

            await LeasesClient.AddLeaseTermAsync(leaseId, addTermRequest);

            // Step 4: Activate the lease
            var activateRequest = new ActivateLeaseRequest { RowVersion = leaseResponse.Data.RowVersion };
            var activateResponse = await LeasesClient.ActivateLeaseAsync(leaseId, activateRequest);

            if (activateResponse.Success)
            {
                Snackbar.Add("Lease created and activated successfully!", Severity.Success);
                NavigationManager.NavigateTo($"/leases/{leaseId}");
            }
            else
            {
                Snackbar.Add(activateResponse.Message ?? "Failed to activate lease.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating lease: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isActivating = false;
        }
    }

    // Tenant search and selection
    private void ShowSearchTenantDialog()
    {
        _tenantSearchText = string.Empty;
        _searchResults = null;
        _showSearchTenantDialog = true;
    }

    private async Task SearchTenants()
    {
        if (string.IsNullOrWhiteSpace(_tenantSearchText))
        {
            _searchResults = null;
            return;
        }

        try
        {
            var response = await TenantsClient.GetTenantsAsync(OrgId, _tenantSearchText);
            if (response.Success && response.Data != null)
            {
                _searchResults = response.Data;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching tenants: {ex.Message}", Severity.Error);
        }
    }

    private void SelectTenant(TenantListDto tenant)
    {
        // Check if tenant is already added
        if (_parties.Any(p => p.TenantId == tenant.Id))
        {
            Snackbar.Add("This tenant is already added to the lease.", Severity.Warning);
            return;
        }

        var party = new LeasePartyModel
        {
            TenantId = tenant.Id,
            TenantName = tenant.FullName,
            TenantPhone = tenant.Phone,
            Role = _parties.Any() ? LeasePartyRole.CoTenant : LeasePartyRole.PrimaryTenant,
            IsResponsibleForPayment = !_parties.Any()
        };

        _parties.Add(party);
        _showSearchTenantDialog = false;
        Snackbar.Add($"{tenant.FullName} added to the lease.", Severity.Success);
    }

    private void ShowCreateTenantDialog()
    {
        _newTenantModel = new TenantFormModel();
        _showCreateTenantDialog = true;
    }

    private async Task CreateAndAddTenant()
    {
        if (string.IsNullOrWhiteSpace(_newTenantModel.FullName) || string.IsNullOrWhiteSpace(_newTenantModel.Phone))
        {
            Snackbar.Add("Name and phone are required.", Severity.Warning);
            return;
        }

        try
        {
            var request = new CreateTenantRequest
            {
                FullName = _newTenantModel.FullName,
                Phone = _newTenantModel.Phone,
                Email = _newTenantModel.Email,
                DateOfBirth = _newTenantModel.DateOfBirth.HasValue
                    ? DateOnly.FromDateTime(_newTenantModel.DateOfBirth.Value)
                    : null,
                Gender = _newTenantModel.Gender
            };

            var response = await TenantsClient.CreateTenantAsync(OrgId, request);
            if (response.Success && response.Data != null)
            {
                var tenant = response.Data;
                var party = new LeasePartyModel
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.FullName,
                    TenantPhone = tenant.Phone,
                    Role = _parties.Any() ? LeasePartyRole.CoTenant : LeasePartyRole.PrimaryTenant,
                    IsResponsibleForPayment = !_parties.Any()
                };

                _parties.Add(party);
                _showCreateTenantDialog = false;
                Snackbar.Add($"{tenant.FullName} created and added to the lease.", Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to create tenant.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating tenant: {ex.Message}", Severity.Error);
        }
    }

    private void RemoveParty(LeasePartyModel party)
    {
        _parties.Remove(party);
    }

    // Checklist management
    private void AddChecklistItem()
    {
        _newChecklistItem = new ChecklistItemModel { Category = "Other", Condition = ItemCondition.Good };
        _showChecklistDialog = true;
    }

    private void AddChecklistItemConfirm()
    {
        if (string.IsNullOrWhiteSpace(_newChecklistItem.ItemName))
        {
            Snackbar.Add("Item name is required.", Severity.Warning);
            return;
        }

        _checklistItems.Add(new ChecklistItemModel
        {
            Category = _newChecklistItem.Category,
            ItemName = _newChecklistItem.ItemName,
            Condition = _newChecklistItem.Condition,
            Remarks = _newChecklistItem.Remarks
        });

        _showChecklistDialog = false;
    }
}

/// <summary>
/// Model for lease form.
/// </summary>
public class LeaseFormModel
{
    public Guid? UnitId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public byte RentDueDay { get; set; } = 1;
    public byte GraceDays { get; set; } = 0;
    public short? NoticePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; } = LateFeeType.None;
    public decimal? LateFeeValue { get; set; }
    public bool IsAutoRenew { get; set; } = false;
}

/// <summary>
/// Model for lease party.
/// </summary>
public class LeasePartyModel
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public LeasePartyRole Role { get; set; } = LeasePartyRole.PrimaryTenant;
    public bool IsResponsibleForPayment { get; set; }
}

/// <summary>
/// Model for lease term.
/// </summary>
public class LeaseTermModel
{
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    public EscalationType EscalationType { get; set; } = EscalationType.None;
    public decimal? EscalationValue { get; set; }
    public short? EscalationEveryMonths { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Model for checklist item.
/// </summary>
public class ChecklistItemModel
{
    public string Category { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ItemCondition Condition { get; set; } = ItemCondition.Good;
    public string? Remarks { get; set; }
}

/// <summary>
/// Model for meter reading.
/// </summary>
public class MeterReadingModel
{
    public MeterType MeterType { get; set; }
    public decimal ReadingValue { get; set; }
}
