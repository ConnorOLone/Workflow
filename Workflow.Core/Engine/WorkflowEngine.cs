using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Engine;

/// <summary>
/// Core workflow execution engine implementation
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowRepository _repository;
    private readonly IActivityHandlerFactory _activityHandlerFactory;
    private readonly IWorkflowEventPublisher _eventPublisher;

    public WorkflowEngine(
        IWorkflowRepository repository,
        IActivityHandlerFactory activityHandlerFactory,
        IWorkflowEventPublisher eventPublisher)
    {
        _repository = repository;
        _activityHandlerFactory = activityHandlerFactory;
        _eventPublisher = eventPublisher;
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Dictionary<string, object?>? initialVariables = null,
        string? initiatedBy = null)
    {
        var definition = await _repository.GetWorkflowDefinitionAsync(workflowDefinitionId)
            ?? throw new InvalidOperationException($"Workflow definition {workflowDefinitionId} not found");

        if (!definition.IsActive)
        {
            throw new InvalidOperationException($"Workflow definition {workflowDefinitionId} is not active");
        }

        // Create new workflow instance
        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = workflowDefinitionId,
            State = WorkflowState.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy,
            Variables = new Dictionary<string, object?>(definition.Variables)
        };

        // Merge initial variables
        if (initialVariables != null)
        {
            foreach (var kvp in initialVariables)
            {
                instance.Variables[kvp.Key] = kvp.Value;
            }
        }

        await _repository.SaveWorkflowInstanceAsync(instance);
        await _eventPublisher.PublishWorkflowStartedAsync(instance);

        // Start execution from the start activity
        await ExecuteFromActivityAsync(instance, definition, definition.StartActivityId);

        return instance;
    }

    public async Task ResumeWorkflowAsync(Guid workflowInstanceId)
    {
        var instance = await _repository.GetWorkflowInstanceAsync(workflowInstanceId)
            ?? throw new InvalidOperationException($"Workflow instance {workflowInstanceId} not found");

        if (instance.State != WorkflowState.Suspended)
        {
            throw new InvalidOperationException($"Workflow instance {workflowInstanceId} is not suspended");
        }

        instance.State = WorkflowState.Running;
        await _repository.SaveWorkflowInstanceAsync(instance);
        await _eventPublisher.PublishWorkflowResumedAsync(instance);

        var definition = await _repository.GetWorkflowDefinitionAsync(instance.WorkflowDefinitionId)
            ?? throw new InvalidOperationException($"Workflow definition not found");

        // Continue execution from active activities
        foreach (var activeActivity in instance.ActiveActivities.ToList())
        {
            await ContinueActivityExecutionAsync(instance, definition, activeActivity);
        }
    }

    public async Task SuspendWorkflowAsync(Guid workflowInstanceId)
    {
        var instance = await _repository.GetWorkflowInstanceAsync(workflowInstanceId)
            ?? throw new InvalidOperationException($"Workflow instance {workflowInstanceId} not found");

        instance.State = WorkflowState.Suspended;
        await _repository.SaveWorkflowInstanceAsync(instance);
        await _eventPublisher.PublishWorkflowSuspendedAsync(instance);
    }

    public async Task CancelWorkflowAsync(Guid workflowInstanceId, string? reason = null)
    {
        var instance = await _repository.GetWorkflowInstanceAsync(workflowInstanceId)
            ?? throw new InvalidOperationException($"Workflow instance {workflowInstanceId} not found");

        instance.State = WorkflowState.Cancelled;
        instance.CompletedAt = DateTime.UtcNow;
        instance.ErrorMessage = reason;

        // Cancel all active activities
        foreach (var activity in instance.ActiveActivities)
        {
            activity.State = ActivityState.Cancelled;
            activity.CompletedAt = DateTime.UtcNow;
        }

        await _repository.SaveWorkflowInstanceAsync(instance);
        await _eventPublisher.PublishWorkflowCancelledAsync(instance, reason);
    }

    public async Task CompleteActivityAsync(
        Guid activityInstanceId,
        Dictionary<string, object?>? output = null,
        string? completedBy = null)
    {
        var activityInstance = await _repository.GetActivityInstanceAsync(activityInstanceId)
            ?? throw new InvalidOperationException($"Activity instance {activityInstanceId} not found");

        var instance = await _repository.GetWorkflowInstanceAsync(activityInstance.WorkflowInstanceId)
            ?? throw new InvalidOperationException($"Workflow instance not found");

        var definition = await _repository.GetWorkflowDefinitionAsync(instance.WorkflowDefinitionId)
            ?? throw new InvalidOperationException($"Workflow definition not found");

        // Update activity instance
        activityInstance.State = ActivityState.Completed;
        activityInstance.CompletedAt = DateTime.UtcNow;
        activityInstance.Output = output ?? new Dictionary<string, object?>();

        await _repository.SaveActivityInstanceAsync(activityInstance);
        await _eventPublisher.PublishActivityCompletedAsync(activityInstance);

        // Remove from active activities and add to history
        instance.ActiveActivities.Remove(activityInstance);
        instance.History.Add(activityInstance);

        // Apply output mappings
        var activityDef = definition.Activities.FirstOrDefault(a => a.Id == activityInstance.ActivityDefinitionId);
        if (activityDef != null)
        {
            ApplyOutputMappings(instance, activityInstance, activityDef);
        }

        await _repository.SaveWorkflowInstanceAsync(instance);

        // Determine next activities
        await DetermineAndExecuteNextActivitiesAsync(instance, definition, activityInstance.ActivityDefinitionId);
    }

    public async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid workflowInstanceId)
    {
        return await _repository.GetWorkflowInstanceAsync(workflowInstanceId);
    }

    public async Task<IEnumerable<WorkflowInstance>> GetActiveWorkflowsAsync(Guid workflowDefinitionId)
    {
        var instances = await _repository.GetWorkflowInstancesByDefinitionAsync(workflowDefinitionId);
        return instances.Where(i => i.State == WorkflowState.Running || i.State == WorkflowState.Suspended);
    }

    private async Task ExecuteFromActivityAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        Guid activityDefinitionId)
    {
        var activityDef = definition.Activities.FirstOrDefault(a => a.Id == activityDefinitionId)
            ?? throw new InvalidOperationException($"Activity definition {activityDefinitionId} not found");

        // Create activity instance
        var activityInstance = new ActivityInstance
        {
            ActivityDefinitionId = activityDefinitionId,
            WorkflowInstanceId = instance.Id,
            State = ActivityState.Ready
        };

        // Apply input mappings
        ApplyInputMappings(instance, activityInstance, activityDef);

        instance.ActiveActivities.Add(activityInstance);
        await _repository.SaveActivityInstanceAsync(activityInstance);
        await _eventPublisher.PublishActivityStartedAsync(activityInstance);

        // Execute the activity
        await ContinueActivityExecutionAsync(instance, definition, activityInstance);
    }

    private async Task ContinueActivityExecutionAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        ActivityInstance activityInstance)
    {
        var activityDef = definition.Activities.FirstOrDefault(a => a.Id == activityInstance.ActivityDefinitionId);
        if (activityDef == null) return;

        activityInstance.State = ActivityState.Running;
        activityInstance.StartedAt = DateTime.UtcNow;
        await _repository.SaveActivityInstanceAsync(activityInstance);

        try
        {
            var handler = _activityHandlerFactory.GetHandler(activityDef.Type);
            var context = new WorkflowContext
            {
                WorkflowInstanceId = instance.Id,
                Variables = instance.Variables,
                InitiatedBy = instance.InitiatedBy
            };

            var result = await handler.ExecuteAsync(activityInstance, activityDef, context);

            if (result.RequiresHumanInput)
            {
                // Activity is waiting for human input
                activityInstance.State = ActivityState.WaitingForInput;
                await _repository.SaveActivityInstanceAsync(activityInstance);
                await _eventPublisher.PublishActivityWaitingAsync(activityInstance);
            }
            else if (result.Success)
            {
                // Activity completed successfully
                await CompleteActivityAsync(activityInstance.Id, result.Output);
            }
            else
            {
                // Activity failed
                await HandleActivityFailureAsync(instance, definition, activityInstance, activityDef, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await HandleActivityFailureAsync(instance, definition, activityInstance, activityDef, ex.Message);
        }
    }

    private async Task HandleActivityFailureAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        ActivityInstance activityInstance,
        ActivityDefinition activityDef,
        string? errorMessage)
    {
        activityInstance.ErrorMessage = errorMessage;
        activityInstance.RetryCount++;

        if (activityDef.AllowRetry && activityInstance.RetryCount < activityDef.MaxRetries)
        {
            // Retry the activity
            activityInstance.State = ActivityState.Ready;
            await _repository.SaveActivityInstanceAsync(activityInstance);
            await _eventPublisher.PublishActivityRetryingAsync(activityInstance);
            await ContinueActivityExecutionAsync(instance, definition, activityInstance);
        }
        else
        {
            // Activity failed permanently
            activityInstance.State = ActivityState.Failed;
            activityInstance.CompletedAt = DateTime.UtcNow;
            await _repository.SaveActivityInstanceAsync(activityInstance);
            await _eventPublisher.PublishActivityFailedAsync(activityInstance);

            // Fail the entire workflow
            instance.State = WorkflowState.Failed;
            instance.CompletedAt = DateTime.UtcNow;
            instance.ErrorMessage = $"Activity {activityDef.Name} failed: {errorMessage}";
            await _repository.SaveWorkflowInstanceAsync(instance);
            await _eventPublisher.PublishWorkflowFailedAsync(instance);
        }
    }

    private async Task DetermineAndExecuteNextActivitiesAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        Guid completedActivityId)
    {
        // Find all transitions from the completed activity
        var transitions = definition.Transitions
            .Where(t => t.FromActivityId == completedActivityId)
            .OrderByDescending(t => t.Priority)
            .ToList();

        if (!transitions.Any())
        {
            // No more transitions - workflow completed
            await CompleteWorkflowAsync(instance);
            return;
        }

        // Evaluate transition conditions
        var validTransitions = new List<Transition>();
        foreach (var transition in transitions)
        {
            if (string.IsNullOrEmpty(transition.Condition) || transition.IsDefault)
            {
                validTransitions.Add(transition);
            }
            else
            {
                // Evaluate condition (simplified - you'd use a proper expression evaluator)
                if (EvaluateCondition(transition.Condition, instance.Variables))
                {
                    validTransitions.Add(transition);
                }
            }
        }

        // Take the first valid transition (or default)
        var nextTransition = validTransitions.FirstOrDefault()
            ?? transitions.FirstOrDefault(t => t.IsDefault);

        if (nextTransition != null)
        {
            await ExecuteFromActivityAsync(instance, definition, nextTransition.ToActivityId);
        }
        else
        {
            // No valid transition found
            await CompleteWorkflowAsync(instance);
        }
    }

    private async Task CompleteWorkflowAsync(WorkflowInstance instance)
    {
        instance.State = WorkflowState.Completed;
        instance.CompletedAt = DateTime.UtcNow;
        await _repository.SaveWorkflowInstanceAsync(instance);
        await _eventPublisher.PublishWorkflowCompletedAsync(instance);
    }

    private void ApplyInputMappings(
        WorkflowInstance instance,
        ActivityInstance activityInstance,
        ActivityDefinition activityDef)
    {
        foreach (var mapping in activityDef.InputMappings)
        {
            if (instance.Variables.TryGetValue(mapping.Value, out var value))
            {
                activityInstance.Input[mapping.Key] = value;
            }
        }
    }

    private void ApplyOutputMappings(
        WorkflowInstance instance,
        ActivityInstance activityInstance,
        ActivityDefinition activityDef)
    {
        foreach (var mapping in activityDef.OutputMappings)
        {
            if (activityInstance.Output.TryGetValue(mapping.Key, out var value))
            {
                instance.Variables[mapping.Value] = value;
            }
        }
    }

    private bool EvaluateCondition(string condition, Dictionary<string, object?> variables)
    {
        // Simplified condition evaluation
        // In production, use a proper expression evaluator like NCalc or DynamicExpresso
        // For now, just return true
        return true;
    }
}
