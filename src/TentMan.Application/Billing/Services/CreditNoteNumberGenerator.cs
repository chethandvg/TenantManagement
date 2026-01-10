using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Thread-safe service for generating unique credit note numbers.
/// </summary>
public class CreditNoteNumberGenerator : ICreditNoteNumberGenerator
{
    private readonly INumberSequenceRepository _numberSequenceRepository;
    private const string SequenceType = "CreditNote";
    private const string DefaultPrefix = "CN";

    public CreditNoteNumberGenerator(INumberSequenceRepository numberSequenceRepository)
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
        var creditNoteNumber = $"{effectivePrefix}-{yearMonth}-{sequenceNumber:D6}";

        return creditNoteNumber;
    }
}
