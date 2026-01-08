using System;
using TentMan.Ui.State;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TentMan.Ui.Components.State;

/// <summary>
/// Wraps UI content with a standardized busy/error presentation driven by a shared <see cref="UiState"/> instance.
/// </summary>
public partial class BusyBoundary : ComponentBase, IDisposable
{
    private UiState? state;

    /// <summary>
    /// Gets or sets the default <see cref="UiState"/> injected from the service container when no explicit value is supplied.
    /// </summary>
    [Inject]
    public UiState DefaultState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UI state used to drive the boundary. When omitted, <see cref="DefaultState"/> is used.
    /// </summary>
    [Parameter]
    public UiState? Value { get; set; }

    /// <summary>
    /// Gets or sets the content to render when the boundary is neither busy nor in an error state.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets custom markup for rendering error content. The boundary will supply the error message as the parameter.
    /// </summary>
    [Parameter]
    public RenderFragment<string>? ErrorContent { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked when the consumer chooses to retry after an error.
    /// </summary>
    [Parameter]
    public EventCallback Retry { get; set; }

    /// <summary>
    /// Gets or sets the label for the default retry button rendered when <see cref="Retry"/> is supplied.
    /// </summary>
    [Parameter]
    public string RetryLabel { get; set; } = "Retry";

    /// <summary>
    /// Gets or sets the MudBlazor color used by the busy indicator.
    /// </summary>
    [Parameter]
    public Color BusyColor { get; set; } = Color.Primary;

    /// <summary>
    /// Gets or sets the MudBlazor size applied to the busy indicator.
    /// </summary>
    [Parameter]
    public Size BusySize { get; set; } = Size.Large;

    /// <summary>
    /// Gets or sets the CSS class used by the busy container.
    /// </summary>
    [Parameter]
    public string BusyCssClass { get; set; } = "d-flex flex-column align-items-center justify-content-center py-6";

    /// <summary>
    /// Gets the <see cref="UiState"/> instance driving the boundary.
    /// </summary>
    protected UiState State => state ??= Value ?? DefaultState;

    /// <summary>
    /// Subscribes to busy state changes when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        state = Value ?? DefaultState;
        Subscribe();
    }

    /// <summary>
    /// Handles parameter updates so the boundary resubscribes when a different <see cref="UiState"/> is provided.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var resolvedState = Value ?? DefaultState;

        if (!ReferenceEquals(state, resolvedState))
        {
            Unsubscribe();
            state = resolvedState;
            Subscribe();
        }
    }

    /// <summary>
    /// Re-renders the component when the busy state raises a change notification.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="args">The event arguments.</param>
    private void OnBusyStateChanged(object? sender, EventArgs args)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Attaches the boundary to the current <see cref="BusyState"/> change notifications.
    /// </summary>
    private void Subscribe()
    {
        State.Busy.StateChanged += OnBusyStateChanged;
    }

    /// <summary>
    /// Detaches the boundary from the current <see cref="BusyState"/> change notifications.
    /// </summary>
    private void Unsubscribe()
    {
        if (state != null)
        {
            state.Busy.StateChanged -= OnBusyStateChanged;
        }
    }

    /// <summary>
    /// Disposes the boundary and releases event subscriptions.
    /// </summary>
    public void Dispose()
    {
        Unsubscribe();
        GC.SuppressFinalize(this);
    }
}
