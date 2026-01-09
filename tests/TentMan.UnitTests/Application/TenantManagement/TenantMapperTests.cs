using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement;

public class TenantMapperTests
{
    [Fact]
    public void ToDetailDto_MapsBasicProperties_Correctly()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var result = TenantMapper.ToDetailDto(tenant);

        // Assert
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal(tenant.OrgId, result.OrgId);
        Assert.Equal(tenant.FullName, result.FullName);
        Assert.Equal(tenant.Phone, result.Phone);
        Assert.Equal(tenant.Email, result.Email);
        Assert.Equal(tenant.DateOfBirth, result.DateOfBirth);
        Assert.Equal(tenant.Gender, result.Gender);
        Assert.Equal(tenant.IsActive, result.IsActive);
    }

    [Fact]
    public void ToDetailDto_MapsAddresses_Correctly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Addresses.Add(new TenantAddress
        {
            Id = Guid.NewGuid(),
            Type = AddressType.Current,
            Line1 = "123 Main Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            Country = "IN",
            IsPrimary = true
        });

        // Act
        var result = TenantMapper.ToDetailDto(tenant);

        // Assert
        Assert.Single(result.Addresses);
        Assert.Equal("123 Main Street", result.Addresses[0].Line1);
        Assert.Equal(AddressType.Current, result.Addresses[0].Type);
        Assert.True(result.Addresses[0].IsPrimary);
    }

    [Fact]
    public void ToDetailDto_MapsEmergencyContacts_Correctly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.EmergencyContacts.Add(new TenantEmergencyContact
        {
            Id = Guid.NewGuid(),
            Name = "Jane Doe",
            Relationship = "Spouse",
            Phone = "+919876543210"
        });

        // Act
        var result = TenantMapper.ToDetailDto(tenant);

        // Assert
        Assert.Single(result.EmergencyContacts);
        Assert.Equal("Jane Doe", result.EmergencyContacts[0].Name);
        Assert.Equal("Spouse", result.EmergencyContacts[0].Relationship);
    }

    [Fact]
    public void ToDetailDto_MapsDocuments_Correctly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var fileId = Guid.NewGuid();
        tenant.Documents.Add(new TenantDocument
        {
            Id = Guid.NewGuid(),
            DocType = DocumentType.IDProof,
            DocNumberMasked = "XXXX1234",
            FileId = fileId,
            File = new FileMetadata
            {
                Id = fileId,
                FileName = "aadhaar.pdf"
            }
        });

        // Act
        var result = TenantMapper.ToDetailDto(tenant);

        // Assert
        Assert.Single(result.Documents);
        Assert.Equal(DocumentType.IDProof, result.Documents[0].DocType);
        Assert.Equal("XXXX1234", result.Documents[0].DocNumberMasked);
        Assert.Equal("aadhaar.pdf", result.Documents[0].FileName);
    }

    [Fact]
    public void ToDetailDto_HandlesNullCollections_Gracefully()
    {
        // Arrange
        var tenant = CreateTestTenant();
        // Ensure collections are empty but not null

        // Act
        var result = TenantMapper.ToDetailDto(tenant);

        // Assert
        Assert.Empty(result.Addresses);
        Assert.Empty(result.EmergencyContacts);
        Assert.Empty(result.Documents);
    }

    private static Tenant CreateTestTenant()
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            FullName = "John Doe",
            Phone = "+919876543210",
            Email = "john.doe@example.com",
            DateOfBirth = new DateOnly(1990, 1, 15),
            Gender = Gender.Male,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = new byte[] { 0x00 }
        };
    }
}
