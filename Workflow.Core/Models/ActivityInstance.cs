namespace Workflow.Core.Models;

/// <summary>
/// Represents a runtime instance of an activity execution
/// </summary>
public class ActivityInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the activity definition
    /// </summary>
    public Guid ActivityDefinitionId { get; set; }

    /// <summary>
    /// Reference to the workflow instance
    /// </summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// Current state of this activity
    /// </summary>
    public ActivityState State { get; set; } = ActivityState.Ready;

    /// <summary>
    /// When the activity was created/queued
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the activity started executing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the activity completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Input data for this activity execution
    /// </summary>
    public Dictionary<string, object?> Input { get; set; } = new();

    /// <summary>
    /// Output data from this activity execution
    /// </summary>
    public Dictionary<string, object?> Output { get; set; } = new();

    /// <summary>
    /// User assigned to this activity (for human tasks)
    /// </summary>
    public Entity? AssignedTo { get; set; }

    /// <summary>
    /// Group/role assigned to this activity
    /// </summary>
    public Entity? AssignedToGroup { get; set; }    // Need to create - Group.cs 

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message if activity failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if activity failed
    /// </summary>
    public string? ErrorStackTrace { get; set; }
}

public enum ActivityState
{
    Ready,
    Running,
    WaitingForInput,
    Completed,
    Failed,
    Cancelled,
    Skipped
}
