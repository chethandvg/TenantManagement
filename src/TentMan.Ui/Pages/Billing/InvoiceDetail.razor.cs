using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice detail page showing full invoice information with actions.
/// </summary>
public partial class InvoiceDetail : ComponentBase
{
    private InvoiceDto? _invoice;
    private bool _showVoidDialog;
    private string _voidReason = string.Empty;
    private readonly DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading invoice...");
        UiState.Busy.ClearError();
        _invoice = null;

        try
        {
            var response = await BillingClient.GetInvoiceAsync(Id);

            if (response.Success && response.Data != null)
            {
                _invoice = response.Data;
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load invoice.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading invoice: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task IssueInvoiceAsync()
    {
        using var busyScope = UiState.Busy.Begin("Issuing invoice...");

        try
        {
            var response = await BillingClient.IssueInvoiceAsync(Id);

            if (response.Success && response.Data != null)
            {
                _invoice = response.Data;
                Snackbar.Add("Invoice issued successfully.", Severity.Success);
            }
            else
            {
                var message = response.Message ?? "Failed to issue invoice.";
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error issuing invoice: {ex.Message}", Severity.Error);
        }
    }

    private Task ShowVoidDialogAsync()
    {
        _voidReason = string.Empty;
        _showVoidDialog = true;
        return Task.CompletedTask;
    }

    private void CloseVoidDialog()
    {
        _showVoidDialog = false;
        _voidReason = string.Empty;
    }

    private async Task VoidInvoiceAsync()
    {
        if (string.IsNullOrWhiteSpace(_voidReason))
        {
            Snackbar.Add("Please provide a void reason.", Severity.Warning);
            return;
        }

        using var busyScope = UiState.Busy.Begin("Voiding invoice...");
        CloseVoidDialog();

        try
        {
            var request = new VoidInvoiceRequest { VoidReason = _voidReason };
            var response = await BillingClient.VoidInvoiceAsync(Id, request);

            if (response.Success && response.Data != null)
            {
                _invoice = response.Data;
                Snackbar.Add("Invoice voided successfully.", Severity.Success);
            }
            else
            {
                var message = response.Message ?? "Failed to void invoice.";
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error voiding invoice: {ex.Message}", Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceStatus status)
    {
        var (color, text) = status switch
        {
            InvoiceStatus.Draft => (Color.Default, "Draft"),
            InvoiceStatus.Issued => (Color.Primary, "Issued"),
            InvoiceStatus.PartiallyPaid => (Color.Info, "Partially Paid"),
            InvoiceStatus.Paid => (Color.Success, "Paid"),
            InvoiceStatus.Overdue => (Color.Error, "Overdue"),
            InvoiceStatus.Voided => (Color.Warning, "Voided"),
            _ => (Color.Default, status.ToString())
        };

        return builder =>
        {
            builder.OpenComponent<MudChip<string>>(0);
            builder.AddAttribute(1, "Color", color);
            builder.AddAttribute(2, "Size", Size.Small);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(0, text);
            }));
            builder.CloseComponent();
        };
    }
}
