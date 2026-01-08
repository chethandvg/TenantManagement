using Microsoft.AspNetCore.Components;

namespace TentMan.Ui.Components.Common;

/// <summary>
/// A reusable wizard stepper component for multi-step forms.
/// </summary>
public partial class WizardStepper : ComponentBase
{
    private int _activeStepIndex;

    /// <summary>
    /// The list of steps in the wizard.
    /// </summary>
    [Parameter]
    public List<WizardStep> Steps { get; set; } = new();

    /// <summary>
    /// The current active step index.
    /// </summary>
    [Parameter]
    public int ActiveStepIndex
    {
        get => _activeStepIndex;
        set
        {
            if (_activeStepIndex != value)
            {
                _activeStepIndex = value;
                ActiveStepIndexChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// Callback when active step changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> ActiveStepIndexChanged { get; set; }

    /// <summary>
    /// Whether the wizard requires linear progression (must complete steps in order).
    /// </summary>
    [Parameter]
    public bool Linear { get; set; } = true;

    /// <summary>
    /// Whether to show the skip button for optional steps.
    /// </summary>
    [Parameter]
    public bool ShowSkipButton { get; set; }

    /// <summary>
    /// Text for the previous button.
    /// </summary>
    [Parameter]
    public string PreviousButtonText { get; set; } = "Previous";

    /// <summary>
    /// Text for the next button.
    /// </summary>
    [Parameter]
    public string NextButtonText { get; set; } = "Next";

    /// <summary>
    /// Text for the complete button.
    /// </summary>
    [Parameter]
    public string CompleteButtonText { get; set; } = "Complete";

    /// <summary>
    /// Whether the wizard can be completed.
    /// </summary>
    [Parameter]
    public bool CanComplete { get; set; } = true;

    /// <summary>
    /// Callback when the wizard is completed.
    /// </summary>
    [Parameter]
    public EventCallback OnComplete { get; set; }

    /// <summary>
    /// Callback to validate a step before allowing progression.
    /// Returns true if the step is valid.
    /// </summary>
    [Parameter]
    public Func<int, Task<bool>>? ValidateStep { get; set; }

    private async Task<bool> ValidateStepAsync(StepChangeDirection direction, int targetIndex)
    {
        if (direction == StepChangeDirection.Backward)
        {
            return true;
        }

        if (ValidateStep != null)
        {
            return await ValidateStep(ActiveStepIndex);
        }

        return true;
    }

    private Task GoToPreviousStep()
    {
        if (ActiveStepIndex > 0)
        {
            ActiveStepIndex--;
        }
        return Task.CompletedTask;
    }

    private async Task GoToNextStep()
    {
        if (ValidateStep != null && !await ValidateStep(ActiveStepIndex))
        {
            return;
        }

        if (ActiveStepIndex < Steps.Count - 1)
        {
            ActiveStepIndex++;
        }
    }

    private void SkipStep()
    {
        if (ActiveStepIndex < Steps.Count - 1 && CanSkipCurrentStep())
        {
            ActiveStepIndex++;
        }
    }

    private bool CanSkipCurrentStep()
    {
        return Steps.Count > ActiveStepIndex && Steps[ActiveStepIndex].IsOptional;
    }

    private async Task Complete()
    {
        if (OnComplete.HasDelegate)
        {
            await OnComplete.InvokeAsync();
        }
    }
}

/// <summary>
/// Represents a step in the wizard.
/// </summary>
public class WizardStep
{
    /// <summary>
    /// The title of the step.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional icon for the step.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The content to render for this step.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// Whether this step can be skipped.
    /// </summary>
    public bool IsOptional { get; set; }
}

/// <summary>
/// Direction of step change in the wizard.
/// </summary>
public enum StepChangeDirection
{
    Forward,
    Backward
}
