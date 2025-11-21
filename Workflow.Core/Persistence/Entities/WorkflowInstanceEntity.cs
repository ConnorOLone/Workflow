using Workflow.Core.Models;

namespace Workflow.Core.Persistence.Entities;

/// <summary>
/// EF Core entity for WorkflowInstance
/// </summary>
public class WorkflowInstanceEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? InitiatedBy { get; set; }
    public string? BusinessKey { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ParentWorkflowInstanceId { get; set; }

    /// <summary>
    /// JSON serialized variables
    /// </summary>
    public string VariablesJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized metadata
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;

    // Navigation properties
    public WorkflowDefinitionEntity WorkflowDefinition { get; set; } = null!;
    public ICollection<ActivityInstanceEntity> ActivityInstances { get; set; } = new List<ActivityInstanceEntity>();
    public WorkflowInstanceEntity? ParentWorkflowInstance { get; set; }
    public ICollection<WorkflowInstanceEntity> ChildWorkflowInstances { get; set; } = new List<WorkflowInstanceEntity>();
}
