using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Engine;

/// <summary>
/// Handler for start activities
/// </summary>
public class StartActivityHandler : IActivityHandler
{
    public ActivityType SupportedType => ActivityType.Start;

    public Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        // Start activity just passes through
        return Task.FromResult(new ActivityExecutionResult
        {
            Success = true
        });
    }
}
