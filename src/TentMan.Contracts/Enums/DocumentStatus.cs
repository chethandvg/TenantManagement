namespace TentMan.Contracts.Enums;

/// <summary>
/// Status of a tenant document for verification and approval.
/// </summary>
public enum DocumentStatus : byte
{
    /// <summary>
    /// Document has been uploaded but not yet reviewed.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Document has been verified and approved.
    /// </summary>
    Verified = 1,
    
    /// <summary>
    /// Document has been rejected (invalid, unclear, expired, etc.).
    /// </summary>
    Rejected = 2
}
