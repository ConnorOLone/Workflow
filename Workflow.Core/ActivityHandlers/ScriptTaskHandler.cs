using Workflow.Core.Interfaces;
using Workflow.Core.Models;
using Workflow.Core.Scripting;

namespace Workflow.Core.ActivityHandlers;

/// <summary>
/// Handler for script task activities - executes inline scripts in various languages
/// </summary>
public class ScriptTaskHandler : IActivityHandler
{
    private readonly IScriptExecutorFactory _scriptExecutorFactory;

    public ScriptTaskHandler(IScriptExecutorFactory scriptExecutorFactory)
    {
        _scriptExecutorFactory = scriptExecutorFactory ?? throw new ArgumentNullException(nameof(scriptExecutorFactory));
    }

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

            // Get script language (default to csharp)
            var language = activityDefinition.Configuration.TryGetValue("language", out var langObj)
                ? langObj?.ToString() ?? "csharp"
                : "csharp";

            // Get script executor for the specified language
            IScriptExecutor executor;
            try
            {
                executor = _scriptExecutorFactory.GetExecutor(language);
            }
            catch (NotSupportedException ex)
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }

            // Prepare variables for script (merge workflow variables and activity input)
            var scriptVariables = new Dictionary<string, object?>(workflowContext.Variables);
            foreach (var (key, value) in activityInstance.Input)
            {
                scriptVariables[key] = value;
            }

            // Get script options from configuration
            var scriptOptions = new ScriptOptions();

            // Get timeout from configuration or activity definition
            if (activityDefinition.Configuration.TryGetValue("timeoutSeconds", out var timeoutObj)
                && int.TryParse(timeoutObj?.ToString(), out var timeout))
            {
                scriptOptions.TimeoutSeconds = timeout;
            }
            else if (activityDefinition.TimeoutSeconds.HasValue)
            {
                scriptOptions.TimeoutSeconds = activityDefinition.TimeoutSeconds.Value;
            }

            // Get security permissions
            if (activityDefinition.Configuration.TryGetValue("allowFileSystem", out var allowFsObj)
                && bool.TryParse(allowFsObj?.ToString(), out var allowFs))
            {
                scriptOptions.AllowFileSystemAccess = allowFs;
            }

            if (activityDefinition.Configuration.TryGetValue("allowNetwork", out var allowNetObj)
                && bool.TryParse(allowNetObj?.ToString(), out var allowNet))
            {
                scriptOptions.AllowNetworkAccess = allowNet;
            }

            // Get allowed namespaces (for C# scripts)
            if (activityDefinition.Configuration.TryGetValue("allowedNamespaces", out var namespacesObj))
            {
                if (namespacesObj is string namespacesStr)
                {
                    scriptOptions.AllowedNamespaces = namespacesStr
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(ns => ns.Trim())
                        .ToList();
                }
                else if (namespacesObj is IEnumerable<string> namespacesList)
                {
                    scriptOptions.AllowedNamespaces = namespacesList.ToList();
                }
            }

            // Get allowed assemblies (for C# scripts)
            if (activityDefinition.Configuration.TryGetValue("allowedAssemblies", out var assembliesObj))
            {
                if (assembliesObj is string assembliesStr)
                {
                    scriptOptions.AllowedAssemblies = assembliesStr
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(asm => asm.Trim())
                        .ToList();
                }
                else if (assembliesObj is IEnumerable<string> assembliesList)
                {
                    scriptOptions.AllowedAssemblies = assembliesList.ToList();
                }
            }

            // Execute script
            var result = await executor.ExecuteAsync(script, scriptVariables, scriptOptions);

            if (!result.Success)
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
            }

            // Prepare output
            var output = new Dictionary<string, object?>
            {
                ["scriptResult"] = result.ReturnValue,
                ["executionTime"] = result.ExecutionTime.TotalMilliseconds,
                ["language"] = language
            };

            // Add modified variables to output
            foreach (var (key, value) in result.ModifiedVariables)
            {
                output[$"modified_{key}"] = value;

                // Also update the workflow context variables
                workflowContext.Variables[key] = value;
            }

            return new ActivityExecutionResult
            {
                Success = true,
                Output = output
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
