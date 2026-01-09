using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.TenantInvites;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Tenants;

/// <summary>
/// Tenant detail page with tabs for profile, addresses, documents, and leases.
/// </summary>
public partial class TenantDetails : ComponentBase
{
    private TenantDetailDto? _tenant;
    private IEnumerable<LeaseListDto>? _leases;
    private IEnumerable<TenantInviteDto>? _invites;
    private int _activeTab = 0;
    private bool _showAddressDialog = false;
    private bool _showDocumentDialog = false;
    private bool _showGenerateInviteDialog = false;
    private AddressFormModel _addressModel = new();
    private DocumentFormModel _documentModel = new();
    private GenerateInviteFormModel _generateInviteModel = new();
    private TenantInviteDto? _generatedInvite;

    private List<BreadcrumbItem> _breadcrumbs = new();

    [Parameter]
    public Guid TenantId { get; set; }

    [Inject]
    public ITenantsApiClient TenantsClient { get; set; } = default!;

    [Inject]
    public ITenantInvitesApiClient TenantInvitesClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantAsync();
        await LoadInvitesAsync();
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

    private void GenerateInvite()
    {
        if (_tenant == null) return;

        _generateInviteModel = new GenerateInviteFormModel { ExpiryDays = 7 };
        _generatedInvite = null;
        _showGenerateInviteDialog = true;
    }

    private async Task SubmitGenerateInvite()
    {
        if (_tenant == null) return;

        using var busyScope = UiState.Busy.Begin("Generating invite...");
        UiState.Busy.ClearError();

        try
        {
            var request = new GenerateInviteRequest
            {
                TenantId = _tenant.Id,
                ExpiryDays = _generateInviteModel.ExpiryDays
            };

            var response = await TenantInvitesClient.GenerateInviteAsync(_tenant.OrgId, _tenant.Id, request);

            if (response.Success && response.Data != null)
            {
                _generatedInvite = response.Data;
                _generatedInvite.InviteUrl = $"{NavigationManager.BaseUri}accept-invite?token={_generatedInvite.InviteToken}";
                Snackbar.Add("Invite generated successfully!", Severity.Success);
                await LoadInvitesAsync();
            }
            else
            {
                var message = response.Message ?? "Failed to generate invite.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error generating invite: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task LoadInvitesAsync()
    {
        if (_tenant == null) return;

        try
        {
            var response = await TenantInvitesClient.GetInvitesByTenantAsync(_tenant.OrgId, _tenant.Id);
            if (response.Success && response.Data != null)
            {
                _invites = response.Data.Select(invite => new TenantInviteDto
                {
                    Id = invite.Id,
                    OrgId = invite.OrgId,
                    TenantId = invite.TenantId,
                    InviteToken = invite.InviteToken,
                    InviteUrl = $"{NavigationManager.BaseUri}accept-invite?token={invite.InviteToken}",
                    Phone = invite.Phone,
                    Email = invite.Email,
                    CreatedAtUtc = invite.CreatedAtUtc,
                    ExpiresAtUtc = invite.ExpiresAtUtc,
                    IsUsed = invite.IsUsed,
                    UsedAtUtc = invite.UsedAtUtc,
                    TenantFullName = invite.TenantFullName
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading invites: {ex.Message}", Severity.Warning);
        }
    }

    private async Task CancelInvite(TenantInviteDto invite)
    {
        if (_tenant == null) return;

        bool? confirmed = await ShowConfirmationDialog(
            "Cancel Invite",
            $"Are you sure you want to cancel this invite? This action cannot be undone.");

        if (confirmed != true) return;

        using var busyScope = UiState.Busy.Begin("Canceling invite...");
        UiState.Busy.ClearError();

        try
        {
            var response = await TenantInvitesClient.CancelInviteAsync(_tenant.OrgId, invite.Id);

            if (response.Success)
            {
                Snackbar.Add("Invite canceled successfully!", Severity.Success);
                await LoadInvitesAsync();
            }
            else
            {
                var message = response.Message ?? "Failed to cancel invite.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error canceling invite: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task CopyToClipboard(string text)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            Snackbar.Add("Copied to clipboard!", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Failed to copy to clipboard", Severity.Warning);
        }
    }

    private async Task<bool?> ShowConfirmationDialog(string title, string message)
    {
        var dialog = await DialogService.ShowMessageBox(title, message, "Confirm", "Cancel");
        return dialog;
    }

    private string GetInviteStatus(TenantInviteDto invite)
    {
        if (invite.IsUsed)
            return "Used";
        if (invite.ExpiresAtUtc < DateTime.UtcNow)
            return "Expired";
        return "Pending";
    }

    private Color GetInviteStatusColor(TenantInviteDto invite)
    {
        if (invite.IsUsed)
            return Color.Success;
        if (invite.ExpiresAtUtc < DateTime.UtcNow)
            return Color.Error;
        return Color.Warning;
    }

    private bool CanResendInvite(TenantInviteDto invite)
    {
        return !invite.IsUsed && invite.ExpiresAtUtc < DateTime.UtcNow;
    }

    private bool CanCancelInvite(TenantInviteDto invite)
    {
        return !invite.IsUsed && invite.ExpiresAtUtc >= DateTime.UtcNow;
    }
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

/// <summary>
/// Model for generate invite form.
/// </summary>
public class GenerateInviteFormModel
{
    public int ExpiryDays { get; set; } = 7;
}

