namespace TentMan.Contracts.Enums;

/// <summary>
/// Role of a party in a lease agreement.
/// </summary>
public enum LeasePartyRole : byte
{
    PrimaryTenant = 1,
    CoTenant = 2,
    Occupant = 3,
    Guarantor = 4
}
