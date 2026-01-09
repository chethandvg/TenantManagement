using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Tenants;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Tenants;

/// <summary>
/// Tenant detail page with tabs for profile, addresses, documents, and leases.
/// </summary>
public partial class TenantDetails : ComponentBase
{
    private TenantDetailDto? _tenant;
    private IEnumerable<LeaseListDto>? _leases;
    private int _activeTab = 0;
    private bool _showAddressDialog = false;
    private bool _showDocumentDialog = false;
    private AddressFormModel _addressModel = new();
    private DocumentFormModel _documentModel = new();

    private List<BreadcrumbItem> _breadcrumbs = new();

    [Parameter]
    public Guid TenantId { get; set; }

    [Inject]
    public ITenantsApiClient TenantsClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantAsync();
    }

    private async Task LoadTenantAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading tenant details...");
        UiState.Busy.ClearError();

        try
        {
            var response = await TenantsClient.GetTenantAsync(TenantId);

            if (response.Success && response.Data != null)
            {
                _tenant = response.Data;
                UpdateBreadcrumbs();
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load tenant details.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading tenant: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private void UpdateBreadcrumbs()
    {
        _breadcrumbs = new List<BreadcrumbItem>
        {
            new("Tenants", "/tenants", false, Icons.Material.Filled.People),
            new(_tenant?.FullName ?? "Details", null, true)
        };
    }

    private void ShowEditDialog()
    {
        // TODO: Implement edit dialog
        Snackbar.Add("Edit functionality coming soon!", Severity.Info);
    }

    private void ShowAddAddressDialog()
    {
        _addressModel = new AddressFormModel();
        _showAddressDialog = true;
    }

    private async Task SaveAddress()
    {
        // TODO: Implement add address API call
        Snackbar.Add("Add address functionality coming soon!", Severity.Info);
        _showAddressDialog = false;
        await Task.CompletedTask;
    }

    private void ShowUploadDocumentDialog()
    {
        _documentModel = new DocumentFormModel();
        _showDocumentDialog = true;
    }

    private void OnDocumentFileSelected(IBrowserFile file)
    {
        _documentModel.File = file;
    }

    private async Task UploadDocument()
    {
        // TODO: Implement document upload API call
        Snackbar.Add("Document upload functionality coming soon!", Severity.Info);
        _showDocumentDialog = false;
        await Task.CompletedTask;
    }

    private void PreviewDocument(TenantDocumentDto document)
    {
        // TODO: Implement document preview
        Snackbar.Add("Document preview coming soon!", Severity.Info);
    }

    private Color GetDocTypeColor(DocumentType docType) => docType switch
    {
        DocumentType.IDProof => Color.Primary,
        DocumentType.AddressProof => Color.Info,
        DocumentType.Photo => Color.Success,
        DocumentType.PoliceVerification => Color.Warning,
        DocumentType.Agreement => Color.Secondary,
        _ => Color.Default
    };

    private Color GetLeaseStatusColor(LeaseStatus status) => status switch
    {
        LeaseStatus.Draft => Color.Default,
        LeaseStatus.Active => Color.Success,
        LeaseStatus.NoticeGiven => Color.Warning,
        LeaseStatus.Ended => Color.Error,
        LeaseStatus.Cancelled => Color.Error,
        _ => Color.Default
    };
}

/// <summary>
/// Model for address form.
/// </summary>
public class AddressFormModel
{
    public AddressType Type { get; set; } = AddressType.Current;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "IN";
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Model for document upload form.
/// </summary>
public class DocumentFormModel
{
    public DocumentType DocType { get; set; } = DocumentType.IDProof;
    public string? DocNumberMasked { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public IBrowserFile? File { get; set; }
}
