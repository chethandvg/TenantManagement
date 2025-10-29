using System;

namespace Archu.Ui.State;

/// <summary>
/// Aggregates shared UI containers (busy state, notifications, etc.) so pages can compose common workflows.
/// </summary>
public sealed class UiState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UiState"/> class with the provided busy state.
    /// </summary>
    /// <param name="busyState">The busy state tracker used to communicate loading and error feedback.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="busyState"/> is <see langword="null"/>.</exception>
    public UiState(BusyState busyState)
    {
        Busy = busyState ?? throw new ArgumentNullException(nameof(busyState));
    }

    /// <summary>
    /// Gets the busy state tracker that coordinates loading and error messaging for a page.
    /// </summary>
    public BusyState Busy { get; }
}
