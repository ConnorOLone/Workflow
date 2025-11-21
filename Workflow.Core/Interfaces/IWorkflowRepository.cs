using Workflow.Core.Models;

namespace Workflow.Core.Interfaces;

/// <summary>
/// Repository interface for workflow persistence
/// </summary>
public interface IWorkflowRepository
{
    // Workflow Definitions
    Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(Guid id);
    Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
    Task SaveWorkflowDefinitionAsync(WorkflowDefinition definition);
    Task DeleteWorkflowDefinitionAsync(Guid id);

    // Workflow Instances
    Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid id);
    Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesByDefinitionAsync(Guid definitionId);
    Task<IEnumerable<WorkflowInstance>> GetActiveWorkflowInstancesAsync();
    Task SaveWorkflowInstanceAsync(WorkflowInstance instance);
    Task DeleteWorkflowInstanceAsync(Guid id);

    // Activity Instances
    Task<ActivityInstance?> GetActivityInstanceAsync(Guid id);
    Task<IEnumerable<ActivityInstance>> GetActiveActivitiesAsync(Guid workflowInstanceId);
    Task SaveActivityInstanceAsync(ActivityInstance activityInstance);
}
