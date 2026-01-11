using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// Request to issue an invoice.
/// Note: Concurrency control is handled internally by the service.
/// </summary>
public sealed class IssueInvoiceRequest
{
    // No parameters needed - the service handles concurrency internally
}
