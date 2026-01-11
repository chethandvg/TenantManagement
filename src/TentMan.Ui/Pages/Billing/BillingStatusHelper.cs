using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.Contracts.Enums;

namespace TentMan.Ui.Pages.Billing;

/// <summary>
/// Helper class for rendering status chips consistently across billing pages.
/// </summary>
public static class BillingStatusHelper
{
    /// <summary>
    /// Gets a colored chip for an invoice status.
    /// </summary>
    public static RenderFragment GetInvoiceStatusChip(InvoiceStatus status)
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

        return CreateChip(color, text);
    }

    /// <summary>
    /// Gets a colored chip for an invoice run status.
    /// </summary>
    public static RenderFragment GetInvoiceRunStatusChip(InvoiceRunStatus status)
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

        return CreateChip(color, text);
    }

    private static RenderFragment CreateChip(Color color, string text)
    {
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
