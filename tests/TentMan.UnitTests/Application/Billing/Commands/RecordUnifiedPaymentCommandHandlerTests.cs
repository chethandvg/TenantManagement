using Moq;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Billing.Commands.RecordPayment;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Commands;

/// <summary>
/// Unit tests for RecordPaymentCommandHandler (unified endpoint).
/// </summary>
public class RecordUnifiedPaymentCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
    private readonly Mock<IPaymentRepository> _mockPaymentRepo;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<RecordPaymentCommandHandler>> _mockLogger;
    private readonly RecordPaymentCommandHandler _handler;

    public RecordUnifiedPaymentCommandHandlerTests()
    {
        _mockInvoiceRepo = new Mock<IInvoiceRepository>();
        _mockPaymentRepo = new Mock<IPaymentRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<RecordPaymentCommandHandler>>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("test-user");

        _handler = new RecordPaymentCommandHandler(
            _mockInvoiceRepo.Object,
            _mockPaymentRepo.Object,
            _mockDbContext.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_CashPayment_MarkedAsCompleted()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId);

        var command = new RecordUnifiedPaymentCommand
        {
            InvoiceId = invoiceId,
            PaymentMode = PaymentMode.Cash,
            PaymentType = PaymentType.Rent,
            Amount = 500m,
            PaymentDate = DateTime.Today
        };

        SetupMocks(invoice);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PaymentId);
        
        _mockPaymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.PaymentMode == PaymentMode.Cash &&
            p.Status == PaymentStatus.Completed &&
            p.Amount == 500m
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnlinePaymentWithGatewayId_MarkedAsPending()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId);

        var command = new RecordUnifiedPaymentCommand
        {
            InvoiceId = invoiceId,
            PaymentMode = PaymentMode.Online,
            PaymentType = PaymentType.Rent,
            Amount = 500m,
            PaymentDate = DateTime.Today,
            TransactionReference = "TXN-123",  // Required for non-cash
            GatewayTransactionId = "txn_123",
            GatewayName = "Razorpay"
        };

        // For pending payments, don't expect invoice update
        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        _mockPaymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.PaymentMode == PaymentMode.Online &&
            p.Status == PaymentStatus.Pending &&
            p.GatewayTransactionId == "txn_123"
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Invoice should NOT be updated for pending payments
        _mockInvoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonCashWithoutTransactionRef_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId);

        var command = new RecordUnifiedPaymentCommand
        {
            InvoiceId = invoiceId,
            PaymentMode = PaymentMode.UPI,
            Amount = 500m,
            PaymentDate = DateTime.Today
            // Missing TransactionReference
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Transaction reference is required", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new RecordUnifiedPaymentCommand
        {
            InvoiceId = invoiceId,
            PaymentMode = PaymentMode.Cash,
            Amount = 500m,
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    private Invoice CreateTestInvoice(Guid invoiceId)
    {
        return new Invoice
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
    }

    private void SetupMocks(Invoice invoice)
    {
        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockInvoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
