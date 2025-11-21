using Workflow.Core.Models;

namespace Workflow.Core.Interfaces;

/// <summary>
/// Interface for handling specific activity type execution
/// </summary>
public interface IActivityHandler
{
    /// <summary>
    /// The activity type this handler supports
    /// </summary>
    ActivityType SupportedType { get; }

    /// <summary>
    /// Execute the activity
    /// </summary>
    /// <param name="activityInstance">The activity instance to execute</param>
    /// <param name="activityDefinition">The activity definition</param>
    /// <param name="workflowContext">The workflow context with variables</param>
    /// <returns>Activity execution result</returns>
    Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext);
}

/// <summary>
/// Result of an activity execution
/// </summary>
public class ActivityExecutionResult
{
    public bool Success { get; set; }
    public Dictionary<string, object?> Output { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool RequiresHumanInput { get; set; } = false;
}

/// <summary>
/// Context provided to activity handlers
/// </summary>
public class WorkflowContext
{
    public Guid WorkflowInstanceId { get; set; }
    public Dictionary<string, object?> Variables { get; set; } = new();
    public string? InitiatedBy { get; set; }
}
