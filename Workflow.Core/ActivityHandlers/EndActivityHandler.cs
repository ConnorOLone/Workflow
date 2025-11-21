using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Engine;

/// <summary>
/// Handler for end activities
/// </summary>
public class EndActivityHandler : IActivityHandler
{
    public ActivityType SupportedType => ActivityType.End;

    public Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        // End activity just completes
        return Task.FromResult(new ActivityExecutionResult
        {
            Success = true
        });
    }
}
