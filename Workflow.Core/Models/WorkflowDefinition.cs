namespace Workflow.Core.Models;

/// <summary>
/// Represents a workflow process definition (template/blueprint)
/// </summary>
public class WorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The starting activity of the workflow
    /// </summary>
    public Guid StartActivityId { get; set; }

    /// <summary>
    /// All activities in this workflow definition
    /// </summary>
    public List<ActivityDefinition> Activities { get; set; } = new();

    /// <summary>
    /// Transitions between activities
    /// </summary>
    public List<Transition> Transitions { get; set; } = new();

    /// <summary>
    /// Global workflow variables and their default values
    /// </summary>
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Workflow metadata and configuration
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
