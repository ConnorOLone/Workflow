using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Events;

/// <summary>
/// Default implementation of workflow event publisher
/// </summary>
public class WorkflowEventPublisher : IWorkflowEventPublisher
{
    private readonly List<IWorkflowEventHandler> _eventHandlers = new();

    public void RegisterEventHandler(IWorkflowEventHandler handler)
    {
        _eventHandlers.Add(handler);
    }

    public async Task PublishWorkflowStartedAsync(WorkflowInstance instance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowStarted,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishWorkflowCompletedAsync(WorkflowInstance instance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowCompleted,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishWorkflowFailedAsync(WorkflowInstance instance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowFailed,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object?> { ["errorMessage"] = instance.ErrorMessage }
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishWorkflowSuspendedAsync(WorkflowInstance instance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowSuspended,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishWorkflowResumedAsync(WorkflowInstance instance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowResumed,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishWorkflowCancelledAsync(WorkflowInstance instance, string? reason)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.WorkflowCancelled,
            WorkflowInstanceId = instance.Id,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object?> { ["reason"] = reason }
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishActivityStartedAsync(ActivityInstance activityInstance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.ActivityStarted,
            WorkflowInstanceId = activityInstance.WorkflowInstanceId,
            ActivityInstanceId = activityInstance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishActivityCompletedAsync(ActivityInstance activityInstance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.ActivityCompleted,
            WorkflowInstanceId = activityInstance.WorkflowInstanceId,
            ActivityInstanceId = activityInstance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishActivityFailedAsync(ActivityInstance activityInstance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.ActivityFailed,
            WorkflowInstanceId = activityInstance.WorkflowInstanceId,
            ActivityInstanceId = activityInstance.Id,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object?> { ["errorMessage"] = activityInstance.ErrorMessage }
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishActivityWaitingAsync(ActivityInstance activityInstance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.ActivityWaiting,
            WorkflowInstanceId = activityInstance.WorkflowInstanceId,
            ActivityInstanceId = activityInstance.Id,
            Timestamp = DateTime.UtcNow
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }

    public async Task PublishActivityRetryingAsync(ActivityInstance activityInstance)
    {
        var @event = new WorkflowEvent
        {
            EventType = WorkflowEventType.ActivityRetrying,
            WorkflowInstanceId = activityInstance.WorkflowInstanceId,
            ActivityInstanceId = activityInstance.Id,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object?> { ["retryCount"] = activityInstance.RetryCount }
        };

        foreach (var handler in _eventHandlers)
        {
            await handler.HandleEventAsync(@event);
        }
    }
}

public interface IWorkflowEventHandler
{
    Task HandleEventAsync(WorkflowEvent @event);
}

public class WorkflowEvent
{
    public WorkflowEventType EventType { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid? ActivityInstanceId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
}

public enum WorkflowEventType
{
    WorkflowStarted,
    WorkflowCompleted,
    WorkflowFailed,
    WorkflowSuspended,
    WorkflowResumed,
    WorkflowCancelled,
    ActivityStarted,
    ActivityCompleted,
    ActivityFailed,
    ActivityWaiting,
    ActivityRetrying
}
