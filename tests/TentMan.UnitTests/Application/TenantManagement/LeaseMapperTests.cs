using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement;

public class LeaseMapperTests
{
    [Fact]
    public void ToDetailDto_MapsBasicProperties_Correctly()
    {
        // Arrange
        var lease = CreateTestLease();

        // Act
        var result = LeaseMapper.ToDetailDto(lease);

        // Assert
        Assert.Equal(lease.Id, result.Id);
        Assert.Equal(lease.OrgId, result.OrgId);
        Assert.Equal(lease.UnitId, result.UnitId);
        Assert.Equal(lease.LeaseNumber, result.LeaseNumber);
        Assert.Equal(lease.Status, result.Status);
        Assert.Equal(lease.StartDate, result.StartDate);
        Assert.Equal(lease.EndDate, result.EndDate);
        Assert.Equal(lease.RentDueDay, result.RentDueDay);
        Assert.Equal(lease.GraceDays, result.GraceDays);
        Assert.Equal(lease.LateFeeType, result.LateFeeType);
        Assert.Equal(lease.LateFeeValue, result.LateFeeValue);
        Assert.Equal(lease.IsAutoRenew, result.IsAutoRenew);
    }

    [Fact]
    public void ToDetailDto_FiltersDeletedParties_Correctly()
    {
        // Arrange
        var lease = CreateTestLease();
        lease.Parties.Add(new LeaseParty
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Role = LeasePartyRole.PrimaryTenant,
            IsDeleted = false,
            Tenant = new Tenant { FullName = "Active Tenant", Phone = "1234567890" }
        });
        lease.Parties.Add(new LeaseParty
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Role = LeasePartyRole.CoTenant,
            IsDeleted = true,
            Tenant = new Tenant { FullName = "Deleted Tenant", Phone = "0987654321" }
        });

        // Act
        var result = LeaseMapper.ToDetailDto(lease);

        // Assert
        Assert.Single(result.Parties);
        Assert.Equal("Active Tenant", result.Parties[0].TenantName);
    }

    [Fact]
    public void ToDetailDto_FiltersDeletedTerms_Correctly()
    {
        // Arrange
        var lease = CreateTestLease();
        lease.Terms.Add(new LeaseTerm
        {
            Id = Guid.NewGuid(),
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            MonthlyRent = 10000m,
            SecurityDeposit = 20000m,
            IsDeleted = false
        });
        lease.Terms.Add(new LeaseTerm
        {
            Id = Guid.NewGuid(),
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
            MonthlyRent = 9000m,
            SecurityDeposit = 18000m,
            IsDeleted = true
        });

        // Act
        var result = LeaseMapper.ToDetailDto(lease);

        // Assert
        Assert.Single(result.Terms);
        Assert.Equal(10000m, result.Terms[0].MonthlyRent);
    }

    [Fact]
    public void ToDetailDto_CalculatesDepositBalance_Correctly()
    {
        // Arrange
        var lease = CreateTestLease();
        lease.DepositTransactions.Add(new DepositTransaction
        {
            Id = Guid.NewGuid(),
            TxnType = DepositTransactionType.Collected,
            Amount = 50000m,
            TxnDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDeleted = false
        });
        lease.DepositTransactions.Add(new DepositTransaction
        {
            Id = Guid.NewGuid(),
            TxnType = DepositTransactionType.Deduction,
            Amount = 5000m,
            TxnDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDeleted = false
        });
        lease.DepositTransactions.Add(new DepositTransaction
        {
            Id = Guid.NewGuid(),
            TxnType = DepositTransactionType.Refund,
            Amount = 10000m,
            TxnDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDeleted = false
        });
        // This one is deleted and should not be counted
        lease.DepositTransactions.Add(new DepositTransaction
        {
            Id = Guid.NewGuid(),
            TxnType = DepositTransactionType.Collected,
            Amount = 100000m,
            TxnDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDeleted = true
        });

        // Act
        var result = LeaseMapper.ToDetailDto(lease);

        // Assert
        Assert.Equal(50000m, result.TotalDepositCollected);
        Assert.Equal(35000m, result.DepositBalance); // 50000 - 5000 - 10000
    }

    [Fact]
    public void ToDetailDto_IdentifiesActiveTerm_Correctly()
    {
        // Arrange
        var lease = CreateTestLease();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Active term (covers today)
        lease.Terms.Add(new LeaseTerm
        {
            Id = Guid.NewGuid(),
            EffectiveFrom = today.AddMonths(-1),
            EffectiveTo = today.AddMonths(11),
            MonthlyRent = 15000m,
            SecurityDeposit = 30000m,
            IsDeleted = false
        });
        
        // Past term (ended)
        lease.Terms.Add(new LeaseTerm
        {
            Id = Guid.NewGuid(),
            EffectiveFrom = today.AddMonths(-12),
            EffectiveTo = today.AddMonths(-2),
            MonthlyRent = 12000m,
            SecurityDeposit = 24000m,
            IsDeleted = false
        });

        // Act
        var result = LeaseMapper.ToDetailDto(lease);

        // Assert
        Assert.Equal(2, result.Terms.Count);
        var activeTerm = result.Terms.Single(t => t.IsActive);
        Assert.Equal(15000m, activeTerm.MonthlyRent);
    }

    private static Lease CreateTestLease()
    {
        return new Lease
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            LeaseNumber = "LSE-001",
            Status = LeaseStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11)),
            RentDueDay = 1,
            GraceDays = 5,
            LateFeeType = LateFeeType.Flat,
            LateFeeValue = 500m,
            IsAutoRenew = false,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = new byte[] { 0x00 },
            Unit = new Unit { UnitNumber = "101", Building = new Building { Name = "Test Building" } }
        };
    }
}
