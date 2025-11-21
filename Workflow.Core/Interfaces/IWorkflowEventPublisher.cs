using Workflow.Core.Models;

namespace Workflow.Core.Interfaces;

/// <summary>
/// Interface for publishing workflow events
/// </summary>
public interface IWorkflowEventPublisher
{
    Task PublishWorkflowStartedAsync(WorkflowInstance instance);
    Task PublishWorkflowCompletedAsync(WorkflowInstance instance);
    Task PublishWorkflowFailedAsync(WorkflowInstance instance);
    Task PublishWorkflowSuspendedAsync(WorkflowInstance instance);
    Task PublishWorkflowResumedAsync(WorkflowInstance instance);
    Task PublishWorkflowCancelledAsync(WorkflowInstance instance, string? reason);

    Task PublishActivityStartedAsync(ActivityInstance activityInstance);
    Task PublishActivityCompletedAsync(ActivityInstance activityInstance);
    Task PublishActivityFailedAsync(ActivityInstance activityInstance);
    Task PublishActivityWaitingAsync(ActivityInstance activityInstance);
    Task PublishActivityRetryingAsync(ActivityInstance activityInstance);
}
