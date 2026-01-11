using TentMan.Application.Abstractions;
using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class CreditNoteServiceTests
{
    private readonly Mock<ICreditNoteRepository> _mockCreditNoteRepository;
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<ICreditNoteNumberGenerator> _mockNumberGenerator;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly CreditNoteService _service;

    public CreditNoteServiceTests()
    {
        _mockCreditNoteRepository = new Mock<ICreditNoteRepository>();
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _mockNumberGenerator = new Mock<ICreditNoteNumberGenerator>();
        _mockDbContext = new Mock<IApplicationDbContext>();

        _service = new CreditNoteService(
            _mockCreditNoteRepository.Object,
            _mockInvoiceRepository.Object,
            _mockNumberGenerator.Object,
            _mockDbContext.Object);
    }

    #region CreateCreditNoteAsync Tests

    [Fact]
    public async Task CreateCreditNoteAsync_IssuedInvoiceWithValidLines_CreatesSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest
            {
                InvoiceLineId = invoice.Lines.First().Id,
                Amount = 500m,
                Notes = "Partial credit"
            }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockNumberGenerator
            .Setup(g => g.GenerateNextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CN-202401-000001");

        _mockCreditNoteRepository
            .Setup(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditNote cn, CancellationToken _) => cn);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateCreditNoteAsync(
            invoiceId,
            CreditNoteReason.Discount,
            lineItems,
            "Test credit note");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CreditNote.Should().NotBeNull();
        result.CreditNote!.InvoiceId.Should().Be(invoiceId);
        result.CreditNote.Reason.Should().Be(CreditNoteReason.Discount);
        result.CreditNote.Lines.Should().HaveCount(1);
        result.CreditNote.Lines.First().TotalAmount.Should().Be(-500m); // Negative for credit
        result.CreditNote.TotalAmount.Should().Be(-500m);
        result.CreditNote.CreditNoteNumber.Should().Be("CN-202401-000001");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = Guid.NewGuid(), Amount = 100m }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_EmptyLineItems_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var lineItems = new List<CreditNoteLineRequest>();

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least one line item is required");

        _mockInvoiceRepository.Verify(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_DraftInvoice_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        invoice.Status = InvoiceStatus.Draft;

        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.First().Id, Amount = 100m }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Credit notes can only be created for issued or paid invoices");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_VoidedInvoice_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        invoice.Status = InvoiceStatus.Voided;

        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.First().Id, Amount = 100m }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("issued or paid invoices");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_InvalidInvoiceLineId_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = Guid.NewGuid(), Amount = 100m } // Invalid line ID
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invoice line");
        result.ErrorMessage.Should().Contain("not found");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_NegativeAmount_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.First().Id, Amount = -100m }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Credit note line amount must be positive");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_AmountExceedsInvoiceLine_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithLines(invoiceId);
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.First().Id, Amount = 2000m } // More than invoice line
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockNumberGenerator
            .Setup(g => g.GenerateNextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CN-202401-000001");

        // Act
        var result = await _service.CreateCreditNoteAsync(invoiceId, CreditNoteReason.Discount, lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Credit amount");
        result.ErrorMessage.Should().Contain("exceeds invoice line amount");

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_MultipleLines_CreatesCorrectly()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoiceWithMultipleLines(invoiceId);
        var lineItems = new List<CreditNoteLineRequest>
        {
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.First().Id, Amount = 300m },
            new CreditNoteLineRequest { InvoiceLineId = invoice.Lines.Last().Id, Amount = 150m }
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockNumberGenerator
            .Setup(g => g.GenerateNextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CN-202401-000002");

        _mockCreditNoteRepository
            .Setup(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditNote cn, CancellationToken _) => cn);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateCreditNoteAsync(
            invoiceId,
            CreditNoteReason.Refund,
            lineItems);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CreditNote.Should().NotBeNull();
        result.CreditNote!.Lines.Should().HaveCount(2);
        result.CreditNote.TotalAmount.Should().Be(-450m); // Sum of negative amounts

        _mockCreditNoteRepository.Verify(r => r.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IssueCreditNoteAsync Tests

    [Fact]
    public async Task IssueCreditNoteAsync_ValidCreditNote_IssuesSuccessfully()
    {
        // Arrange
        var creditNoteId = Guid.NewGuid();
        var creditNote = CreateCreditNoteWithLines(creditNoteId);

        _mockCreditNoteRepository
            .Setup(r => r.GetByIdWithLinesAsync(creditNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditNote);

        _mockCreditNoteRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CreditNote>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.IssueCreditNoteAsync(creditNoteId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CreditNote.Should().NotBeNull();
        result.CreditNote!.AppliedAtUtc.Should().NotBeNull();
        result.CreditNote.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockCreditNoteRepository.Verify(r => r.UpdateAsync(It.IsAny<CreditNote>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_CreditNoteNotFound_ReturnsError()
    {
        // Arrange
        var creditNoteId = Guid.NewGuid();

        _mockCreditNoteRepository
            .Setup(r => r.GetByIdWithLinesAsync(creditNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditNote?)null);

        // Act
        var result = await _service.IssueCreditNoteAsync(creditNoteId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        _mockCreditNoteRepository.Verify(r => r.UpdateAsync(It.IsAny<CreditNote>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_AlreadyIssued_ReturnsError()
    {
        // Arrange
        var creditNoteId = Guid.NewGuid();
        var creditNote = CreateCreditNoteWithLines(creditNoteId);
        creditNote.AppliedAtUtc = DateTime.UtcNow.AddDays(-1);

        _mockCreditNoteRepository
            .Setup(r => r.GetByIdWithLinesAsync(creditNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditNote);

        // Act
        var result = await _service.IssueCreditNoteAsync(creditNoteId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already been issued");

        _mockCreditNoteRepository.Verify(r => r.UpdateAsync(It.IsAny<CreditNote>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_WithoutLines_ReturnsError()
    {
        // Arrange
        var creditNoteId = Guid.NewGuid();
        var creditNote = new CreditNote
        {
            Id = creditNoteId,
            OrgId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            CreditNoteNumber = "CN-001",
            Lines = new List<CreditNoteLine>(),
            RowVersion = new byte[8]
        };

        _mockCreditNoteRepository
            .Setup(r => r.GetByIdWithLinesAsync(creditNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditNote);

        // Act
        var result = await _service.IssueCreditNoteAsync(creditNoteId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("without line items");

        _mockCreditNoteRepository.Verify(r => r.UpdateAsync(It.IsAny<CreditNote>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private Invoice CreateIssuedInvoiceWithLines(Guid invoiceId)
    {
        return new Invoice
        {
            Id = invoiceId,
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            InvoiceNumber = "INV-001",
            TotalAmount = 1000m,
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    LineNumber = 1,
                    Description = "Rent for Jan 2024",
                    Amount = 1000m,
                    TaxAmount = 0m,
                    TotalAmount = 1000m
                }
            },
            RowVersion = new byte[8]
        };
    }

    private Invoice CreateIssuedInvoiceWithMultipleLines(Guid invoiceId)
    {
        return new Invoice
        {
            Id = invoiceId,
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            InvoiceNumber = "INV-002",
            TotalAmount = 1500m,
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    LineNumber = 1,
                    Description = "Rent",
                    Amount = 1000m,
                    TaxAmount = 0m,
                    TotalAmount = 1000m
                },
                new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    LineNumber = 2,
                    Description = "Maintenance",
                    Amount = 500m,
                    TaxAmount = 0m,
                    TotalAmount = 500m
                }
            },
            RowVersion = new byte[8]
        };
    }

    private CreditNote CreateCreditNoteWithLines(Guid creditNoteId)
    {
        var invoiceLineId = Guid.NewGuid();
        return new CreditNote
        {
            Id = creditNoteId,
            OrgId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            CreditNoteNumber = "CN-001",
            Reason = CreditNoteReason.Discount,
            TotalAmount = -200m,
            Lines = new List<CreditNoteLine>
            {
                new CreditNoteLine
                {
                    Id = Guid.NewGuid(),
                    CreditNoteId = creditNoteId,
                    InvoiceLineId = invoiceLineId,
                    LineNumber = 1,
                    Description = "Credit for Rent",
                    Amount = -200m,
                    TaxAmount = 0m,
                    TotalAmount = -200m
                }
            },
            RowVersion = new byte[8]
        };
    }

    #endregion
}
