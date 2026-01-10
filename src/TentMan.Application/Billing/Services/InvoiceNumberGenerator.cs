using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Thread-safe service for generating unique invoice numbers.
/// </summary>
public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly INumberSequenceRepository _numberSequenceRepository;
    private const string SequenceType = "Invoice";
    private const string DefaultPrefix = "INV";

    public InvoiceNumberGenerator(INumberSequenceRepository numberSequenceRepository)
    {
        _numberSequenceRepository = numberSequenceRepository;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateNextAsync(
        Guid orgId,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var sequenceNumber = await _numberSequenceRepository.GetNextSequenceNumberAsync(
            orgId,
            SequenceType,
            cancellationToken);

        var effectivePrefix = string.IsNullOrWhiteSpace(prefix) ? DefaultPrefix : prefix.Trim();
        
        // Format: PREFIX-YYYYMM-NNNNNN
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        var invoiceNumber = $"{effectivePrefix}-{yearMonth}-{sequenceNumber:D6}";

        return invoiceNumber;
    }
}
