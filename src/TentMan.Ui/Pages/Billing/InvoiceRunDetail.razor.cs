using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Invoice run detail page showing run results and per-lease items.
/// </summary>
public partial class InvoiceRunDetail : ComponentBase
{
    private InvoiceRunDto? _invoiceRun;

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public IBillingApiClient BillingClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceRunAsync();
    }

    private async Task LoadInvoiceRunAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading invoice run...");
        UiState.Busy.ClearError();
        _invoiceRun = null;

        try
        {
            var response = await BillingClient.GetInvoiceRunAsync(Id);

            if (response.Success && response.Data != null)
            {
                _invoiceRun = response.Data;
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load invoice run.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading invoice run: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private RenderFragment GetStatusChip(InvoiceRunStatus status)
    {
        var (color, text) = status switch
        {
            InvoiceRunStatus.Pending => (Color.Default, "Pending"),
            InvoiceRunStatus.InProgress => (Color.Info, "In Progress"),
            InvoiceRunStatus.Completed => (Color.Success, "Completed"),
            InvoiceRunStatus.CompletedWithErrors => (Color.Warning, "Completed With Errors"),
            InvoiceRunStatus.Failed => (Color.Error, "Failed"),
            InvoiceRunStatus.Cancelled => (Color.Secondary, "Cancelled"),
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
