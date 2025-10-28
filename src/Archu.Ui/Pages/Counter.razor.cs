namespace Archu.Ui.Pages;

/// <summary>
/// Counter page backing logic for incrementing a displayed value.
/// </summary>
public partial class Counter
{
    private int currentCount = 0;

    /// <summary>
    /// Increments the current counter value each time the user presses the button.
    /// </summary>
    private void IncrementCount()
    {
        currentCount++;
    }
}
