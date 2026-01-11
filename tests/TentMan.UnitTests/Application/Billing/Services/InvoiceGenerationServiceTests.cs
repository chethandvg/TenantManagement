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
public class InvoiceGenerationServiceTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly Mock<ILeaseBillingSettingRepository> _mockBillingSettingRepository;
    private readonly Mock<IChargeTypeRepository> _mockChargeTypeRepository;
    private readonly Mock<IRentCalculationService> _mockRentCalculationService;
    private readonly Mock<IRecurringChargeCalculationService> _mockRecurringChargeCalculationService;
    private readonly Mock<IInvoiceNumberGenerator> _mockInvoiceNumberGenerator;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly InvoiceGenerationService _service;

    public InvoiceGenerationServiceTests()
    {
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _mockLeaseRepository = new Mock<ILeaseRepository>();
        _mockBillingSettingRepository = new Mock<ILeaseBillingSettingRepository>();
        _mockChargeTypeRepository = new Mock<IChargeTypeRepository>();
        _mockRentCalculationService = new Mock<IRentCalculationService>();
        _mockRecurringChargeCalculationService = new Mock<IRecurringChargeCalculationService>();
        _mockInvoiceNumberGenerator = new Mock<IInvoiceNumberGenerator>();
        _mockDbContext = new Mock<IApplicationDbContext>();

        _service = new InvoiceGenerationService(
            _mockInvoiceRepository.Object,
            _mockLeaseRepository.Object,
            _mockBillingSettingRepository.Object,
            _mockChargeTypeRepository.Object,
            _mockRentCalculationService.Object,
            _mockRecurringChargeCalculationService.Object,
            _mockInvoiceNumberGenerator.Object,
            _mockDbContext.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GenerateInvoiceAsync_ActiveLease_CreatesNewInvoice()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateActiveLease(leaseId, orgId);
        var rentChargeType = CreateChargeType(ChargeTypeCode.RENT);
        var rentCalculation = CreateRentCalculation(10000m);
        var recurringChargeCalculation = CreateRecurringChargeCalculation();

        SetupMocks(lease, rentChargeType, rentCalculation, recurringChargeCalculation, null);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Invoice.Should().NotBeNull();
        result.Invoice!.Status.Should().Be(InvoiceStatus.Draft);
        result.Invoice.LeaseId.Should().Be(leaseId);
        result.Invoice.OrgId.Should().Be(orgId);
        result.Invoice.BillingPeriodStart.Should().Be(billingStart);
        result.Invoice.BillingPeriodEnd.Should().Be(billingEnd);
        result.WasUpdated.Should().BeFalse();

        _mockInvoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_ExistingDraftInvoice_UpdatesInvoice()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateActiveLease(leaseId, orgId);
        var existingInvoice = CreateDraftInvoice(leaseId, orgId, billingStart, billingEnd);
        var rentChargeType = CreateChargeType(ChargeTypeCode.RENT);
        var rentCalculation = CreateRentCalculation(10000m);
        var recurringChargeCalculation = CreateRecurringChargeCalculation();

        SetupMocks(lease, rentChargeType, rentCalculation, recurringChargeCalculation, existingInvoice);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.WasUpdated.Should().BeTrue();
        result.Invoice.Should().NotBeNull();

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_InactiveLease_ReturnsError()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateActiveLease(leaseId, orgId);
        lease.Status = LeaseStatus.Ended;

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public async Task GenerateInvoiceAsync_LeaseNotFound_ReturnsError()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lease?)null);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task GenerateInvoiceAsync_ExistingIssuedInvoice_ReturnsError()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateActiveLease(leaseId, orgId);
        var existingInvoice = CreateDraftInvoice(leaseId, orgId, billingStart, billingEnd);
        existingInvoice.Status = InvoiceStatus.Issued; // Invoice is already issued

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(
                leaseId, billingStart, billingEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("An invoice already exists for this period");
        result.ErrorMessage.Should().Contain("Issued");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockInvoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_ExistingVoidedInvoice_ReturnsError()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateActiveLease(leaseId, orgId);
        var existingInvoice = CreateDraftInvoice(leaseId, orgId, billingStart, billingEnd);
        existingInvoice.Status = InvoiceStatus.Voided; // Invoice is voided

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(
                leaseId, billingStart, billingEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        // Act
        var result = await _service.GenerateInvoiceAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("An invoice already exists for this period");
        result.ErrorMessage.Should().Contain("Voided");

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockInvoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private Lease CreateActiveLease(Guid leaseId, Guid orgId)
    {
        return new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = new DateOnly(2024, 1, 1),
            Terms = new List<LeaseTerm>()
        };
    }

    private Invoice CreateDraftInvoice(Guid leaseId, Guid orgId, DateOnly billingStart, DateOnly billingEnd)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.Draft,
            BillingPeriodStart = billingStart,
            BillingPeriodEnd = billingEnd,
            Lines = new List<InvoiceLine>(),
            RowVersion = new byte[8]
        };
    }

    private ChargeType CreateChargeType(ChargeTypeCode code)
    {
        return new ChargeType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code.ToString(),
            IsSystemDefined = true,
            IsActive = true
        };
    }

    private RentCalculationResult CreateRentCalculation(decimal amount)
    {
        return new RentCalculationResult
        {
            TotalAmount = amount,
            LineItems = new List<RentLineItem>
            {
                new RentLineItem
                {
                    PeriodStart = new DateOnly(2024, 1, 1),
                    PeriodEnd = new DateOnly(2024, 1, 31),
                    Amount = amount,
                    Description = "Rent for Jan 2024",
                    IsProrated = false
                }
            }
        };
    }

    private RecurringChargeCalculationResult CreateRecurringChargeCalculation()
    {
        return new RecurringChargeCalculationResult
        {
            TotalAmount = 0,
            LineItems = new List<RecurringChargeLineItem>()
        };
    }

    private void SetupMocks(
        Lease lease,
        ChargeType rentChargeType,
        RentCalculationResult rentCalculation,
        RecurringChargeCalculationResult recurringChargeCalculation,
        Invoice? existingInvoice)
    {
        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(lease.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        _mockBillingSettingRepository
            .Setup(r => r.GetByLeaseIdAsync(lease.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaseBillingSetting?)null);

        _mockChargeTypeRepository
            .Setup(r => r.GetByCodeAsync(ChargeTypeCode.RENT, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentChargeType);

        var maintChargeType = CreateChargeType(ChargeTypeCode.MAINT);
        _mockChargeTypeRepository
            .Setup(r => r.GetByCodeAsync(ChargeTypeCode.MAINT, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(maintChargeType);

        _mockRentCalculationService
            .Setup(s => s.CalculateRentAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentCalculation);

        _mockRecurringChargeCalculationService
            .Setup(s => s.CalculateChargesAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurringChargeCalculation);

        _mockInvoiceNumberGenerator
            .Setup(g => g.GenerateNextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-202401-000001");

        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice inv, CancellationToken _) => inv);

        _mockInvoiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
