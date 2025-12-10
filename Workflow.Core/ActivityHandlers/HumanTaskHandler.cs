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
        // How do i handle human's as objects in the system.
        // Can use AD but this is Windows only
        // Do not want to create separate users for workflow
        // Extract assignment information from configuration
        // 
        if (activityDefinition.Configuration.TryGetValue("assignedTo", out var assignedTo))
        {
            activityInstance.AssignedTo = (User?)assignedTo;
        }

        if (activityDefinition.Configuration.TryGetValue("assignedToGroup", out var assignedToGroup))
        {
            activityInstance.AssignedToGroup = (User?)assignedToGroup;
        }

        // Human tasks require external completion
        return Task.FromResult(new ActivityExecutionResult
        {
            Success = true,
            RequiresHumanInput = true
        });
    }
}
