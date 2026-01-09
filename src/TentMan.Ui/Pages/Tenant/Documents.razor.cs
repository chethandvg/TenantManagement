using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

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

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // TODO: Call API to get tenant documents
        await Task.Delay(500);

        // Mock data
        _documents = new List<DocumentViewModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FileName = "Aadhar_Card.pdf",
                DocumentType = "ID Proof",
                UploadedDate = DateTime.Now.AddDays(-10),
                Status = "Verified"
            },
            new()
            {
                Id = Guid.NewGuid(),
                FileName = "Bank_Statement.pdf",
                DocumentType = "Address Proof",
                UploadedDate = DateTime.Now.AddDays(-5),
                Status = "Pending"
            }
        };
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
        _uploadModel.SelectedFile = file;
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
            // TODO: Call API to upload document
            await Task.Delay(1500);

            var newDoc = new DocumentViewModel
            {
                Id = Guid.NewGuid(),
                FileName = _uploadModel.SelectedFile.Name,
                DocumentType = _uploadModel.DocumentType,
                UploadedDate = DateTime.Now,
                Status = "Pending"
            };

            _documents.Add(newDoc);

            Snackbar.Add("Document uploaded successfully!", Severity.Success);
            CloseUploadDialog();
        }
        catch (Exception ex)
        {
            _uploadError = ex.Message;
        }
        finally
        {
            _isUploading = false;
        }
    }

    private async Task DownloadDocument(Guid documentId)
    {
        // TODO: Call API to download document
        await Task.Delay(100);
        Snackbar.Add("Download started", Severity.Info);
    }

    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "Verified" => Color.Success,
            "Pending" => Color.Warning,
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
    }
}
