namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the method used for calculating prorated charges.
/// </summary>
public enum ProrationMethod
{
    /// <summary>
    /// Use actual number of days in the month (28-31)
    /// </summary>
    ActualDaysInMonth = 1,

    /// <summary>
    /// Use fixed 30 days per month regardless of actual calendar days
    /// </summary>
    ThirtyDayMonth = 2
}
