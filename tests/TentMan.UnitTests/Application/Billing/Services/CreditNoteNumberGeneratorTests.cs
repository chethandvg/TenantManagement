using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class CreditNoteNumberGeneratorTests
{
    private readonly Mock<INumberSequenceRepository> _mockRepository;
    private readonly CreditNoteNumberGenerator _generator;

    public CreditNoteNumberGeneratorTests()
    {
        _mockRepository = new Mock<INumberSequenceRepository>();
        _generator = new CreditNoteNumberGenerator(_mockRepository.Object);
    }

    [Fact]
    public async Task GenerateNextAsync_WithDefaultPrefix_ReturnsCorrectFormat()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("CN-");
        result.Should().EndWith("-000001");
        result.Should().MatchRegex(@"^CN-\d{6}-\d{6}$");
    }

    [Fact]
    public async Task GenerateNextAsync_WithCustomPrefix_UsesCustomPrefix()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 42L;
        var customPrefix = "CR";
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, customPrefix);

        // Assert
        result.Should().StartWith("CR-");
        result.Should().EndWith("-000042");
        result.Should().MatchRegex(@"^CR-\d{6}-\d{6}$");
    }

    [Fact]
    public async Task GenerateNextAsync_IncrementingSequence_GeneratesUniqueNumbers()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequence = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => sequence++)
            .Callback(() => { });

        // Act
        var result1 = await _generator.GenerateNextAsync(orgId);
        var result2 = await _generator.GenerateNextAsync(orgId);
        var result3 = await _generator.GenerateNextAsync(orgId);

        // Assert
        result1.Should().EndWith("-000001");
        result2.Should().EndWith("-000002");
        result3.Should().EndWith("-000003");
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
    }

    [Fact]
    public async Task GenerateNextAsync_LargeSequenceNumber_FormatsCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 999999L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId);

        // Assert
        result.Should().EndWith("-999999");
    }

    [Fact]
    public async Task GenerateNextAsync_IncludesYearMonth_InCorrectFormat()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId);

        // Assert
        var currentYearMonth = DateTime.UtcNow.ToString("yyyyMM");
        result.Should().Contain(currentYearMonth);
    }

    [Fact]
    public async Task GenerateNextAsync_WithWhitespacePrefix_TrimsPrefix()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        var prefixWithSpaces = "  CREDIT  ";
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, prefixWithSpaces);

        // Assert
        result.Should().StartWith("CREDIT-");
        result.Should().NotContain("  ");
    }

    [Fact]
    public async Task GenerateNextAsync_WithEmptyPrefix_UsesDefaultPrefix()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, "");

        // Assert
        result.Should().StartWith("CN-");
    }

    [Fact]
    public async Task GenerateNextAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        var cancellationToken = new CancellationToken();
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", cancellationToken))
            .ReturnsAsync(sequenceNumber);

        // Act
        await _generator.GenerateNextAsync(orgId, cancellationToken: cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GenerateNextAsync_MultipleOrganizations_MaintainsSeparateSequences()
    {
        // Arrange
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(org1, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(org2, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);

        // Act
        var result1 = await _generator.GenerateNextAsync(org1);
        var result2 = await _generator.GenerateNextAsync(org2);

        // Assert
        result1.Should().EndWith("-000001");
        result2.Should().EndWith("-000100");
    }

    [Fact]
    public async Task GenerateNextAsync_DifferentFromInvoiceNumbers_UsesSeparateSequence()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        var result = await _generator.GenerateNextAsync(orgId);

        // Assert
        // Verify it calls with "CreditNote" not "Invoice"
        _mockRepository.Verify(
            r => r.GetNextSequenceNumberAsync(orgId, "CreditNote", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRepository.Verify(
            r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
