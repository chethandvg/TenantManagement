using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Abstractions.Storage;
using TentMan.Application.Billing.Commands.PaymentConfirmation;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Unit tests for CreatePaymentConfirmationRequestCommandHandler.
/// </summary>
public class CreatePaymentConfirmationRequestCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
    private readonly Mock<IPaymentConfirmationRequestRepository> _mockRequestRepo;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IFileMetadataRepository> _mockFileMetadataRepo;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly CreatePaymentConfirmationRequestCommandHandler _handler;

    public CreatePaymentConfirmationRequestCommandHandlerTests()
    {
        _mockInvoiceRepo = new Mock<IInvoiceRepository>();
        _mockRequestRepo = new Mock<IPaymentConfirmationRequestRepository>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockFileMetadataRepo = new Mock<IFileMetadataRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("test-user");

        _handler = new CreatePaymentConfirmationRequestCommandHandler(
            _mockInvoiceRepo.Object,
            _mockRequestRepo.Object,
            _mockFileStorageService.Object,
            _mockFileMetadataRepo.Object,
            _mockDbContext.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = invoiceId,
            Amount = 500m,
            PaymentDate = DateTime.Today,
            ReceiptNumber = "REC-001",
            Notes = "Cash payment made"
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockRequestRepo.Setup(r => r.AddAsync(It.IsAny<PaymentConfirmationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentConfirmationRequest p, CancellationToken ct) => p);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RequestId);

        _mockRequestRepo.Verify(r => r.AddAsync(It.Is<PaymentConfirmationRequest>(p =>
            p.InvoiceId == invoiceId &&
            p.Amount == 500m &&
            p.Status == PaymentConfirmationStatus.Pending &&
            p.ReceiptNumber == "REC-001"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 100m,
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DraftInvoice_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Draft,
            TotalAmount = 1000m,
            BalanceAmount = 1000m
        };

        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = invoice.Id,
            Amount = 100m,
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot create payment confirmation", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_AmountExceedsBalance_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m
        };

        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = invoice.Id,
            Amount = 1500m, // More than balance
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("exceeds invoice balance", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ReturnsError()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            BalanceAmount = 1000m
        };

        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = invoice.Id,
            Amount = 0m,
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("must be greater than zero", result.ErrorMessage);
    }
}
