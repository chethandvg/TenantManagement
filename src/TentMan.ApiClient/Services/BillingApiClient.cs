using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Billing API client.
/// </summary>
public sealed class BillingApiClient : ApiClientServiceBase, IBillingApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BillingApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public BillingApiClient(HttpClient httpClient, ILogger<BillingApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api";

    // Invoice Operations
    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceDto>> GenerateInvoiceAsync(
        Guid leaseId,
        DateOnly? periodStart = null,
        DateOnly? periodEnd = null,
        CancellationToken cancellationToken = default)
    {
        var query = string.Empty;
        if (periodStart.HasValue)
        {
            query += $"?periodStart={periodStart.Value:yyyy-MM-dd}";
            if (periodEnd.HasValue)
            {
                query += $"&periodEnd={periodEnd.Value:yyyy-MM-dd}";
            }
        }

        return PostAsync<object, InvoiceDto>(
            $"leases/{leaseId}/invoices/generate{query}",
            new { },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<InvoiceDto>>> GetInvoicesAsync(
        Guid orgId,
        InvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?orgId={orgId}";
        if (status.HasValue)
        {
            query += $"&status={status.Value}";
        }

        return GetAsync<IEnumerable<InvoiceDto>>(
            $"invoices{query}",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceDto>> GetInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<InvoiceDto>($"invoices/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceDto>> IssueInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<object, InvoiceDto>(
            $"invoices/{id}/issue",
            new { },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceDto>> VoidInvoiceAsync(
        Guid id,
        VoidInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<VoidInvoiceRequest, InvoiceDto>(
            $"invoices/{id}/void",
            request,
            cancellationToken);
    }

    // Invoice Run Operations
    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceRunDto>> CreateMonthlyInvoiceRunAsync(
        Guid orgId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<object, InvoiceRunDto>(
            $"invoice-runs/monthly?orgId={orgId}&periodStart={periodStart:yyyy-MM-dd}",
            new { },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceRunDto>> CreateUtilityInvoiceRunAsync(
        Guid orgId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<object, InvoiceRunDto>(
            $"invoice-runs/utilities?orgId={orgId}&periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}",
            new { },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<InvoiceRunDto>>> GetInvoiceRunsAsync(
        Guid orgId,
        InvoiceRunStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?orgId={orgId}";
        if (status.HasValue)
        {
            query += $"&status={status.Value}";
        }

        return GetAsync<IEnumerable<InvoiceRunDto>>(
            $"invoice-runs{query}",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<InvoiceRunDto>> GetInvoiceRunAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<InvoiceRunDto>($"invoice-runs/{id}", cancellationToken);
    }

    // Recurring Charges Operations
    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<LeaseRecurringChargeDto>>> GetRecurringChargesAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<LeaseRecurringChargeDto>>(
            $"leases/{leaseId}/recurring-charges",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseRecurringChargeDto>> GetRecurringChargeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<LeaseRecurringChargeDto>($"recurring-charges/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseRecurringChargeDto>> CreateRecurringChargeAsync(
        CreateRecurringChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateRecurringChargeRequest, LeaseRecurringChargeDto>(
            "recurring-charges",
            request,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseRecurringChargeDto>> UpdateRecurringChargeAsync(
        Guid id,
        UpdateRecurringChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateRecurringChargeRequest, LeaseRecurringChargeDto>(
            $"recurring-charges/{id}",
            request,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<bool>> DeleteRecurringChargeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"recurring-charges/{id}", cancellationToken);
    }

    // Utility Statements Operations
    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<UtilityStatementDto>>> GetUtilityStatementsAsync(
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<UtilityStatementDto>>(
            $"utility-statements?orgId={orgId}",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UtilityStatementDto>> GetUtilityStatementAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<UtilityStatementDto>($"utility-statements/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UtilityStatementDto>> CreateUtilityStatementAsync(
        CreateUtilityStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateUtilityStatementRequest, UtilityStatementDto>(
            "utility-statements",
            request,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UtilityStatementDto>> UpdateUtilityStatementAsync(
        Guid id,
        UpdateUtilityStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateUtilityStatementRequest, UtilityStatementDto>(
            $"utility-statements/{id}",
            request,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UtilityStatementDto>> FinalizeUtilityStatementAsync(
        Guid id,
        FinalizeUtilityStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<FinalizeUtilityStatementRequest, UtilityStatementDto>(
            $"utility-statements/{id}/finalize",
            request,
            cancellationToken);
    }

    // Lease Billing Settings Operations
    /// <inheritdoc/>
    public Task<ApiResponse<LeaseBillingSettingDto>> GetLeaseBillingSettingAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<LeaseBillingSettingDto>(
            $"leases/{leaseId}/billing-settings",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseBillingSettingDto>> UpdateLeaseBillingSettingAsync(
        Guid leaseId,
        UpdateLeaseBillingSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateLeaseBillingSettingRequest, LeaseBillingSettingDto>(
            $"leases/{leaseId}/billing-settings",
            request,
            cancellationToken);
    }

    // Charge Types Operations
    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<ChargeTypeDto>>> GetChargeTypesAsync(
        Guid? orgId = null,
        CancellationToken cancellationToken = default)
    {
        var query = orgId.HasValue ? $"?orgId={orgId}" : string.Empty;
        return GetAsync<IEnumerable<ChargeTypeDto>>(
            $"charge-types{query}",
            cancellationToken);
    }
}
