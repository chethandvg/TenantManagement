using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Enums;

namespace TentMan.Ui.Components.Common;

/// <summary>
/// A reusable file upload component with drag-drop support, tagging, and thumbnails.
/// </summary>
public partial class FileUploader : ComponentBase
{
    /// <summary>
    /// The title displayed at the top.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Documents";

    /// <summary>
    /// Help text displayed in the upload area.
    /// </summary>
    [Parameter]
    public string HelpText { get; set; } = "Supported formats: Images, PDFs, Documents";

    /// <summary>
    /// Accepted file types (e.g., ".jpg,.png,.pdf").
    /// </summary>
    [Parameter]
    public string AcceptedFileTypes { get; set; } = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx";

    /// <summary>
    /// Maximum number of files allowed.
    /// </summary>
    [Parameter]
    public int MaxFiles { get; set; } = 10;

    /// <summary>
    /// Maximum file size in bytes.
    /// </summary>
    [Parameter]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Whether to show thumbnails for image files.
    /// </summary>
    [Parameter]
    public bool ShowThumbnails { get; set; } = true;

    /// <summary>
    /// The list of uploaded files.
    /// </summary>
    [Parameter]
    public List<FileUploadModel> Files { get; set; } = new();

    /// <summary>
    /// Callback when files list changes.
    /// </summary>
    [Parameter]
    public EventCallback<List<FileUploadModel>> FilesChanged { get; set; }

    /// <summary>
    /// Callback when a file is uploaded.
    /// </summary>
    [Parameter]
    public EventCallback<IBrowserFile> OnFileUploaded { get; set; }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            if (file.Size > MaxFileSizeBytes)
            {
                continue;
            }

            var fileModel = new FileUploadModel
            {
                FileId = Guid.NewGuid(),
                FileName = file.Name,
                ContentType = file.ContentType,
                SizeBytes = file.Size,
                FileTag = DetermineFileTag(file),
                BrowserFile = file
            };

            Files.Add(fileModel);

            if (OnFileUploaded.HasDelegate)
            {
                await OnFileUploaded.InvokeAsync(file);
            }
        }

        await FilesChanged.InvokeAsync(Files);
    }

    private FileTag DetermineFileTag(IBrowserFile file)
    {
        if (file.ContentType.StartsWith("image/"))
        {
            return FileTag.Photo;
        }

        return FileTag.Document;
    }

    private bool IsImageFile(FileUploadModel file)
    {
        return file.ContentType.StartsWith("image/");
    }

    private string GetFilePreviewUrl(FileUploadModel file)
    {
        // In a real implementation, this would return a data URL or blob URL
        return string.Empty;
    }

    private string GetFileIcon(FileUploadModel file)
    {
        return file.ContentType switch
        {
            var ct when ct.StartsWith("image/") => MudBlazor.Icons.Material.Filled.Image,
            var ct when ct.Contains("pdf") => MudBlazor.Icons.Material.Filled.PictureAsPdf,
            var ct when ct.Contains("word") || ct.Contains("document") => MudBlazor.Icons.Material.Filled.Description,
            _ => MudBlazor.Icons.Material.Filled.InsertDriveFile
        };
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

    private async Task OnTagChanged(FileUploadModel file, FileTag newTag)
    {
        file.FileTag = newTag;
        await FilesChanged.InvokeAsync(Files);
    }

    private async Task RemoveFile(FileUploadModel file)
    {
        Files.Remove(file);
        await FilesChanged.InvokeAsync(Files);
    }
}

/// <summary>
/// Model for file upload tracking.
/// </summary>
public class FileUploadModel
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public FileTag FileTag { get; set; }
    public int SortOrder { get; set; }
    public IBrowserFile? BrowserFile { get; set; }
}
