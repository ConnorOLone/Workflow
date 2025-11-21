using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.ActivityHandlers;

/// <summary>
/// Handler for script task activities - executes inline scripts
/// </summary>
public class ScriptTaskHandler : IActivityHandler
{
    public ActivityType SupportedType => ActivityType.ScriptTask;

    public async Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        try
        {
            // Get script from configuration
            if (!activityDefinition.Configuration.TryGetValue("script", out var scriptObj))
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Script not configured"
                };
            }

            var script = scriptObj?.ToString();
            if (string.IsNullOrEmpty(script))
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Script is empty"
                };
            }

            // Get script language (C#, JavaScript, Python, etc.)
            var language = activityDefinition.Configuration.TryGetValue("language", out var langObj)
                ? langObj?.ToString() ?? "csharp"
                : "csharp";

            // In a real implementation, you would:
            // 1. Use a scripting engine (Roslyn for C#, Jint for JavaScript, etc.)
            // 2. Execute the script with access to workflowContext and activityInstance.Input
            // 3. Return the script output

            // For now, return a mock success
            return new ActivityExecutionResult
            {
                Success = true,
                Output = new Dictionary<string, object?>
                {
                    ["scriptResult"] = $"Executed {language} script"
                }
            };
        }
        catch (Exception ex)
        {
            return new ActivityExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
