using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to add a deposit transaction.
/// </summary>
public class AddDepositTransactionRequest
{
    public DepositTransactionType TxnType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TxnDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
