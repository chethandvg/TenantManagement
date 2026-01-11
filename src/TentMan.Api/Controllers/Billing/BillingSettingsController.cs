using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Domain.Entities;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing lease billing settings.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class BillingSettingsController : ControllerBase
{
    private readonly ILeaseBillingSettingRepository _billingSettingRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly ILogger<BillingSettingsController> _logger;

    public BillingSettingsController(
        ILeaseBillingSettingRepository billingSettingRepository,
        ILeaseRepository leaseRepository,
        ILogger<BillingSettingsController> logger)
    {
        _billingSettingRepository = billingSettingRepository;
        _leaseRepository = leaseRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets the billing settings for a lease.
    /// </summary>
    [HttpGet("api/leases/{leaseId:guid}/billing-settings")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<LeaseBillingSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LeaseBillingSettingDto>>> GetBillingSettings(
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting billing settings for lease {LeaseId}", leaseId);

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(leaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        var settings = await _billingSettingRepository.GetByLeaseIdAsync(leaseId, cancellationToken);
        
        if (settings == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Billing settings for lease {leaseId} not found"));
        }

        var dto = new LeaseBillingSettingDto
        {
            Id = settings.Id,
            LeaseId = settings.LeaseId,
            BillingDay = settings.BillingDay,
            PaymentTermDays = settings.PaymentTermDays,
            GenerateInvoiceAutomatically = settings.GenerateInvoiceAutomatically,
            InvoicePrefix = settings.InvoicePrefix,
            PaymentInstructions = settings.PaymentInstructions,
            Notes = settings.Notes,
            RowVersion = settings.RowVersion
        };

        return Ok(ApiResponse<LeaseBillingSettingDto>.Ok(dto, "Billing settings retrieved successfully"));
    }

    /// <summary>
    /// Updates the billing settings for a lease.
    /// Only admins and managers can update billing settings.
    /// </summary>
    [HttpPut("api/leases/{leaseId:guid}/billing-settings")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<LeaseBillingSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LeaseBillingSettingDto>>> UpdateBillingSettings(
        Guid leaseId,
        UpdateLeaseBillingSettingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating billing settings for lease {LeaseId}", leaseId);

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(leaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        var settings = await _billingSettingRepository.GetByLeaseIdAsync(leaseId, cancellationToken);

        if (settings == null)
        {
            // Create new billing settings if they don't exist
            settings = new LeaseBillingSetting
            {
                LeaseId = leaseId,
                BillingDay = request.BillingDay,
                PaymentTermDays = request.PaymentTermDays,
                GenerateInvoiceAutomatically = request.GenerateInvoiceAutomatically,
                InvoicePrefix = request.InvoicePrefix,
                PaymentInstructions = request.PaymentInstructions,
                Notes = request.Notes
            };

            await _billingSettingRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            // Update existing settings
            settings.BillingDay = request.BillingDay;
            settings.PaymentTermDays = request.PaymentTermDays;
            settings.GenerateInvoiceAutomatically = request.GenerateInvoiceAutomatically;
            settings.InvoicePrefix = request.InvoicePrefix;
            settings.PaymentInstructions = request.PaymentInstructions;
            settings.Notes = request.Notes;
            settings.ModifiedAtUtc = DateTime.UtcNow;

            try
            {
                await _billingSettingRepository.UpdateAsync(settings, request.RowVersion, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest(ApiResponse<object>.Fail("The billing settings were modified by another process. Please retry."));
            }
        }

        var dto = new LeaseBillingSettingDto
        {
            Id = settings.Id,
            LeaseId = settings.LeaseId,
            BillingDay = settings.BillingDay,
            PaymentTermDays = settings.PaymentTermDays,
            GenerateInvoiceAutomatically = settings.GenerateInvoiceAutomatically,
            InvoicePrefix = settings.InvoicePrefix,
            PaymentInstructions = settings.PaymentInstructions,
            Notes = settings.Notes,
            RowVersion = settings.RowVersion
        };

        return Ok(ApiResponse<LeaseBillingSettingDto>.Ok(dto, "Billing settings updated successfully"));
    }
}
