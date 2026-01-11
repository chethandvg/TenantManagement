using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Billing;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

public partial class LeaseBillingSettings : ComponentBase
{
    private SettingsForm? _settings;

    [Parameter]
    public Guid? LeaseId { get; set; }

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (LeaseId.HasValue)
        {
            await LoadSettingsAsync();
        }
    }

    private async Task LoadSettingsAsync()
    {
        if (!LeaseId.HasValue) return;

        using var busyScope = UiState.Busy.Begin("Loading settings...");
        try
        {
            var response = await BillingClient.GetLeaseBillingSettingAsync(LeaseId.Value);
            if (response.Success && response.Data != null)
            {
                _settings = new SettingsForm
                {
                    BillingDay = response.Data.BillingDay,
                    PaymentTermDays = response.Data.PaymentTermDays,
                    GenerateInvoiceAutomatically = response.Data.GenerateInvoiceAutomatically,
                    InvoicePrefix = response.Data.InvoicePrefix,
                    PaymentInstructions = response.Data.PaymentInstructions,
                    Notes = response.Data.Notes,
                    RowVersion = response.Data.RowVersion
                };
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to load settings.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveSettingsAsync()
    {
        if (!LeaseId.HasValue || _settings == null) return;

        // Validate billing day
        if (_settings.BillingDay < 1 || _settings.BillingDay > 28)
        {
            Snackbar.Add("Billing day must be between 1 and 28.", Severity.Warning);
            return;
        }

        // Validate payment term days
        if (_settings.PaymentTermDays < 0 || _settings.PaymentTermDays > 365)
        {
            Snackbar.Add("Payment term days must be between 0 and 365.", Severity.Warning);
            return;
        }

        using var busyScope = UiState.Busy.Begin("Saving settings...");
        try
        {
            var request = new UpdateLeaseBillingSettingRequest
            {
                BillingDay = _settings.BillingDay,
                PaymentTermDays = _settings.PaymentTermDays,
                GenerateInvoiceAutomatically = _settings.GenerateInvoiceAutomatically,
                InvoicePrefix = _settings.InvoicePrefix,
                PaymentInstructions = _settings.PaymentInstructions,
                Notes = _settings.Notes,
                RowVersion = _settings.RowVersion
            };

            var response = await BillingClient.UpdateLeaseBillingSettingAsync(LeaseId.Value, request);
            if (response.Success)
            {
                Snackbar.Add("Settings saved successfully.", Severity.Success);
                await LoadSettingsAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Failed to save settings.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private class SettingsForm
    {
        public byte BillingDay { get; set; }
        public short PaymentTermDays { get; set; }
        public bool GenerateInvoiceAutomatically { get; set; }
        public string? InvoicePrefix { get; set; }
        public string? PaymentInstructions { get; set; }
        public string? Notes { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
