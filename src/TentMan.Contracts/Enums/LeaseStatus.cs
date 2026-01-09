namespace TentMan.Contracts.Enums;

/// <summary>
/// Status of a lease contract.
/// </summary>
public enum LeaseStatus : byte
{
    Draft = 1,
    Active = 2,
    NoticeGiven = 3,
    Ended = 4,
    Cancelled = 5
}
