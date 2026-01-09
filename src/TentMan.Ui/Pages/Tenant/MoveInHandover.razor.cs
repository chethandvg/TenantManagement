using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.TenantPortal;
using Microsoft.JSInterop;

namespace TentMan.Ui.Pages.Tenant;

public partial class MoveInHandover : ComponentBase
{
    private MoveInHandoverResponse? _handover;
    private bool _isSubmitting;
    private bool _isSubmitted;
    private string? _error;
    private string? _signatureDataUrl;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public ITenantPortalApiClient TenantPortalApi { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var response = await TenantPortalApi.GetMoveInHandoverAsync();
            
            if (response.Success && response.Data != null)
            {
                _handover = response.Data;
                _isSubmitted = _handover.IsCompleted;
            }
            else
            {
                // No handover found - this is normal
                _handover = null;
            }
        }
        catch (Exception ex)
        {
            _error = $"Error loading handover: {ex.Message}";
        }
    }

    private async Task SubmitHandover()
    {
        if (_handover == null)
        {
            _error = "No handover data available";
            return;
        }

        if (string.IsNullOrWhiteSpace(_signatureDataUrl))
        {
            _error = "Please provide your signature";
            return;
        }

        _isSubmitting = true;
        _error = null;

        try
        {
            // Convert signature data URL to stream
            var signatureBytes = Convert.FromBase64String(_signatureDataUrl.Split(',')[1]);
            using var signatureStream = new MemoryStream(signatureBytes);

            // Prepare submission request
            var request = new SubmitHandoverRequest
            {
                HandoverId = _handover.HandoverId,
                Notes = _handover.Notes,
                ChecklistItems = _handover.ChecklistItems.Select(ci => new HandoverChecklistItemUpdateDto
                {
                    Id = ci.Id,
                    Condition = ci.Condition,
                    Remarks = ci.Remarks
                }).ToList(),
                MeterReadings = _handover.MeterReadings.Select(mr => new MeterReadingUpdateDto
                {
                    MeterId = mr.MeterId,
                    Reading = mr.Reading,
                    ReadingDate = mr.ReadingDate
                }).ToList()
            };

            var response = await TenantPortalApi.SubmitMoveInHandoverAsync(
                request,
                signatureStream,
                "signature.png",
                "image/png");

            if (response.Success && response.Data != null)
            {
                _handover = response.Data;
                _isSubmitted = true;
                Snackbar.Add("Handover checklist submitted successfully!", Severity.Success);
            }
            else
            {
                _error = response.Message ?? "Failed to submit handover";
            }
        }
        catch (Exception ex)
        {
            _error = $"Error submitting handover: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task ClearSignature()
    {
        _signatureDataUrl = null;
        await JS.InvokeVoidAsync("clearSignaturePad");
    }

    private async Task OnSignatureChanged(string dataUrl)
    {
        _signatureDataUrl = dataUrl;
        await Task.CompletedTask;
    }
}
