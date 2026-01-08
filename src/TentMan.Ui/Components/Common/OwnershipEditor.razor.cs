using Microsoft.AspNetCore.Components;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Owners;

namespace TentMan.Ui.Components.Common;

/// <summary>
/// A reusable component for editing ownership shares with validation.
/// </summary>
public partial class OwnershipEditor : ComponentBase
{
    private const decimal ValidTotalPercent = 100.00M;
    private const decimal Epsilon = 0.01M;

    /// <summary>
    /// The title displayed at the top of the editor.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Ownership";

    /// <summary>
    /// The list of ownership shares to edit.
    /// </summary>
    [Parameter]
    public List<OwnershipShareModel> Shares { get; set; } = new();

    /// <summary>
    /// Callback when shares are changed.
    /// </summary>
    [Parameter]
    public EventCallback<List<OwnershipShareModel>> SharesChanged { get; set; }

    /// <summary>
    /// Available owners to select from.
    /// </summary>
    [Parameter]
    public IEnumerable<OwnerDto> AvailableOwners { get; set; } = Enumerable.Empty<OwnerDto>();

    /// <summary>
    /// Whether the editor is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether to show the effective date picker.
    /// </summary>
    [Parameter]
    public bool ShowEffectiveDate { get; set; } = true;

    /// <summary>
    /// The effective date for the ownership changes.
    /// </summary>
    [Parameter]
    public DateTime? EffectiveDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Callback when effective date changes.
    /// </summary>
    [Parameter]
    public EventCallback<DateTime?> EffectiveDateChanged { get; set; }

    /// <summary>
    /// Callback for adding a new owner.
    /// </summary>
    [Parameter]
    public EventCallback OnAddOwnerRequested { get; set; }

    /// <summary>
    /// Gets the total percentage of all shares.
    /// </summary>
    public decimal TotalPercent => Shares.Sum(s => s.SharePercent);

    /// <summary>
    /// Gets whether the shares are valid (sum to 100% within epsilon, no duplicates).
    /// </summary>
    public bool IsValid => Math.Abs(TotalPercent - ValidTotalPercent) <= Epsilon && !HasDuplicateOwners();

    /// <summary>
    /// Gets the validation message if shares are invalid.
    /// </summary>
    public string? ValidationMessage => GetValidationMessage();

    private string GetOwnerName(Guid ownerId)
    {
        return AvailableOwners.FirstOrDefault(o => o.Id == ownerId)?.DisplayName ?? "Unknown Owner";
    }

    private bool IsDuplicateOwner(OwnershipShareModel share)
    {
        return Shares.Count(s => s.OwnerId == share.OwnerId) > 1;
    }

    private bool HasDuplicateOwners()
    {
        var ownerIds = Shares.Select(s => s.OwnerId).Where(id => id != Guid.Empty).ToList();
        return ownerIds.Count != ownerIds.Distinct().Count();
    }

    private string? GetValidationMessage()
    {
        if (!Shares.Any())
        {
            return null;
        }

        if (HasDuplicateOwners())
        {
            return "Each owner can only appear once in the ownership shares.";
        }

        if (Shares.Any(s => s.SharePercent <= 0))
        {
            return "Share percentage must be greater than 0.";
        }

        if (Math.Abs(TotalPercent - ValidTotalPercent) > Epsilon)
        {
            return $"Total ownership must equal 100%. Current total: {TotalPercent:F2}%";
        }

        return null;
    }

    private async Task AddOwner()
    {
        if (OnAddOwnerRequested.HasDelegate)
        {
            await OnAddOwnerRequested.InvokeAsync();
        }
        else
        {
            var firstAvailableOwner = AvailableOwners
                .FirstOrDefault(o => !Shares.Any(s => s.OwnerId == o.Id));

            Shares.Add(new OwnershipShareModel
            {
                OwnerId = firstAvailableOwner?.Id ?? Guid.Empty,
                SharePercent = 0
            });

            await NotifySharesChanged();
        }
    }

    private async Task OnOwnerChanged(OwnershipShareModel share, Guid newOwnerId)
    {
        share.OwnerId = newOwnerId;
        await NotifySharesChanged();
    }

    private async Task OnShareChanged(OwnershipShareModel share, decimal newValue)
    {
        share.SharePercent = newValue;
        await NotifySharesChanged();
    }

    private async Task RemoveShare(OwnershipShareModel share)
    {
        Shares.Remove(share);
        await NotifySharesChanged();
    }

    private async Task NotifySharesChanged()
    {
        await SharesChanged.InvokeAsync(Shares);
    }
}

/// <summary>
/// Model for ownership share editing.
/// </summary>
public class OwnershipShareModel
{
    public Guid OwnerId { get; set; }
    public decimal SharePercent { get; set; }
}
