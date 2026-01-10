using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class InvoiceNumberGeneratorTests
{
    private readonly Mock<INumberSequenceRepository> _mockRepository;
    private readonly InvoiceNumberGenerator _generator;

    public InvoiceNumberGeneratorTests()
    {
        _mockRepository = new Mock<INumberSequenceRepository>();
        _generator = new InvoiceNumberGenerator(_mockRepository.Object);
    }

    [Fact]
    public async Task GenerateNextAsync_WithDefaultPrefix_ReturnsCorrectFormat()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("INV-");
        result.Should().EndWith("-000001");
        result.Should().MatchRegex(@"^INV-\d{6}-\d{6}$");
    }

    [Fact]
    public async Task GenerateNextAsync_WithCustomPrefix_UsesCustomPrefix()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 42L;
        var customPrefix = "CUST";
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, customPrefix);

        // Assert
        result.Should().StartWith("CUST-");
        result.Should().EndWith("-000042");
        result.Should().MatchRegex(@"^CUST-\d{6}-\d{6}$");
    }

    [Fact]
    public async Task GenerateNextAsync_IncrementingSequence_GeneratesUniqueNumbers()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequence = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
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
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
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
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
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
        var prefixWithSpaces = "  TRIM  ";
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, prefixWithSpaces);

        // Assert
        result.Should().StartWith("TRIM-");
        result.Should().NotContain("  ");
    }

    [Fact]
    public async Task GenerateNextAsync_WithEmptyPrefix_UsesDefaultPrefix()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNumber);

        // Act
        var result = await _generator.GenerateNextAsync(orgId, "");

        // Assert
        result.Should().StartWith("INV-");
    }

    [Fact]
    public async Task GenerateNextAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sequenceNumber = 1L;
        var cancellationToken = new CancellationToken();
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(orgId, "Invoice", cancellationToken))
            .ReturnsAsync(sequenceNumber);

        // Act
        await _generator.GenerateNextAsync(orgId, cancellationToken: cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.GetNextSequenceNumberAsync(orgId, "Invoice", cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GenerateNextAsync_MultipleOrganizations_MaintainsSeparateSequences()
    {
        // Arrange
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(org1, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        
        _mockRepository
            .Setup(r => r.GetNextSequenceNumberAsync(org2, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);

        // Act
        var result1 = await _generator.GenerateNextAsync(org1);
        var result2 = await _generator.GenerateNextAsync(org2);

        // Assert
        result1.Should().EndWith("-000001");
        result2.Should().EndWith("-000100");
    }
}
