using TentMan.Api.Controllers.Billing;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Api.Controllers.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
[Trait("Component", "Controller")]
public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly Mock<IInvoiceGenerationService> _mockInvoiceGenerationService;
    private readonly Mock<IInvoiceManagementService> _mockInvoiceManagementService;
    private readonly Mock<IChargeTypeRepository> _mockChargeTypeRepository;
    private readonly Mock<ILogger<InvoicesController>> _mockLogger;
    private readonly InvoicesController _controller;

    public InvoicesControllerTests()
    {
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _mockLeaseRepository = new Mock<ILeaseRepository>();
        _mockInvoiceGenerationService = new Mock<IInvoiceGenerationService>();
        _mockInvoiceManagementService = new Mock<IInvoiceManagementService>();
        _mockChargeTypeRepository = new Mock<IChargeTypeRepository>();
        _mockLogger = new Mock<ILogger<InvoicesController>>();

        _controller = new InvoicesController(
            _mockInvoiceRepository.Object,
            _mockLeaseRepository.Object,
            _mockInvoiceGenerationService.Object,
            _mockInvoiceManagementService.Object,
            _mockChargeTypeRepository.Object,
            _mockLogger.Object);
    }

    #region GenerateInvoice Tests

    [Fact]
    public async Task GenerateInvoice_WithValidLease_ReturnsOk()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var chargeTypeId = Guid.NewGuid();
        var invoice = CreateTestInvoice(leaseId, orgId, chargeTypeId);

        _mockLeaseRepository.Setup(r => r.ExistsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockInvoiceGenerationService.Setup(s => s.GenerateInvoiceAsync(
                leaseId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceGenerationResult
            {
                IsSuccess = true,
                Invoice = invoice,
                WasUpdated = false,
                ErrorMessage = null
            });

        _mockChargeTypeRepository.Setup(r => r.GetByIdAsync(chargeTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeType { Id = chargeTypeId, Name = "Rent" });

        // Act
        var result = await _controller.GenerateInvoice(leaseId, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GenerateInvoice_WithNonExistentLease_ReturnsNotFound()
    {
        // Arrange
        var leaseId = Guid.NewGuid();

        _mockLeaseRepository.Setup(r => r.ExistsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GenerateInvoice(leaseId, null, null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GenerateInvoice_WithFailedGeneration_ReturnsBadRequest()
    {
        // Arrange
        var leaseId = Guid.NewGuid();

        _mockLeaseRepository.Setup(r => r.ExistsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockInvoiceGenerationService.Setup(s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceGenerationResult
            {
                IsSuccess = false,
                Invoice = null,
                WasUpdated = false,
                ErrorMessage = "Generation failed"
            });

        // Act
        var result = await _controller.GenerateInvoice(leaseId, null, null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetInvoices Tests

    [Fact]
    public async Task GetInvoices_WithOrgId_ReturnsInvoices()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var chargeTypeId = Guid.NewGuid();
        var invoices = new List<Invoice>
        {
            CreateTestInvoice(Guid.NewGuid(), orgId, chargeTypeId),
            CreateTestInvoice(Guid.NewGuid(), orgId, chargeTypeId)
        };

        _mockInvoiceRepository.Setup(r => r.GetByOrgIdAsync(orgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);

        _mockChargeTypeRepository.Setup(r => r.GetByIdAsync(chargeTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeType { Id = chargeTypeId, Name = "Rent" });

        // Act
        var result = await _controller.GetInvoices(orgId, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetInvoices_WithoutOrgId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetInvoices(null, null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetInvoices_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var status = InvoiceStatus.Issued;

        _mockInvoiceRepository.Setup(r => r.GetByOrgIdAsync(orgId, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>());

        // Act
        await _controller.GetInvoices(orgId, status, CancellationToken.None);

        // Assert
        _mockInvoiceRepository.Verify(
            r => r.GetByOrgIdAsync(orgId, status, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetInvoice Tests

    [Fact]
    public async Task GetInvoice_WithValidId_ReturnsInvoice()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var chargeTypeId = Guid.NewGuid();
        var invoice = CreateTestInvoice(Guid.NewGuid(), orgId, chargeTypeId);
        invoice.Id = invoiceId;

        _mockInvoiceRepository.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockChargeTypeRepository.Setup(r => r.GetByIdAsync(chargeTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeType { Id = chargeTypeId, Name = "Rent" });

        // Act
        var result = await _controller.GetInvoice(invoiceId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetInvoice_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdWithLinesAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        // Act
        var result = await _controller.GetInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region IssueInvoice Tests

    [Fact]
    public async Task IssueInvoice_WithValidDraftInvoice_ReturnsOk()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var chargeTypeId = Guid.NewGuid();
        var invoice = CreateTestInvoice(Guid.NewGuid(), orgId, chargeTypeId);
        invoice.Status = InvoiceStatus.Issued;

        _mockInvoiceManagementService.Setup(s => s.IssueInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceIssueResult
            {
                IsSuccess = true,
                Invoice = invoice,
                ErrorMessage = null
            });

        _mockChargeTypeRepository.Setup(r => r.GetByIdAsync(chargeTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeType { Id = chargeTypeId, Name = "Rent" });

        // Act
        var result = await _controller.IssueInvoice(invoiceId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task IssueInvoice_WithFailedIssue_ReturnsBadRequest()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceManagementService.Setup(s => s.IssueInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceIssueResult
            {
                IsSuccess = false,
                Invoice = null,
                ErrorMessage = "Cannot issue invoice that is not in Draft status"
            });

        // Act
        var result = await _controller.IssueInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region VoidInvoice Tests

    [Fact]
    public async Task VoidInvoice_WithValidInvoice_ReturnsOk()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var chargeTypeId = Guid.NewGuid();
        var invoice = CreateTestInvoice(Guid.NewGuid(), orgId, chargeTypeId);
        invoice.Status = InvoiceStatus.Voided;
        
        var request = new VoidInvoiceRequest { VoidReason = "Test void" };

        _mockInvoiceManagementService.Setup(s => s.VoidInvoiceAsync(
                invoiceId,
                request.VoidReason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVoidResult
            {
                IsSuccess = true,
                Invoice = invoice,
                ErrorMessage = null
            });

        _mockChargeTypeRepository.Setup(r => r.GetByIdAsync(chargeTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeType { Id = chargeTypeId, Name = "Rent" });

        // Act
        var result = await _controller.VoidInvoice(invoiceId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task VoidInvoice_WithFailedVoid_ReturnsBadRequest()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var request = new VoidInvoiceRequest { VoidReason = "Test void" };

        _mockInvoiceManagementService.Setup(s => s.VoidInvoiceAsync(
                invoiceId,
                request.VoidReason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVoidResult
            {
                IsSuccess = false,
                Invoice = null,
                ErrorMessage = "Cannot void paid invoice"
            });

        // Act
        var result = await _controller.VoidInvoice(invoiceId, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private Invoice CreateTestInvoice(Guid leaseId, Guid orgId, Guid chargeTypeId)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            LeaseId = leaseId,
            InvoiceNumber = "INV-202401-000001",
            InvoiceDate = new DateOnly(2024, 1, 1),
            DueDate = new DateOnly(2024, 1, 15),
            Status = InvoiceStatus.Draft,
            BillingPeriodStart = new DateOnly(2024, 1, 1),
            BillingPeriodEnd = new DateOnly(2024, 1, 31),
            SubTotal = 1000m,
            TaxAmount = 0m,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            BalanceAmount = 1000m,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ChargeTypeId = chargeTypeId,
                    LineNumber = 1,
                    Description = "Rent - January 2024",
                    Quantity = 1,
                    UnitPrice = 1000m,
                    Amount = 1000m,
                    TaxRate = 0m,
                    TaxAmount = 0m,
                    TotalAmount = 1000m
                }
            },
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        return invoice;
    }

    #endregion
}
