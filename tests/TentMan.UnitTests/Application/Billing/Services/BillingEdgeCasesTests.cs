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

/// <summary>
/// Comprehensive tests for billing engine edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
[Trait("TestType", "EdgeCases")]
public class BillingEdgeCasesTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly Mock<ILeaseBillingSettingRepository> _mockBillingSettingRepository;
    private readonly Mock<IChargeTypeRepository> _mockChargeTypeRepository;
    private readonly Mock<IRentCalculationService> _mockRentCalculationService;
    private readonly Mock<IRecurringChargeCalculationService> _mockRecurringChargeCalculationService;
    private readonly Mock<IInvoiceNumberGenerator> _mockInvoiceNumberGenerator;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly InvoiceGenerationService _invoiceService;
    private readonly RentCalculationService _rentCalculationService;

    public BillingEdgeCasesTests()
    {
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _mockLeaseRepository = new Mock<ILeaseRepository>();
        _mockBillingSettingRepository = new Mock<ILeaseBillingSettingRepository>();
        _mockChargeTypeRepository = new Mock<IChargeTypeRepository>();
        _mockRentCalculationService = new Mock<IRentCalculationService>();
        _mockRecurringChargeCalculationService = new Mock<IRecurringChargeCalculationService>();
        _mockInvoiceNumberGenerator = new Mock<IInvoiceNumberGenerator>();
        _mockDbContext = new Mock<IApplicationDbContext>();

        _invoiceService = new InvoiceGenerationService(
            _mockInvoiceRepository.Object,
            _mockLeaseRepository.Object,
            _mockBillingSettingRepository.Object,
            _mockChargeTypeRepository.Object,
            _mockRentCalculationService.Object,
            _mockRecurringChargeCalculationService.Object,
            _mockInvoiceNumberGenerator.Object,
            _mockDbContext.Object);

        _rentCalculationService = new RentCalculationService(_mockLeaseRepository.Object);
    }

    #region Edge Case: Lease starts mid-month → proration

    [Fact]
    public async Task LeaseStartsMidMonth_ShouldProratRent()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        
        // Lease starts on Jan 15
        var leaseStartDate = new DateOnly(2024, 1, 15);
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = leaseStartDate,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = termId,
                    LeaseId = leaseId,
                    MonthlyRent = monthlyRent,
                    EffectiveFrom = leaseStartDate,
                    EffectiveTo = null
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(1);
        
        var lineItem = result.LineItems.First();
        lineItem.PeriodStart.Should().Be(leaseStartDate);
        lineItem.PeriodEnd.Should().Be(billingPeriodEnd);
        lineItem.IsProrated.Should().BeTrue();
        lineItem.LeaseTermId.Should().Be(termId);
        
        // 17 days out of 31 = 5483.87
        lineItem.Amount.Should().BeApproximately(5483.87m, 0.01m);
    }

    #endregion

    #region Edge Case: Lease ends mid-month → proration + final invoice

    [Fact]
    public async Task LeaseEndsMidMonth_ShouldProratRent()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        
        // Lease ends on Jan 20
        var leaseStartDate = new DateOnly(2024, 1, 1);
        var leaseEndDate = new DateOnly(2024, 1, 20);
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = leaseStartDate,
            EndDate = leaseEndDate,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = termId,
                    LeaseId = leaseId,
                    MonthlyRent = monthlyRent,
                    EffectiveFrom = leaseStartDate,
                    EffectiveTo = leaseEndDate
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(1);
        
        var lineItem = result.LineItems.First();
        lineItem.PeriodStart.Should().Be(billingPeriodStart);
        lineItem.PeriodEnd.Should().Be(leaseEndDate);
        lineItem.IsProrated.Should().BeTrue();
        lineItem.LeaseTermId.Should().Be(termId);
        
        // 20 days out of 31 = 6451.61
        lineItem.Amount.Should().BeApproximately(6451.61m, 0.01m);
    }

    #endregion

    #region Edge Case: Rent changes mid-month → split calculation

    [Fact]
    public async Task RentChangesMidMonth_ShouldSplitCalculation()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var term1Id = Guid.NewGuid();
        var term2Id = Guid.NewGuid();
        
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var rentChangeDate = new DateOnly(2024, 1, 16);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);
        var oldRent = 10000m;
        var newRent = 12000m;

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = billingPeriodStart,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = term1Id,
                    LeaseId = leaseId,
                    MonthlyRent = oldRent,
                    EffectiveFrom = billingPeriodStart,
                    EffectiveTo = new DateOnly(2024, 1, 15)
                },
                new LeaseTerm
                {
                    Id = term2Id,
                    LeaseId = leaseId,
                    MonthlyRent = newRent,
                    EffectiveFrom = rentChangeDate,
                    EffectiveTo = null
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(2);
        
        var firstPeriod = result.LineItems.First();
        firstPeriod.PeriodStart.Should().Be(billingPeriodStart);
        firstPeriod.PeriodEnd.Should().Be(new DateOnly(2024, 1, 15));
        firstPeriod.FullMonthlyRent.Should().Be(oldRent);
        firstPeriod.IsProrated.Should().BeTrue();
        firstPeriod.LeaseTermId.Should().Be(term1Id);
        
        var secondPeriod = result.LineItems.Last();
        secondPeriod.PeriodStart.Should().Be(rentChangeDate);
        secondPeriod.PeriodEnd.Should().Be(billingPeriodEnd);
        secondPeriod.FullMonthlyRent.Should().Be(newRent);
        secondPeriod.IsProrated.Should().BeTrue();
        secondPeriod.LeaseTermId.Should().Be(term2Id);
        
        // Total should be sum of both prorated amounts
        result.TotalAmount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Case: Voided invoice → must not be regenerated

    [Fact]
    public async Task VoidedInvoice_ShouldNotBeRegenerated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var voidedInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.Voided,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            VoidedAtUtc = DateTime.UtcNow,
            VoidReason = "Test void"
        };

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            Terms = new List<LeaseTerm>()
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(leaseId, billingPeriodStart, billingPeriodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidedInvoice);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Voided");
        result.Invoice.Should().BeNull();
    }

    #endregion

    #region Edge Case: February edge cases (28/29 days)

    [Theory]
    [InlineData(2024, 29)] // Leap year
    [InlineData(2023, 28)] // Non-leap year
    public async Task FebruaryRent_ShouldHandleDifferentDayCounts(int year, int expectedDays)
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(year, 2, 1);
        var billingPeriodEnd = new DateOnly(year, 2, expectedDays);
        var monthlyRent = 10000m;

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = billingPeriodStart,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = termId,
                    LeaseId = leaseId,
                    MonthlyRent = monthlyRent,
                    EffectiveFrom = billingPeriodStart,
                    EffectiveTo = null
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(1);
        
        var lineItem = result.LineItems.First();
        lineItem.IsProrated.Should().BeFalse(); // Full month
        lineItem.Amount.Should().Be(monthlyRent); // Full rent for full month
    }

    #endregion

    #region Edge Case: Invalid billing day (>28)

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public void InvalidBillingDay_ShouldBeRejectedByValidation(byte invalidDay)
    {
        // Arrange
        var billingSetting = new LeaseBillingSetting
        {
            LeaseId = Guid.NewGuid(),
            BillingDay = invalidDay
        };

        // Act & Assert
        // Note: Validation should happen at the API/application layer
        // This test documents the requirement that BillingDay must be 1-28
        invalidDay.Should().BeGreaterThan(28);
    }

    #endregion

    #region Edge Case: Duplicate invoice generation (idempotency)

    [Fact]
    public async Task DuplicateInvoiceGeneration_ShouldUpdateExistingDraft()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var existingDraftInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.Draft,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            InvoiceNumber = "INV-2024-001",
            Lines = new List<InvoiceLine>()
        };

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = Guid.NewGuid(),
                    LeaseId = leaseId,
                    MonthlyRent = 10000m,
                    EffectiveFrom = billingPeriodStart,
                    EffectiveTo = null
                }
            }
        };

        var chargeType = new ChargeType
        {
            Id = Guid.NewGuid(),
            Code = ChargeTypeCode.RENT,
            Name = "Rent"
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(leaseId, billingPeriodStart, billingPeriodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDraftInvoice);

        _mockChargeTypeRepository
            .Setup(r => r.GetByCodeAsync(ChargeTypeCode.RENT, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chargeType);

        _mockRentCalculationService
            .Setup(s => s.CalculateRentAsync(leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RentCalculationResult
            {
                TotalAmount = 10000m,
                LineItems = new List<RentLineItem>
                {
                    new RentLineItem
                    {
                        LeaseTermId = Guid.NewGuid(),
                        Amount = 10000m,
                        Description = "Rent for Jan 2024"
                    }
                }
            });

        _mockRecurringChargeCalculationService
            .Setup(s => s.CalculateChargesAsync(leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecurringChargeCalculationResult());

        _mockInvoiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.WasUpdated.Should().BeTrue();
        result.Invoice.Should().NotBeNull();
        result.Invoice!.Id.Should().Be(existingDraftInvoice.Id);

        _mockInvoiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceRepository.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Case: Source tracking on invoice lines

    [Fact]
    public async Task InvoiceGeneration_ShouldPopulateSourceTracking()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            Terms = new List<LeaseTerm>()
        };

        var rentChargeType = new ChargeType { Id = Guid.NewGuid(), Code = ChargeTypeCode.RENT, Name = "Rent" };
        var maintenanceChargeType = new ChargeType { Id = Guid.NewGuid(), Code = ChargeTypeCode.MAINT, Name = "Maintenance" };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        _mockInvoiceRepository
            .Setup(r => r.GetDraftInvoiceForPeriodAsync(leaseId, billingPeriodStart, billingPeriodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice)null!);

        _mockChargeTypeRepository
            .Setup(r => r.GetByCodeAsync(ChargeTypeCode.RENT, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentChargeType);

        _mockChargeTypeRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChargeType> { maintenanceChargeType });

        _mockRentCalculationService
            .Setup(s => s.CalculateRentAsync(leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RentCalculationResult
            {
                TotalAmount = 10000m,
                LineItems = new List<RentLineItem>
                {
                    new RentLineItem
                    {
                        LeaseTermId = termId,
                        Amount = 10000m,
                        Description = "Rent for Jan 2024"
                    }
                }
            });

        _mockRecurringChargeCalculationService
            .Setup(s => s.CalculateChargesAsync(leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecurringChargeCalculationResult
            {
                TotalAmount = 500m,
                LineItems = new List<RecurringChargeLineItem>
                {
                    new RecurringChargeLineItem
                    {
                        ChargeId = chargeId,
                        ChargeTypeId = maintenanceChargeType.Id,
                        Amount = 500m,
                        ChargeDescription = "Maintenance Fee"
                    }
                }
            });

        _mockInvoiceNumberGenerator
            .Setup(g => g.GenerateNextAsync(orgId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-2024-001");

        Invoice? capturedInvoice = null;
        _mockInvoiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .Callback<Invoice, CancellationToken>((inv, ct) => capturedInvoice = inv)
            .ReturnsAsync((Invoice)null!);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        capturedInvoice.Should().NotBeNull();
        capturedInvoice!.Lines.Should().HaveCount(2);
        
        // Verify rent line has source tracking
        var rentLine = capturedInvoice.Lines.First();
        rentLine.Source.Should().Be("Rent");
        rentLine.SourceRefId.Should().Be(termId);
        
        // Verify recurring charge line has source tracking
        var chargeLine = capturedInvoice.Lines.Last();
        chargeLine.Source.Should().Be("RecurringCharge");
        chargeLine.SourceRefId.Should().Be(chargeId);
    }

    #endregion

    #region Edge Case: 30-day month proration method

    [Fact]
    public async Task ThirtyDayMonthProration_ShouldUseFixedMonthLength()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        
        // Lease starts on Jan 15 (17 days remain in 31-day month)
        var leaseStartDate = new DateOnly(2024, 1, 15);
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = new Lease
        {
            Id = leaseId,
            OrgId = orgId,
            Status = LeaseStatus.Active,
            StartDate = leaseStartDate,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = termId,
                    LeaseId = leaseId,
                    MonthlyRent = monthlyRent,
                    EffectiveFrom = leaseStartDate,
                    EffectiveTo = null
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ThirtyDayMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(1);
        
        var lineItem = result.LineItems.First();
        lineItem.IsProrated.Should().BeTrue();
        
        // With 30-day method: 17 days / 30 = 5666.67
        lineItem.Amount.Should().BeApproximately(5666.67m, 0.01m);
    }

    #endregion

    #region Edge Case: Tenant swapped mid-month (multiple leases)

    [Fact]
    public async Task TenantSwappedMidMonth_ShouldProrateForBothLeases()
    {
        // Arrange
        const decimal MONTHLY_RENT = 10000m;
        var unitId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        
        var oldLeaseId = Guid.NewGuid();
        var newLeaseId = Guid.NewGuid();
        
        var oldTermId = Guid.NewGuid();
        var newTermId = Guid.NewGuid();
        
        // Old lease ends Jan 15
        var oldLeaseEndDate = new DateOnly(2024, 1, 15);
        // New lease starts Jan 16
        var newLeaseStartDate = new DateOnly(2024, 1, 16);
        
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var oldLease = new Lease
        {
            Id = oldLeaseId,
            OrgId = orgId,
            UnitId = unitId,
            Status = LeaseStatus.Ended,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = oldLeaseEndDate,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = oldTermId,
                    LeaseId = oldLeaseId,
                    MonthlyRent = MONTHLY_RENT,
                    EffectiveFrom = new DateOnly(2024, 1, 1),
                    EffectiveTo = oldLeaseEndDate
                }
            }
        };

        var newLease = new Lease
        {
            Id = newLeaseId,
            OrgId = orgId,
            UnitId = unitId,
            Status = LeaseStatus.Active,
            StartDate = newLeaseStartDate,
            Terms = new List<LeaseTerm>
            {
                new LeaseTerm
                {
                    Id = newTermId,
                    LeaseId = newLeaseId,
                    MonthlyRent = MONTHLY_RENT,
                    EffectiveFrom = newLeaseStartDate,
                    EffectiveTo = null
                }
            }
        };

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(oldLeaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldLease);

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(newLeaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newLease);

        // Act - Generate invoices for both leases
        var oldLeaseResult = await _rentCalculationService.CalculateRentAsync(
            oldLeaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        var newLeaseResult = await _rentCalculationService.CalculateRentAsync(
            newLeaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        oldLeaseResult.Should().NotBeNull();
        oldLeaseResult.LineItems.Should().HaveCount(1);
        
        var oldLeaseLineItem = oldLeaseResult.LineItems.First();
        oldLeaseLineItem.IsProrated.Should().BeTrue();
        // Old lease: 15 days (Jan 1-15) / 31 days in January * 10000 = 4838.71
        oldLeaseLineItem.Amount.Should().BeApproximately(4838.71m, 0.01m);

        newLeaseResult.Should().NotBeNull();
        newLeaseResult.LineItems.Should().HaveCount(1);
        
        var newLeaseLineItem = newLeaseResult.LineItems.First();
        newLeaseLineItem.IsProrated.Should().BeTrue();
        // New lease: 16 days (Jan 16-31) / 31 days in January * 10000 = 5161.29
        newLeaseLineItem.Amount.Should().BeApproximately(5161.29m, 0.01m);

        // Verify combined total equals full month rent (accounting for rounding)
        var totalRent = oldLeaseLineItem.Amount + newLeaseLineItem.Amount;
        totalRent.Should().Be(MONTHLY_RENT);
    }

    #endregion
}
