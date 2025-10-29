using System;

namespace Archu.Ui.State;

/// <summary>
/// Tracks long-running operations and related error messages so components can render consistent busy and failure experiences.
/// </summary>
public sealed class BusyState
{
    private readonly object syncRoot = new();
    private int operationDepth;

    /// <summary>
    /// Occurs whenever the busy or error state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Gets a value indicating whether any tracked operation is currently in progress.
    /// </summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Gets the optional human-readable message associated with the active busy state.
    /// </summary>
    public string? BusyMessage { get; private set; }

    /// <summary>
    /// Gets the error message to display when the most recent operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Marks the beginning of a busy operation and returns an <see cref="IDisposable"/> scope that clears the busy state on disposal.
    /// </summary>
    /// <param name="message">An optional message describing the work that is being performed.</param>
    /// <returns>An <see cref="IDisposable"/> scope that should be disposed when the operation completes.</returns>
    public IDisposable Begin(string? message = null)
    {
        lock (syncRoot)
        {
            operationDepth++;
            IsBusy = true;

            if (!string.IsNullOrWhiteSpace(message))
            {
                BusyMessage = message;
            }
        }

        NotifyStateChanged();
        return new BusyOperationScope(this);
    }

    /// <summary>
    /// Updates the busy message without altering the current busy flag, allowing components to surface progress details.
    /// </summary>
    /// <param name="message">The busy message to display, or <see langword="null"/> to clear it.</param>
    public void SetBusyMessage(string? message)
    {
        lock (syncRoot)
        {
            BusyMessage = message;
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Records an error message so that UI consumers can surface consistent failure notifications.
    /// </summary>
    /// <param name="message">The error message to display, or <see langword="null"/> to clear it.</param>
    public void SetError(string? message)
    {
        lock (syncRoot)
        {
            ErrorMessage = message;
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears any existing error message and raises <see cref="StateChanged"/> if a value was removed.
    /// </summary>
    public void ClearError()
    {
        bool cleared;

        lock (syncRoot)
        {
            cleared = ErrorMessage != null;
            ErrorMessage = null;
        }

        if (cleared)
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Decrements the busy operation depth and notifies listeners about any resulting state changes.
    /// </summary>
    private void CompleteOperation()
    {
        var shouldNotify = false;

        lock (syncRoot)
        {
            if (operationDepth == 0)
            {
                return;
            }

            operationDepth--;
            shouldNotify = true;

            if (operationDepth == 0)
            {
                IsBusy = false;
                BusyMessage = null;
            }
        }

        if (shouldNotify)
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event to notify components that they should re-render.
    /// </summary>
    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class BusyOperationScope : IDisposable
    {
        private readonly BusyState owner;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusyOperationScope"/> class to release busy state changes on disposal.
        /// </summary>
        /// <param name="owner">The <see cref="BusyState"/> being tracked.</param>
        public BusyOperationScope(BusyState owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Disposes the busy scope and signals the parent <see cref="BusyState"/> that the operation completed.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            owner.CompleteOperation();
        }
    }
}
