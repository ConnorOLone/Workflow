using Workflow.Core.Models;

namespace Workflow.Core.Persistence.Entities;

/// <summary>
/// EF Core entity for ActivityInstance
/// </summary>
public class ActivityInstanceEntity
{
    public Guid Id { get; set; }
    public Guid ActivityDefinitionId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public ActivityState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedToGroup { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// JSON serialized input data
    /// </summary>
    public string InputJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized output data
    /// </summary>
    public string OutputJson { get; set; } = string.Empty;

    // Navigation properties
    public WorkflowInstanceEntity WorkflowInstance { get; set; } = null!;
}
