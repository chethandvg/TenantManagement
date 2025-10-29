using Archu.Ui.Configuration;
using Archu.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Archu.Ui.Pages;

/// <summary>
/// Counter page backing logic for incrementing a displayed value.
/// </summary>
public partial class Counter
{
    private bool isLoading = true;
    private bool isPreviewEnabled;
    private int currentCount = 0;

    [Inject]
    protected IClientFeatureService ClientFeatureService { get; set; } = default!;

    /// <summary>
    /// Evaluates the preview feature flag so the page can adjust its visibility.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        isPreviewEnabled = await ClientFeatureService.IsEnabledAsync(ClientFeatureFlags.PreviewComponents);
        isLoading = false;
    }

    /// <summary>
    /// Increments the current counter value each time the user presses the button.
    /// </summary>
    private void IncrementCount()
    {
        currentCount++;
    }
}
