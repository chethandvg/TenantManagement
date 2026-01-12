using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Billing.Commands.PaymentConfirmation;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Unit tests for ConfirmPaymentRequestCommandHandler.
/// </summary>
public class ConfirmPaymentRequestCommandHandlerTests
{
    private readonly Mock<IPaymentConfirmationRequestRepository> _mockRequestRepo;
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
    private readonly Mock<IPaymentRepository> _mockPaymentRepo;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly ConfirmPaymentRequestCommandHandler _handler;

    public ConfirmPaymentRequestCommandHandlerTests()
    {
        _mockRequestRepo = new Mock<IPaymentConfirmationRequestRepository>();
        _mockInvoiceRepo = new Mock<IInvoiceRepository>();
        _mockPaymentRepo = new Mock<IPaymentRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("owner-user");

        _handler = new ConfirmPaymentRequestCommandHandler(
            _mockRequestRepo.Object,
            _mockInvoiceRepo.Object,
            _mockPaymentRepo.Object,
            _mockDbContext.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPaymentAndUpdatesInvoice()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = requestId,
            OrgId = orgId,
            InvoiceId = invoiceId,
            LeaseId = leaseId,
            Amount = 500m,
            PaymentDateUtc = DateTime.UtcNow,
            ReceiptNumber = "REC-001",
            Status = PaymentConfirmationStatus.Pending,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var invoice = new Invoice
        {
            Id = invoiceId,
            OrgId = orgId,
            LeaseId = leaseId,
            InvoiceNumber = "INV-001",
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = requestId,
            ReviewResponse = "Payment verified and approved"
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockInvoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<PaymentConfirmationRequest>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PaymentId);

        // Verify payment was created
        _mockPaymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.InvoiceId == invoiceId &&
            p.Amount == 500m &&
            p.Status == PaymentStatus.Completed &&
            p.PaymentMode == PaymentMode.Cash
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify invoice was updated
        Assert.Equal(500m, invoice.PaidAmount);
        Assert.Equal(500m, invoice.BalanceAmount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);

        // Verify request was updated
        _mockRequestRepo.Verify(r => r.UpdateAsync(It.Is<PaymentConfirmationRequest>(req =>
            req.Status == PaymentConfirmationStatus.Confirmed &&
            req.ReviewResponse == "Payment verified and approved"
        ), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FullPayment_MarksInvoiceAsPaid()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = requestId,
            OrgId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            LeaseId = Guid.NewGuid(),
            Amount = 1000m, // Full amount
            PaymentDateUtc = DateTime.UtcNow,
            Status = PaymentConfirmationStatus.Pending,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var invoice = new Invoice
        {
            Id = invoiceId,
            OrgId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = requestId
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockInvoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<PaymentConfirmationRequest>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, invoice.BalanceAmount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAtUtc);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ReturnsError()
    {
        // Arrange
        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = Guid.NewGuid()
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentConfirmationRequest?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RequestNotPending_ReturnsError()
    {
        // Arrange
        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = Guid.NewGuid(),
            Status = PaymentConfirmationStatus.Confirmed // Already confirmed
        };

        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = confirmationRequest.Id
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not pending", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_AmountExceedsRemainingBalance_ReturnsError()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = requestId,
            InvoiceId = invoiceId,
            Amount = 600m, // Exceeds remaining balance
            Status = PaymentConfirmationStatus.Pending
        };

        var invoice = new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m
        };

        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = requestId
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        _mockInvoiceRepo.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepo.Setup(r => r.GetTotalPaidAmountAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500m); // Already paid 500

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("exceeds remaining balance", result.ErrorMessage);
    }
}
