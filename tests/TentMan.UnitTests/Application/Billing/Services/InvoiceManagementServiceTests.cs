using TentMan.Application.Abstractions;
using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class InvoiceManagementServiceTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly InvoiceManagementService _service;

    public InvoiceManagementServiceTests()
    {
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("test-user-id");
        
        _service = new InvoiceManagementService(
            _mockInvoiceRepository.Object, 
            _mockDbContext.Object,
            _mockCurrentUser.Object);
    }

    #region IssueInvoiceAsync Tests

    [Fact]
    public async Task IssueInvoiceAsync_DraftInvoiceWithLines_IssuesSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateDraftInvoiceWithLines(invoiceId);

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockInvoiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Invoice.Should().NotBeNull();
        result.Invoice!.Status.Should().Be(InvoiceStatus.Issued);
        result.Invoice.IssuedAtUtc.Should().NotBeNull();
        result.Invoice.IssuedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueInvoiceAsync_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueInvoiceAsync_InvoiceAlreadyIssued_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateDraftInvoiceWithLines(invoiceId);
        invoice.Status = InvoiceStatus.Issued;

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Only Draft invoices can be issued");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueInvoiceAsync_InvoiceWithoutLines_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Draft,
            Lines = new List<InvoiceLine>(),
            RowVersion = new byte[8]
        };

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("without line items");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueInvoiceAsync_InvoiceWithZeroTotal_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateDraftInvoiceWithLines(invoiceId);
        invoice.TotalAmount = 0;

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zero or negative total amount");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueInvoiceAsync_VoidedInvoice_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateDraftInvoiceWithLines(invoiceId);
        invoice.Status = InvoiceStatus.Voided;

        _mockInvoiceRepository
            .Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.IssueInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Only Draft invoices can be issued");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region VoidInvoiceAsync Tests

    [Fact]
    public async Task VoidInvoiceAsync_IssuedInvoice_VoidsSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoice(invoiceId);
        var reason = "Customer requested cancellation";

        _mockInvoiceRepository
            .Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockInvoiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, reason);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Invoice.Should().NotBeNull();
        result.Invoice!.Status.Should().Be(InvoiceStatus.Voided);
        result.Invoice.VoidedAtUtc.Should().NotBeNull();
        result.Invoice.VoidedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Invoice.VoidReason.Should().Be(reason);

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidInvoiceAsync_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var reason = "Test reason";

        _mockInvoiceRepository
            .Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, reason);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidInvoiceAsync_EmptyReason_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, "");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Void reason is required");

        _mockInvoiceRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidInvoiceAsync_AlreadyVoidedInvoice_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoice(invoiceId);
        invoice.Status = InvoiceStatus.Voided;
        invoice.VoidedAtUtc = DateTime.UtcNow;
        invoice.VoidReason = "Previously voided";

        _mockInvoiceRepository
            .Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, "New reason");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already voided");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidInvoiceAsync_DraftInvoice_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateDraftInvoiceWithLines(invoiceId);

        _mockInvoiceRepository
            .Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, "Test reason");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Draft invoices should be deleted, not voided");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidInvoiceAsync_InvoiceWithPayments_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateIssuedInvoice(invoiceId);
        invoice.PaidAmount = 500m;
        invoice.Status = InvoiceStatus.PartiallyPaid;

        _mockInvoiceRepository
            .Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _service.VoidInvoiceAsync(invoiceId, "Test reason");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot void an invoice with payments");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private Invoice CreateDraftInvoiceWithLines(Guid invoiceId)
    {
        var invoice = new Invoice
        {
            Id = invoiceId,
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Status = InvoiceStatus.Draft,
            InvoiceNumber = "INV-001",
            TotalAmount = 1000m,
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    LineNumber = 1,
                    Description = "Rent",
                    Amount = 1000m,
                    TotalAmount = 1000m
                }
            },
            RowVersion = new byte[8]
        };
        return invoice;
    }

    private Invoice CreateIssuedInvoice(Guid invoiceId)
    {
        var invoice = CreateDraftInvoiceWithLines(invoiceId);
        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAtUtc = DateTime.UtcNow.AddDays(-1);
        return invoice;
    }

    #endregion
}
