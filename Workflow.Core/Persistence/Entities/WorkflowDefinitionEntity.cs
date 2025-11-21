namespace Workflow.Core.Persistence.Entities;

/// <summary>
/// EF Core entity for WorkflowDefinition
/// </summary>
public class WorkflowDefinitionEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid StartActivityId { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// JSON serialized activities
    /// </summary>
    public string ActivitiesJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized transitions
    /// </summary>
    public string TransitionsJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized variables
    /// </summary>
    public string VariablesJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized metadata
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<WorkflowInstanceEntity> Instances { get; set; } = new List<WorkflowInstanceEntity>();
}
