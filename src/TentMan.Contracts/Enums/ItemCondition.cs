namespace TentMan.Contracts.Enums;

/// <summary>
/// Condition of a handover checklist item.
/// </summary>
public enum ItemCondition : byte
{
    Good = 1,
    Ok = 2,
    Bad = 3,
    Missing = 4
}
