namespace TentMan.Contracts.Enums;

/// <summary>
/// Type of deposit transaction.
/// </summary>
public enum DepositTransactionType : byte
{
    Collected = 1,
    Refund = 2,
    Deduction = 3,
    Adjustment = 4
}
