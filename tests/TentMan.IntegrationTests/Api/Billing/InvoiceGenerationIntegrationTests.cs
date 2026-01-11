using System.Net;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using TentMan.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace TentMan.IntegrationTests.Api.Billing;

/// <summary>
/// Integration tests for end-to-end invoice generation with database and services.
/// </summary>
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
[Trait("Feature", "Billing")]
public class InvoiceGenerationIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private Guid _testOrgId;
    private Guid _testUserId;
    private Guid _testPropertyId;
    private Guid _testUnitId;
    private Guid _testLeaseId;
    private Guid _testTenantId;
    private Guid _chargeTypeId;

    public InvoiceGenerationIntegrationTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testOrgId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testPropertyId = Guid.NewGuid();
        _testUnitId = Guid.NewGuid();
        _testLeaseId = Guid.NewGuid();
        _testTenantId = Guid.NewGuid();
        _chargeTypeId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GenerateInvoice_EndToEnd_CreatesInvoiceInDatabase()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate?periodStart=2024-01-01&periodEnd=2024-01-31",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var invoiceInDb = await dbContext.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.LeaseId == _testLeaseId);

        invoiceInDb.Should().NotBeNull();
        invoiceInDb!.Status.Should().Be(InvoiceStatus.Draft);
        invoiceInDb.Lines.Should().NotBeEmpty();
        invoiceInDb.TotalAmount.Should().Be(10000m); // Full month rent
    }

    [Fact]
    public async Task GenerateInvoice_WithProration_CalculatesCorrectAmount()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Generate invoice for half month (Jan 1-15)
        var response = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate?periodStart=2024-01-01&periodEnd=2024-01-15",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        apiResponse!.Data!.TotalAmount.Should().BeLessThan(10000m); // Prorated amount
        apiResponse.Data.TotalAmount.Should().BeGreaterThan(4500m); // At least ~half
    }

    [Fact]
    public async Task IssueInvoice_ChangesStatusAndTimestamp()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Generate invoice first
        var generateResponse = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate?periodStart=2024-01-01&periodEnd=2024-01-31",
            null);
        
        var invoiceDto = (await generateResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;

        // Act - Issue the invoice
        var issueResponse = await _client.PostAsync(
            $"/api/invoices/{invoiceDto.Id}/issue",
            null);

        // Assert
        issueResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var issuedInvoice = (await issueResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;
        issuedInvoice.Status.Should().Be(InvoiceStatus.Issued);
        issuedInvoice.IssuedAtUtc.Should().NotBeNull();
        issuedInvoice.IssuedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var invoiceInDb = await dbContext.Invoices.FindAsync(invoiceDto.Id);
        invoiceInDb.Should().NotBeNull();
        invoiceInDb!.Status.Should().Be(InvoiceStatus.Issued);
        invoiceInDb.IssuedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task VoidInvoice_MarksInvoiceAsVoided()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Generate and issue invoice
        var generateResponse = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate",
            null);
        var invoiceDto = (await generateResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;

        await _client.PostAsync($"/api/invoices/{invoiceDto.Id}/issue", null);

        // Act - Void the invoice
        var voidRequest = new { VoidReason = "Customer request" };
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/invoices/{invoiceDto.Id}/void",
            voidRequest);

        // Assert
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var voidedInvoice = (await voidResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;
        voidedInvoice.Status.Should().Be(InvoiceStatus.Voided);
        voidedInvoice.VoidedAtUtc.Should().NotBeNull();
        voidedInvoice.VoidReason.Should().Be("Customer request");
    }

    [Fact]
    public async Task GetInvoices_ReturnsOnlyOrgInvoices()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Generate multiple invoices
        await _client.PostAsync($"/api/leases/{_testLeaseId}/invoices/generate", null);

        // Act
        var response = await _client.GetAsync($"/api/invoices?orgId={_testOrgId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<InvoiceDto>>>();
        apiResponse!.Data.Should().NotBeEmpty();
        apiResponse.Data.Should().OnlyContain(i => i.OrgId == _testOrgId);
    }

    [Fact]
    public async Task RegenerateInvoice_UpdatesExistingDraftInvoice()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Manager", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Generate invoice first time
        var firstResponse = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate",
            null);
        var firstInvoice = (await firstResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;

        // Act - Generate again for same period
        var secondResponse = await _client.PostAsync(
            $"/api/leases/{_testLeaseId}/invoices/generate",
            null);

        // Assert
        var secondInvoice = (await secondResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;
        
        // Should update same invoice, not create new one
        secondInvoice.Id.Should().Be(firstInvoice.Id);

        // Verify only one invoice in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var invoiceCount = await dbContext.Invoices.CountAsync(i => i.LeaseId == _testLeaseId);
        invoiceCount.Should().Be(1);
    }

    private async Task SeedTestDataAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "billingtest",
            Email = "billing@test.com",
            NormalizedEmail = "BILLING@TEST.COM",
            PasswordHash = "dummy-hash",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        dbContext.Users.Add(testUser);

        // Create organization
        var org = new Organization
        {
            Id = _testOrgId,
            Name = "Test Org",
            Description = "Test Organization"
        };
        dbContext.Organizations.Add(org);

        // Create property
        var property = new Property
        {
            Id = _testPropertyId,
            OrgId = _testOrgId,
            Name = "Test Property",
            Address = "123 Test St"
        };
        dbContext.Properties.Add(property);

        // Create unit
        var unit = new Unit
        {
            Id = _testUnitId,
            PropertyId = _testPropertyId,
            UnitNumber = "101",
            FloorArea = 1000
        };
        dbContext.Units.Add(unit);

        // Create tenant
        var tenant = new Tenant
        {
            Id = _testTenantId,
            OrgId = _testOrgId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Phone = "1234567890"
        };
        dbContext.Tenants.Add(tenant);

        // Create lease
        var lease = new Lease
        {
            Id = _testLeaseId,
            OrgId = _testOrgId,
            UnitId = _testUnitId,
            Status = LeaseStatus.Active,
            LeaseStartDate = new DateOnly(2024, 1, 1),
            LeaseEndDate = new DateOnly(2024, 12, 31)
        };
        dbContext.Leases.Add(lease);

        // Create lease party (tenant)
        var leaseParty = new LeaseParty
        {
            Id = Guid.NewGuid(),
            LeaseId = _testLeaseId,
            TenantId = _testTenantId,
            IsPrimaryTenant = true
        };
        dbContext.LeaseParties.Add(leaseParty);

        // Create lease term with rent
        var leaseTerm = new LeaseTerm
        {
            Id = Guid.NewGuid(),
            LeaseId = _testLeaseId,
            RentAmount = 10000m,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = new DateOnly(2024, 12, 31),
            IsActive = true
        };
        dbContext.LeaseTerms.Add(leaseTerm);

        // Create billing settings
        var billingSetting = new LeaseBillingSetting
        {
            Id = Guid.NewGuid(),
            LeaseId = _testLeaseId,
            BillingDay = 1,
            PaymentTermDays = 15,
            GenerateInvoiceAutomatically = false,
            ProrationMethod = ProrationMethod.ActualDaysInMonth
        };
        dbContext.LeaseBillingSettings.Add(billingSetting);

        // Create charge type
        var chargeType = new ChargeType
        {
            Id = _chargeTypeId,
            OrgId = _testOrgId,
            Name = "Rent",
            Code = "RENT",
            Category = ChargeCategory.Rent,
            IsActive = true
        };
        dbContext.ChargeTypes.Add(chargeType);

        await dbContext.SaveChangesAsync();
    }
}
