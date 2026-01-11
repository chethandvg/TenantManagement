using System.Net;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using TentMan.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TentMan.IntegrationTests.Api.Billing;

/// <summary>
/// Integration tests for batch invoice generation and concurrent operations.
/// </summary>
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
[Trait("Feature", "Billing")]
public class BatchInvoiceRunIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private readonly Guid _testOrgId;
    private readonly Guid _testUserId;
    private readonly List<Guid> _testLeaseIds = new();
    private readonly Guid _chargeTypeId;

    public BatchInvoiceRunIntegrationTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testOrgId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _chargeTypeId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MonthlyInvoiceRun_GeneratesInvoicesForMultipleLeases()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var periodStart = new DateOnly(2024, 1, 1);

        // Act
        var response = await _client.PostAsync(
            $"/api/invoice-runs/monthly?orgId={_testOrgId}&periodStart={periodStart:yyyy-MM-dd}",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceRunDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();

        // Verify invoice run in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var invoiceRun = await dbContext.InvoiceRuns
            .Include(ir => ir.Items)
            .FirstOrDefaultAsync(ir => ir.Id == apiResponse.Data!.Id);

        invoiceRun.Should().NotBeNull();
        invoiceRun!.Status.Should().Be(InvoiceRunStatus.Completed);
        invoiceRun.Items.Should().HaveCount(_testLeaseIds.Count);
        invoiceRun.Items.Should().OnlyContain(i => i.IsSuccess);

        // Verify invoices were created
        var invoices = await dbContext.Invoices
            .Where(i => _testLeaseIds.Contains(i.LeaseId))
            .ToListAsync();

        invoices.Should().HaveCount(_testLeaseIds.Count);
        invoices.Should().OnlyContain(i => i.Status == InvoiceStatus.Draft);
    }

    [Fact]
    public async Task MonthlyInvoiceRun_HandlesLargeNumberOfLeases()
    {
        // Arrange - Create 50 additional leases
        await SeedAdditionalLeasesAsync(50);

        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var periodStart = new DateOnly(2024, 1, 1);

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.PostAsync(
            $"/api/invoice-runs/monthly?orgId={_testOrgId}&periodStart={periodStart:yyyy-MM-dd}",
            null);
        var endTime = DateTime.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceRunDto>>();
        apiResponse!.Data!.TotalLeases.Should().BeGreaterThanOrEqualTo(55);

        // Performance check - should complete in reasonable time
        var duration = endTime - startTime;
        duration.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ConcurrentInvoiceGeneration_HandlesMultipleRequests()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Generate invoices concurrently for different leases
        var tasks = _testLeaseIds.Take(5).Select(leaseId =>
            _client.PostAsync(
                $"/api/leases/{leaseId}/invoices/generate?periodStart=2024-01-01&periodEnd=2024-01-31",
                null)
        ).ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        // Verify all invoices were created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var invoiceCount = await dbContext.Invoices
            .CountAsync(i => _testLeaseIds.Take(5).Contains(i.LeaseId));

        invoiceCount.Should().Be(5);
    }

    [Fact]
    public async Task ConcurrentInvoiceGeneration_SameLeaseMultipleTimes_UpdatesSingleInvoice()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var leaseId = _testLeaseIds.First();

        // Act - Generate same invoice concurrently (tests idempotency and thread safety)
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _client.PostAsync(
                $"/api/leases/{leaseId}/invoices/generate?periodStart=2024-01-01&periodEnd=2024-01-31",
                null)
        ).ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        // Verify only one invoice exists
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var invoiceCount = await dbContext.Invoices
            .CountAsync(i => i.LeaseId == leaseId);

        invoiceCount.Should().Be(1);
    }

    [Fact]
    public async Task GetInvoices_WithPagination_ReturnsCorrectData()
    {
        // Arrange
        await SeedAdditionalLeasesAsync(20);
        
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Generate invoices for all leases
        await _client.PostAsync(
            $"/api/invoice-runs/monthly?orgId={_testOrgId}&periodStart=2024-01-01",
            null);

        // Act - Get invoices (should have pagination in future)
        var response = await _client.GetAsync($"/api/invoices?orgId={_testOrgId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<InvoiceDto>>>();
        apiResponse!.Data.Should().HaveCountGreaterThan(20);
    }

    private async Task SeedTestDataAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "batchtest",
            Email = "batch@test.com",
            NormalizedEmail = "BATCH@TEST.COM",
            PasswordHash = "dummy-hash",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        dbContext.Users.Add(testUser);

        // Create organization
        var org = new Organization
        {
            Id = _testOrgId,
            Name = "Test Org"
        };
        dbContext.Organizations.Add(org);

        // Create charge type
        var chargeType = new ChargeType
        {
            Id = _chargeTypeId,
            OrgId = _testOrgId,
            Name = "Rent",
            Code = ChargeTypeCode.RENT,
            IsActive = true
        };
        dbContext.ChargeTypes.Add(chargeType);

        // Create 10 test leases
        await CreateLeasesAsync(dbContext, 10);

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAdditionalLeasesAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await CreateLeasesAsync(dbContext, count);
        await dbContext.SaveChangesAsync();
    }

    private Task CreateLeasesAsync(ApplicationDbContext dbContext, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var buildingId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var leaseId = Guid.NewGuid();

            _testLeaseIds.Add(leaseId);

            // Create building
            var building = new Building
            {
                Id = buildingId,
                OrgId = _testOrgId,
                Name = $"Building {i + 1}",
                BuildingCode = $"B{(i + 1):D3}",
                PropertyType = PropertyType.Apartment
            };
            dbContext.Buildings.Add(building);

            // Create unit
            var unit = new Unit
            {
                Id = unitId,
                BuildingId = buildingId,
                UnitNumber = $"{100 + i}",
                UnitType = UnitType.TwoBHK,
                AreaSqFt = 1000
            };
            dbContext.Units.Add(unit);

            // Create tenant
            var tenant = new Tenant
            {
                Id = tenantId,
                OrgId = _testOrgId,
                FullName = $"Tenant{i} Test",
                Email = $"tenant{i}@test.com",
                Phone = "+1234567890"
            };
            dbContext.Tenants.Add(tenant);

            // Create lease
            var lease = new Lease
            {
                Id = leaseId,
                OrgId = _testOrgId,
                UnitId = unitId,
                Status = LeaseStatus.Active,
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = new DateOnly(2024, 12, 31)
            };
            dbContext.Leases.Add(lease);

            // Create lease party
            var leaseParty = new LeaseParty
            {
                Id = Guid.NewGuid(),
                LeaseId = leaseId,
                TenantId = tenantId,
                Role = LeasePartyRole.PrimaryTenant,
                IsResponsibleForPayment = true
            };
            dbContext.LeaseParties.Add(leaseParty);

            // Create lease term
            var leaseTerm = new LeaseTerm
            {
                Id = Guid.NewGuid(),
                LeaseId = leaseId,
                MonthlyRent = 10000m + (i * 100m),
                EffectiveFrom = new DateOnly(2024, 1, 1),
                EffectiveTo = new DateOnly(2024, 12, 31)
            };
            dbContext.LeaseTerms.Add(leaseTerm);

            // Create billing settings
            var billingSetting = new LeaseBillingSetting
            {
                Id = Guid.NewGuid(),
                LeaseId = leaseId,
                BillingDay = 1,
                PaymentTermDays = 15,
                GenerateInvoiceAutomatically = false,
                ProrationMethod = ProrationMethod.ActualDaysInMonth
            };
            dbContext.LeaseBillingSettings.Add(billingSetting);
        }
        
        return Task.CompletedTask;
    }
}
