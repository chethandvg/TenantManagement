using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Billing.Commands.PaymentConfirmation;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Unit tests for RejectPaymentRequestCommandHandler.
/// </summary>
public class RejectPaymentRequestCommandHandlerTests
{
    private readonly Mock<IPaymentConfirmationRequestRepository> _mockRequestRepo;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly RejectPaymentRequestCommandHandler _handler;

    public RejectPaymentRequestCommandHandlerTests()
    {
        _mockRequestRepo = new Mock<IPaymentConfirmationRequestRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _mockCurrentUser.Setup(u => u.UserId).Returns("owner-user");

        _handler = new RejectPaymentRequestCommandHandler(
            _mockRequestRepo.Object,
            _mockDbContext.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_RejectsSuccessfully()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = requestId,
            OrgId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            LeaseId = Guid.NewGuid(),
            Amount = 500m,
            Status = PaymentConfirmationStatus.Pending,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var command = new RejectPaymentRequestCommand
        {
            RequestId = requestId,
            ReviewResponse = "Receipt does not match our records"
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        _mockRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<PaymentConfirmationRequest>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockRequestRepo.Verify(r => r.UpdateAsync(It.Is<PaymentConfirmationRequest>(req =>
            req.Status == PaymentConfirmationStatus.Rejected &&
            req.ReviewResponse == "Receipt does not match our records" &&
            req.ReviewedBy == "owner-user" &&
            req.ReviewedAtUtc != null
        ), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ReturnsError()
    {
        // Arrange
        var command = new RejectPaymentRequestCommand
        {
            RequestId = Guid.NewGuid(),
            ReviewResponse = "Test rejection"
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

        var command = new RejectPaymentRequestCommand
        {
            RequestId = confirmationRequest.Id,
            ReviewResponse = "Test rejection"
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
    public async Task Handle_EmptyReviewResponse_ReturnsError()
    {
        // Arrange
        var confirmationRequest = new PaymentConfirmationRequest
        {
            Id = Guid.NewGuid(),
            Status = PaymentConfirmationStatus.Pending
        };

        var command = new RejectPaymentRequestCommand
        {
            RequestId = confirmationRequest.Id,
            ReviewResponse = "" // Empty response
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmationRequest);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Review response is required", result.ErrorMessage);
    }
}
