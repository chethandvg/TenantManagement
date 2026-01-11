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
/// Tests for invoice immutability and credit note workflow.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
[Trait("TestType", "Immutability")]
public class InvoiceImmutabilityTests
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

    public InvoiceImmutabilityTests()
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
    }

    #region Invoice Immutability Tests

    [Fact]
    public async Task IssuedInvoice_ShouldNotBeRegenerated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var issuedInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.Issued,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-5),
            InvoiceNumber = "INV-2024-001"
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
            .ReturnsAsync(issuedInvoice);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
        result.ErrorMessage.Should().Contain("Issued");
        result.Invoice.Should().BeNull();
    }

    [Fact]
    public async Task PaidInvoice_ShouldNotBeRegenerated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var paidInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.Paid,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-10),
            PaidAtUtc = DateTime.UtcNow.AddDays(-3),
            InvoiceNumber = "INV-2024-001",
            TotalAmount = 10000m,
            PaidAmount = 10000m
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
            .ReturnsAsync(paidInvoice);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
        result.ErrorMessage.Should().Contain("Paid");
        result.Invoice.Should().BeNull();
    }

    [Fact]
    public async Task PartiallyPaidInvoice_ShouldNotBeRegenerated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var partiallyPaidInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            OrgId = orgId,
            Status = InvoiceStatus.PartiallyPaid,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-10),
            InvoiceNumber = "INV-2024-001",
            TotalAmount = 10000m,
            PaidAmount = 5000m,
            BalanceAmount = 5000m
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
            .ReturnsAsync(partiallyPaidInvoice);

        // Act
        var result = await _invoiceService.GenerateInvoiceAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
        result.ErrorMessage.Should().Contain("PartiallyPaid");
        result.Invoice.Should().BeNull();
    }

    [Fact]
    public async Task DraftInvoice_CanBeRegenerated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var billingPeriodStart = new DateOnly(2024, 1, 1);
        var billingPeriodEnd = new DateOnly(2024, 1, 31);

        var draftInvoice = new Invoice
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
            .ReturnsAsync(draftInvoice);

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
        result.Invoice!.Status.Should().Be(InvoiceStatus.Draft);
    }

    #endregion

    #region Invoice State Tracking Tests

    [Fact]
    public void IssuedInvoice_ShouldHaveIssuedTimestamp()
    {
        // Arrange & Act
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Issued,
            IssuedAtUtc = DateTime.UtcNow
        };

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.IssuedAtUtc.Should().NotBeNull();
        invoice.IssuedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void VoidedInvoice_ShouldHaveVoidedTimestampAndReason()
    {
        // Arrange & Act
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Voided,
            VoidedAtUtc = DateTime.UtcNow,
            VoidReason = "Customer requested cancellation"
        };

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Voided);
        invoice.VoidedAtUtc.Should().NotBeNull();
        invoice.VoidedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.VoidReason.Should().NotBeNullOrEmpty();
        invoice.VoidReason.Should().Be("Customer requested cancellation");
    }

    [Fact]
    public void PaidInvoice_ShouldHavePaidTimestamp()
    {
        // Arrange & Act
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Paid,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-10),
            PaidAtUtc = DateTime.UtcNow,
            TotalAmount = 10000m,
            PaidAmount = 10000m,
            BalanceAmount = 0m
        };

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
        invoice.PaidAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.PaidAtUtc.Should().BeAfter(invoice.IssuedAtUtc!.Value);
        invoice.BalanceAmount.Should().Be(0m);
    }

    #endregion

    #region Credit Note Workflow Tests

    [Fact]
    public void CreditNote_ShouldLinkToInvoice()
    {
        // Arrange & Act
        var invoiceId = Guid.NewGuid();
        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            OrgId = Guid.NewGuid(),
            CreditNoteNumber = "CN-2024-001",
            CreditNoteDate = DateOnly.FromDateTime(DateTime.Today),
            Reason = CreditNoteReason.Refund,
            TotalAmount = 1000m,
            Notes = "Partial refund for service issue"
        };

        // Assert
        creditNote.InvoiceId.Should().Be(invoiceId);
        creditNote.Reason.Should().Be(CreditNoteReason.Refund);
        creditNote.TotalAmount.Should().Be(1000m);
        creditNote.AppliedAtUtc.Should().BeNull(); // Not yet applied
    }

    [Fact]
    public void AppliedCreditNote_ShouldHaveTimestamp()
    {
        // Arrange & Act
        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            CreditNoteNumber = "CN-2024-001",
            CreditNoteDate = DateOnly.FromDateTime(DateTime.Today),
            Reason = CreditNoteReason.Refund,
            TotalAmount = 1000m,
            AppliedAtUtc = DateTime.UtcNow
        };

        // Assert
        creditNote.AppliedAtUtc.Should().NotBeNull();
        creditNote.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreditNoteLine_ShouldLinkToInvoiceLine()
    {
        // Arrange & Act
        var invoiceLineId = Guid.NewGuid();
        var creditNoteLine = new CreditNoteLine
        {
            Id = Guid.NewGuid(),
            CreditNoteId = Guid.NewGuid(),
            InvoiceLineId = invoiceLineId,
            Description = "Refund for rent adjustment",
            Amount = 500m,
            Notes = "Prorated refund"
        };

        // Assert
        creditNoteLine.InvoiceLineId.Should().Be(invoiceLineId);
        creditNoteLine.Amount.Should().Be(500m);
    }

    #endregion

    #region Billing Setting Proration Method Tests

    [Fact]
    public void LeaseBillingSetting_DefaultProrationMethod_ShouldBeActualDays()
    {
        // Arrange & Act
        var setting = new LeaseBillingSetting
        {
            LeaseId = Guid.NewGuid(),
            BillingDay = 1
        };

        // Assert
        setting.ProrationMethod.Should().Be(ProrationMethod.ActualDaysInMonth);
    }

    [Fact]
    public void LeaseBillingSetting_CanSetThirtyDayProration()
    {
        // Arrange & Act
        var setting = new LeaseBillingSetting
        {
            LeaseId = Guid.NewGuid(),
            BillingDay = 1,
            ProrationMethod = ProrationMethod.ThirtyDayMonth
        };

        // Assert
        setting.ProrationMethod.Should().Be(ProrationMethod.ThirtyDayMonth);
    }

    [Fact]
    public void LeaseBillingSetting_BillingDayShouldBeValidated()
    {
        // Arrange & Act
        var validSettings = new[]
        {
            new LeaseBillingSetting { LeaseId = Guid.NewGuid(), BillingDay = 1 },
            new LeaseBillingSetting { LeaseId = Guid.NewGuid(), BillingDay = 15 },
            new LeaseBillingSetting { LeaseId = Guid.NewGuid(), BillingDay = 28 }
        };

        // Assert - all valid settings should have billing day between 1-28
        foreach (var setting in validSettings)
        {
            setting.BillingDay.Should().BeInRange((byte)1, (byte)28);
        }
    }

    #endregion
}
