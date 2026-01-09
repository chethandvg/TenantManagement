using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Tenants;

namespace TentMan.Ui.Pages.Tenant;

public partial class Documents : ComponentBase
{
    private List<DocumentViewModel> _documents = new();
    private bool _uploadDialogVisible;
    private readonly UploadModel _uploadModel = new();
    private bool _isUploading;
    private string? _uploadError;
    private readonly DialogOptions _dialogOptions = new() { CloseOnEscapeKey = true };

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public ITenantPortalApiClient TenantPortalApiClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var response = await TenantPortalApiClient.GetDocumentsAsync();

            if (response.Success && response.Data != null)
            {
                _documents = response.Data.Select(d => new DocumentViewModel
                {
                    Id = d.Id,
                    FileName = d.FileName ?? "Unknown",
                    DocumentType = d.DocType.ToString(),
                    UploadedDate = DateTime.Now, // Would need CreatedAt from server
                    Status = "Uploaded" // Would need status from server
                }).ToList();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to load documents", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading documents: {ex.Message}", Severity.Error);
        }
    }

    private void OpenUploadDialog()
    {
        _uploadModel.DocumentType = "";
        _uploadModel.SelectedFile = null;
        _uploadError = null;
        _uploadDialogVisible = true;
    }

    private void CloseUploadDialog()
    {
        _uploadDialogVisible = false;
    }

    private void HandleFileSelected(IBrowserFile file)
    {
        const long maxFileSize = 10 * 1024 * 1024; // 10MB

        if (file.Size > maxFileSize)
        {
            _uploadError = $"File size exceeds maximum allowed size of 10MB";
            return;
        }

        _uploadModel.SelectedFile = file;
        _uploadError = null;
    }

    private async Task UploadDocument()
    {
        if (_uploadModel.SelectedFile == null || string.IsNullOrEmpty(_uploadModel.DocumentType))
        {
            _uploadError = "Please select a file and document type";
            return;
        }

        _isUploading = true;
        _uploadError = null;

        try
        {
            using var stream = _uploadModel.SelectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);

            var request = new TenantDocumentUploadRequest
            {
                DocType = Enum.Parse<DocumentType>(_uploadModel.DocumentType),
                Notes = _uploadModel.Notes
            };

            var response = await TenantPortalApiClient.UploadDocumentAsync(
                stream,
                _uploadModel.SelectedFile.Name,
                _uploadModel.SelectedFile.ContentType,
                request);

            if (response.Success && response.Data != null)
            {
                var newDoc = new DocumentViewModel
                {
                    Id = response.Data.Id,
                    FileName = response.Data.FileName ?? _uploadModel.SelectedFile.Name,
                    DocumentType = response.Data.DocType.ToString(),
                    UploadedDate = DateTime.Now,
                    Status = "Uploaded"
                };

                _documents.Insert(0, newDoc);

                Snackbar.Add("Document uploaded successfully!", Severity.Success);
                CloseUploadDialog();
            }
            else
            {
                _uploadError = response.Message ?? "Upload failed";
            }
        }
        catch (Exception ex)
        {
            _uploadError = $"Upload error: {ex.Message}";
        }
        finally
        {
            _isUploading = false;
        }
    }

    private async Task DownloadDocument(Guid documentId)
    {
        // TODO: Implement download functionality
        await Task.Delay(100);
        Snackbar.Add("Download functionality coming soon", Severity.Info);
    }

    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "Verified" => Color.Success,
            "Uploaded" or "Pending" => Color.Warning,
            "Rejected" => Color.Error,
            _ => Color.Default
        };
    }

    private sealed class DocumentViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class UploadModel
    {
        public string DocumentType { get; set; } = string.Empty;
        public IBrowserFile? SelectedFile { get; set; }
        public string? Notes { get; set; }
    }
}
