using TentMan.Contracts.Enums;

namespace TentMan.Contracts.TenantPortal;

/// <summary>
/// DTO for a handover checklist item.
/// </summary>
public class HandoverChecklistItemDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ItemCondition Condition { get; set; }
    public string? Remarks { get; set; }
    public Guid? PhotoFileId { get; set; }
    public string? PhotoFileName { get; set; }
}

/// <summary>
/// Response for fetching a move-in handover checklist.
/// </summary>
public class MoveInHandoverResponse
{
    public Guid HandoverId { get; set; }
    public Guid LeaseId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
    public List<HandoverChecklistItemDto> ChecklistItems { get; set; } = new();
    public List<MeterReadingDto> MeterReadings { get; set; } = new();
}

/// <summary>
/// DTO for meter reading.
/// </summary>
public class MeterReadingDto
{
    public Guid MeterId { get; set; }
    public string MeterType { get; set; } = string.Empty;
    public decimal Reading { get; set; }
    public DateOnly ReadingDate { get; set; }
}

/// <summary>
/// Request for submitting a completed handover checklist.
/// </summary>
public class SubmitHandoverRequest
{
    public Guid HandoverId { get; set; }
    public string? Notes { get; set; }
    public List<HandoverChecklistItemUpdateDto> ChecklistItems { get; set; } = new();
    public List<MeterReadingUpdateDto> MeterReadings { get; set; } = new();
}

/// <summary>
/// DTO for updating a handover checklist item.
/// </summary>
public class HandoverChecklistItemUpdateDto
{
    public Guid Id { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// DTO for updating meter readings.
/// </summary>
public class MeterReadingUpdateDto
{
    public Guid MeterId { get; set; }
    public decimal Reading { get; set; }
    public DateOnly ReadingDate { get; set; }
}
