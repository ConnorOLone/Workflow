namespace Workflow.Core.Models;

/// <summary>
/// Represents a running instance of a workflow
/// </summary>
public class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the workflow definition
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Current state of the workflow
    /// </summary>
    public WorkflowState State { get; set; } = WorkflowState.NotStarted;

    /// <summary>
    /// When the workflow instance was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the workflow was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the workflow completed or failed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current runtime variables and their values
    /// </summary>
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Currently active activity instances
    /// </summary>
    public List<ActivityInstance> ActiveActivities { get; set; } = new();

    /// <summary>
    /// History of all activity executions
    /// </summary>
    public List<ActivityInstance> History { get; set; } = new();

    /// <summary>
    /// User or system that initiated this workflow
    /// </summary>
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Optional business key for correlation
    /// </summary>
    public string? BusinessKey { get; set; }

    /// <summary>
    /// Error information if workflow failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Parent workflow instance ID if this is a sub-workflow
    /// </summary>
    public Guid? ParentWorkflowInstanceId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum WorkflowState
{
    NotStarted,
    Running,
    Suspended,
    Completed,
    Failed,
    Cancelled
}
