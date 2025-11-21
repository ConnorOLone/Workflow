namespace Workflow.Core.Models;

/// <summary>
/// Represents a single activity/node in a workflow definition
/// </summary>
public class ActivityDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of activity (HumanTask, ServiceTask, ScriptTask, Decision, etc.)
    /// </summary>
    public ActivityType Type { get; set; }

    /// <summary>
    /// Activity-specific configuration
    /// </summary>
    public Dictionary<string, object?> Configuration { get; set; } = new();

    /// <summary>
    /// Input mappings - how to map workflow variables to activity inputs
    /// </summary>
    public Dictionary<string, string> InputMappings { get; set; } = new();

    /// <summary>
    /// Output mappings - how to map activity outputs back to workflow variables
    /// </summary>
    public Dictionary<string, string> OutputMappings { get; set; } = new();

    /// <summary>
    /// Timeout for this activity in seconds (null = no timeout)
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Whether this activity can be retried on failure
    /// </summary>
    public bool AllowRetry { get; set; } = false;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Visual positioning in workflow designer
    /// </summary>
    public Position? Position { get; set; }
}

public enum ActivityType
{
    Start,
    End,
    HumanTask,
    ServiceTask,
    ScriptTask,
    Decision,
    ParallelGateway,
    ExclusiveGateway,
    SubWorkflow,
    Timer,
    EventListener
}

public class Position
{
    public int X { get; set; }
    public int Y { get; set; }
}
