namespace Workflow.Core.Models;

/// <summary>
/// Represents a transition/edge between activities in a workflow
/// </summary>
public class Transition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Source activity ID
    /// </summary>
    public Guid FromActivityId { get; set; }

    /// <summary>
    /// Target activity ID
    /// </summary>
    public Guid ToActivityId { get; set; }

    /// <summary>
    /// Condition that must be met for this transition to be taken
    /// Expression evaluated against workflow context (e.g., "amount > 1000")
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Priority when multiple transitions are valid (higher = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this is the default transition when no conditions match
    /// </summary>
    public bool IsDefault { get; set; } = false;
}
