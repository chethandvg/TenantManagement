namespace TentMan.Contracts.Enums;

/// <summary>
/// Type of address for a tenant.
/// </summary>
public enum AddressType : byte
{
    Current = 1,
    Permanent = 2,
    Office = 3,
    Other = 4
}
