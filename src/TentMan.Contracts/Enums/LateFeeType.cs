namespace TentMan.Contracts.Enums;

/// <summary>
/// Type of late fee charged for overdue rent.
/// </summary>
public enum LateFeeType : byte
{
    None = 0,
    Flat = 1,
    PerDay = 2,
    Percent = 3
}
