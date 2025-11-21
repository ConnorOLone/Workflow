using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.ActivityHandlers;

/// <summary>
/// Handler for decision/gateway activities - evaluates conditions to route workflow
/// </summary>
public class DecisionHandler : IActivityHandler
{
    public ActivityType SupportedType => ActivityType.Decision;

    public Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        try
        {
            // Decision nodes are handled by transition conditions
            // This handler just marks the decision as passed through

            // Optionally evaluate a decision expression and store result
            if (activityDefinition.Configuration.TryGetValue("decisionExpression", out var exprObj))
            {
                var expression = exprObj?.ToString();

                // In a real implementation, evaluate the expression
                // For now, just pass through
            }

            return Task.FromResult(new ActivityExecutionResult
            {
                Success = true,
                Output = new Dictionary<string, object?>
                {
                    ["decision"] = "evaluated"
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ActivityExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
