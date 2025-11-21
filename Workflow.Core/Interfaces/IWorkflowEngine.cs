using Workflow.Core.Models;

namespace Workflow.Core.Interfaces;

/// <summary>
/// Core workflow execution engine interface
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Start a new workflow instance
    /// </summary>
    Task<WorkflowInstance> StartWorkflowAsync(Guid workflowDefinitionId, Dictionary<string, object?>? initialVariables = null, string? initiatedBy = null);

    /// <summary>
    /// Resume a suspended or waiting workflow
    /// </summary>
    Task ResumeWorkflowAsync(Guid workflowInstanceId);

    /// <summary>
    /// Suspend a running workflow
    /// </summary>
    Task SuspendWorkflowAsync(Guid workflowInstanceId);

    /// <summary>
    /// Cancel a workflow instance
    /// </summary>
    Task CancelWorkflowAsync(Guid workflowInstanceId, string? reason = null);

    /// <summary>
    /// Complete a human task activity
    /// </summary>
    Task CompleteActivityAsync(Guid activityInstanceId, Dictionary<string, object?>? output = null, string? completedBy = null);

    /// <summary>
    /// Get current state of a workflow instance
    /// </summary>
    Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid workflowInstanceId);

    /// <summary>
    /// Get all active workflow instances for a definition
    /// </summary>
    Task<IEnumerable<WorkflowInstance>> GetActiveWorkflowsAsync(Guid workflowDefinitionId);
}
