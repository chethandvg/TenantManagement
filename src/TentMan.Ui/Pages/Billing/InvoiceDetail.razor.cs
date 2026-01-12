using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Contracts.Payments;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice detail page showing full invoice information with actions.
/// </summary>
public partial class InvoiceDetail : ComponentBase
{
    private InvoiceDto? _invoice;
    private List<PaymentDto> _payments = new();
    private bool _showVoidDialog;
    private bool _showPaymentDialog;
    private string _voidReason = string.Empty;
    
    // Payment dialog fields
    private PaymentMode _paymentMode = PaymentMode.Cash;
    private decimal _paymentAmount;
    private DateTime? _paymentDate = DateTime.Today;
    private string? _payerName;
    private string? _transactionReference;
    private string? _receiptNumber;
    private string? _paymentNotes;
    
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

    [Inject]
    public ILogger<InvoiceDetail> Logger { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceAsync();
        await LoadPaymentsAsync();
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

    private async Task LoadPaymentsAsync()
    {
        try
        {
            var response = await BillingClient.GetInvoicePaymentsAsync(Id);

            if (response.Success && response.Data != null)
            {
                _payments = response.Data.ToList();
            }
        }
        catch (Exception ex)
        {
            // Silently fail - payments list just won't show, but log the error
            Logger.LogWarning(ex, "Error loading payments for invoice {InvoiceId}", Id);
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
        return BillingStatusHelper.GetInvoiceStatusChip(status);
    }

    private RenderFragment GetPaymentStatusChip(PaymentStatus status)
    {
        var (color, icon) = status switch
        {
            PaymentStatus.Completed => (Color.Success, Icons.Material.Filled.CheckCircle),
            PaymentStatus.Pending => (Color.Warning, Icons.Material.Filled.Schedule),
            PaymentStatus.Processing => (Color.Info, Icons.Material.Filled.HourglassEmpty),
            PaymentStatus.Failed => (Color.Error, Icons.Material.Filled.Error),
            PaymentStatus.Cancelled => (Color.Default, Icons.Material.Filled.Cancel),
            PaymentStatus.Refunded => (Color.Secondary, Icons.Material.Filled.Undo),
            _ => (Color.Default, Icons.Material.Filled.QuestionMark)
        };

        return builder =>
        {
            builder.OpenComponent<MudChip<string>>(0);
            builder.AddAttribute(1, "Size", Size.Small);
            builder.AddAttribute(2, "Color", color);
            builder.AddAttribute(3, "Icon", icon);
            builder.AddAttribute(4, "Text", status.ToString());
            builder.CloseComponent();
        };
    }

    private Task ShowRecordPaymentDialogAsync()
    {
        // Reset dialog fields
        _paymentMode = PaymentMode.Cash;
        _paymentAmount = _invoice?.BalanceAmount ?? 0;
        _paymentDate = DateTime.Today;
        _payerName = null;
        _transactionReference = null;
        _receiptNumber = null;
        _paymentNotes = null;
        
        _showPaymentDialog = true;
        return Task.CompletedTask;
    }

    private void ClosePaymentDialog()
    {
        _showPaymentDialog = false;
    }

    private async Task RecordPaymentAsync()
    {
        // Validate
        if (_paymentAmount <= 0)
        {
            Snackbar.Add("Payment amount must be greater than zero.", Severity.Warning);
            return;
        }

        if (!_paymentDate.HasValue)
        {
            Snackbar.Add("Please select a payment date.", Severity.Warning);
            return;
        }

        if (_paymentMode != PaymentMode.Cash && string.IsNullOrWhiteSpace(_transactionReference))
        {
            Snackbar.Add("Transaction reference is required for non-cash payments.", Severity.Warning);
            return;
        }

        using var busyScope = UiState.Busy.Begin("Recording payment...");
        ClosePaymentDialog();

        try
        {
            if (_paymentMode == PaymentMode.Cash)
            {
                var request = new RecordCashPaymentRequest
                {
                    Amount = _paymentAmount,
                    PaymentDate = _paymentDate.Value,
                    PayerName = _payerName,
                    ReceiptNumber = _receiptNumber,
                    Notes = _paymentNotes
                };

                var response = await BillingClient.RecordCashPaymentAsync(Id, request);

                if (response.Success)
                {
                    Snackbar.Add(response.Message ?? "Cash payment recorded successfully.", Severity.Success);
                    await LoadInvoiceAsync();
                    await LoadPaymentsAsync();
                }
                else
                {
                    var message = response.Message ?? "Failed to record cash payment.";
                    Snackbar.Add(message, Severity.Error);
                }
            }
            else
            {
                var request = new RecordOnlinePaymentRequest
                {
                    PaymentMode = _paymentMode,
                    Amount = _paymentAmount,
                    PaymentDate = _paymentDate.Value,
                    TransactionReference = _transactionReference!,
                    PayerName = _payerName,
                    Notes = _paymentNotes
                };

                var response = await BillingClient.RecordOnlinePaymentAsync(Id, request);

                if (response.Success)
                {
                    Snackbar.Add(response.Message ?? "Online payment recorded successfully.", Severity.Success);
                    await LoadInvoiceAsync();
                    await LoadPaymentsAsync();
                }
                else
                {
                    var message = response.Message ?? "Failed to record online payment.";
                    Snackbar.Add(message, Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error recording payment: {ex.Message}", Severity.Error);
        }
    }
}
