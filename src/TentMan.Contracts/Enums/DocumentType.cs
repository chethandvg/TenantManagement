namespace TentMan.Contracts.Enums;

/// <summary>
/// Type of tenant document.
/// </summary>
public enum DocumentType : byte
{
    IDProof = 1,
    AddressProof = 2,
    Photo = 3,
    PoliceVerification = 4,
    Agreement = 5,
    Other = 6
}
