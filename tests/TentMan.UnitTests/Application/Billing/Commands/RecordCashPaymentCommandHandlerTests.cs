using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Billing.Commands.RecordPayment;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Commands;

/// <summary>
/// Unit tests for RecordCashPaymentCommandHandler.
/// </summary>
public class RecordCashPaymentCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
    private readonly Mock<IPaymentRepository> _mockPaymentRepo;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly RecordCashPaymentCommandHandler _handler;

    public RecordCashPaymentCommandHandlerTests()
    {
        _mockInvoiceRepo = new Mock<IInvoiceRepository>();
        _mockPaymentRepo = new Mock<IPaymentRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("test-user");

        _handler = new RecordCashPaymentCommandHandler(
            _mockInvoiceRepo.Object,
            _mockPaymentRepo.Object,
            _mockDbContext.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ValidCashPayment_ReturnsSuccess()
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

        var command = new RecordCashPaymentCommand
        {
            InvoiceId = invoiceId,
            Amount = 500m,
            PaymentDate = DateTime.Today,
            PayerName = "John Doe",
            ReceiptNumber = "REC-001"
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockInvoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PaymentId);
        Assert.Equal(500m, result.InvoiceBalanceAmount);

        // Verify invoice was updated with correct amounts
        Assert.Equal(500m, invoice.PaidAmount);
        Assert.Equal(500m, invoice.BalanceAmount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);

        _mockPaymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.PaymentMode == PaymentMode.Cash &&
            p.Status == PaymentStatus.Completed &&
            p.Amount == 500m
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsError()
    {
        // Arrange
        var command = new RecordCashPaymentCommand
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
            TotalAmount = 1000m
        };

        var command = new RecordCashPaymentCommand
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
        Assert.Contains("Cannot record payment", result.ErrorMessage);
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

        var command = new RecordCashPaymentCommand
        {
            InvoiceId = invoice.Id,
            Amount = 1500m, // More than total
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("exceeds remaining balance", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_FullPayment_MarksInvoiceAsPaid()
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
            BalanceAmount = 1000m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var command = new RecordCashPaymentCommand
        {
            InvoiceId = invoice.Id,
            Amount = 1000m, // Full amount
            PaymentDate = DateTime.Today
        };

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockInvoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.InvoiceBalanceAmount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAtUtc);
    }
}
