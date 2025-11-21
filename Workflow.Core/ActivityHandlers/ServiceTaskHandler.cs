using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.ActivityHandlers;

/// <summary>
/// Handler for service task activities - executes automated service calls
/// </summary>
public class ServiceTaskHandler : IActivityHandler
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceTaskHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ActivityType SupportedType => ActivityType.ServiceTask;

    public async Task<ActivityExecutionResult> ExecuteAsync(
        ActivityInstance activityInstance,
        ActivityDefinition activityDefinition,
        WorkflowContext workflowContext)
    {
        try
        {
            // Get service name from configuration
            if (!activityDefinition.Configuration.TryGetValue("serviceName", out var serviceNameObj))
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Service name not configured"
                };
            }

            var serviceName = serviceNameObj?.ToString();
            if (string.IsNullOrEmpty(serviceName))
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Service name is empty"
                };
            }

            // Get method name from configuration
            if (!activityDefinition.Configuration.TryGetValue("methodName", out var methodNameObj))
            {
                return new ActivityExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Method name not configured"
                };
            }

            var methodName = methodNameObj?.ToString();

            // In a real implementation, you would:
            // 1. Resolve the service from _serviceProvider by name
            // 2. Invoke the specified method with activityInstance.Input
            // 3. Return the result

            // For now, return a mock success
            return new ActivityExecutionResult
            {
                Success = true,
                Output = new Dictionary<string, object?>
                {
                    ["serviceResult"] = $"Executed {serviceName}.{methodName}"
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
