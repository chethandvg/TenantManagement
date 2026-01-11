using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Billing API client.
/// Provides methods for managing invoices, invoice runs, recurring charges, utility statements, and billing settings.
/// </summary>
public interface IBillingApiClient
{
    // Invoice Operations
    Task<ApiResponse<InvoiceDto>> GenerateInvoiceAsync(
        Guid leaseId,
        DateOnly? periodStart = null,
        DateOnly? periodEnd = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<InvoiceDto>>> GetInvoicesAsync(
        Guid orgId,
        InvoiceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceDto>> GetInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceDto>> IssueInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceDto>> VoidInvoiceAsync(
        Guid id,
        VoidInvoiceRequest request,
        CancellationToken cancellationToken = default);

    // Invoice Run Operations
    Task<ApiResponse<InvoiceRunDto>> CreateMonthlyInvoiceRunAsync(
        Guid orgId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceRunDto>> CreateUtilityInvoiceRunAsync(
        Guid orgId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<InvoiceRunDto>>> GetInvoiceRunsAsync(
        Guid orgId,
        InvoiceRunStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceRunDto>> GetInvoiceRunAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Recurring Charges Operations
    Task<ApiResponse<IEnumerable<LeaseRecurringChargeDto>>> GetRecurringChargesAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LeaseRecurringChargeDto>> GetRecurringChargeAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LeaseRecurringChargeDto>> CreateRecurringChargeAsync(
        Guid leaseId,
        CreateRecurringChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LeaseRecurringChargeDto>> UpdateRecurringChargeAsync(
        Guid id,
        UpdateRecurringChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteRecurringChargeAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Utility Statements Operations
    Task<ApiResponse<IEnumerable<UtilityStatementDto>>> GetUtilityStatementsAsync(
        Guid orgId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UtilityStatementDto>> GetUtilityStatementAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UtilityStatementDto>> CreateUtilityStatementAsync(
        CreateUtilityStatementRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UtilityStatementDto>> UpdateUtilityStatementAsync(
        Guid id,
        UpdateUtilityStatementRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UtilityStatementDto>> FinalizeUtilityStatementAsync(
        Guid id,
        FinalizeUtilityStatementRequest request,
        CancellationToken cancellationToken = default);

    // Lease Billing Settings Operations
    Task<ApiResponse<LeaseBillingSettingDto>> GetLeaseBillingSettingAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LeaseBillingSettingDto>> UpdateLeaseBillingSettingAsync(
        Guid leaseId,
        UpdateLeaseBillingSettingRequest request,
        CancellationToken cancellationToken = default);

    // Charge Types Operations
    Task<ApiResponse<IEnumerable<ChargeTypeDto>>> GetChargeTypesAsync(
        Guid? orgId = null,
        CancellationToken cancellationToken = default);
}
