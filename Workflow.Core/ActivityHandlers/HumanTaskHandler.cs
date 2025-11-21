using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.ActivityHandlers;

/// <summary>
/// Handler for human task activities - requires manual completion
/// </summary>
public class HumanTaskHandler : IActivityHandler
{
    public ActivityType SupportedType => ActivityType.HumanTask;

    public Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        // Extract assignment information from configuration
        if (activityDefinition.Configuration.TryGetValue("assignedTo", out var assignedTo))
        {
            activityInstance.AssignedTo = assignedTo?.ToString();
        }

        if (activityDefinition.Configuration.TryGetValue("assignedToGroup", out var assignedToGroup))
        {
            activityInstance.AssignedToGroup = assignedToGroup?.ToString();
        }

        // Human tasks require external completion
        return Task.FromResult(new ActivityExecutionResult
        {
            Success = true,
            RequiresHumanInput = true
        });
    }
}
