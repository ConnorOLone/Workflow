using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Persistence;

/// <summary>
/// In-memory implementation of workflow repository for testing and development
/// </summary>
public class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly Dictionary<Guid, WorkflowDefinition> _definitions = new();
    private readonly Dictionary<Guid, WorkflowInstance> _instances = new();
    private readonly Dictionary<Guid, ActivityInstance> _activities = new();
    private readonly object _lock = new();

    public Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_definitions.TryGetValue(id, out var def) ? def : null);
        }
    }

    public Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<WorkflowDefinition>>(_definitions.Values.ToList());
        }
    }

    public Task SaveWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        lock (_lock)
        {
            _definitions[definition.Id] = definition;
            return Task.CompletedTask;
        }
    }

    public Task DeleteWorkflowDefinitionAsync(Guid id)
    {
        lock (_lock)
        {
            _definitions.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_instances.TryGetValue(id, out var instance) ? instance : null);
        }
    }

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesByDefinitionAsync(Guid definitionId)
    {
        lock (_lock)
        {
            var instances = _instances.Values
                .Where(i => i.WorkflowDefinitionId == definitionId)
                .ToList();
            return Task.FromResult<IEnumerable<WorkflowInstance>>(instances);
        }
    }

    public Task<IEnumerable<WorkflowInstance>> GetActiveWorkflowInstancesAsync()
    {
        lock (_lock)
        {
            var instances = _instances.Values
                .Where(i => i.State == WorkflowState.Running || i.State == WorkflowState.Suspended)
                .ToList();
            return Task.FromResult<IEnumerable<WorkflowInstance>>(instances);
        }
    }

    public Task SaveWorkflowInstanceAsync(WorkflowInstance instance)
    {
        lock (_lock)
        {
            _instances[instance.Id] = instance;
            return Task.CompletedTask;
        }
    }

    public Task DeleteWorkflowInstanceAsync(Guid id)
    {
        lock (_lock)
        {
            _instances.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<ActivityInstance?> GetActivityInstanceAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_activities.TryGetValue(id, out var activity) ? activity : null);
        }
    }

    public Task<IEnumerable<ActivityInstance>> GetActiveActivitiesAsync(Guid workflowInstanceId)
    {
        lock (_lock)
        {
            var activities = _activities.Values
                .Where(a => a.WorkflowInstanceId == workflowInstanceId &&
                           (a.State == ActivityState.Running || a.State == ActivityState.WaitingForInput))
                .ToList();
            return Task.FromResult<IEnumerable<ActivityInstance>>(activities);
        }
    }

    public Task SaveActivityInstanceAsync(ActivityInstance activityInstance)
    {
        lock (_lock)
        {
            _activities[activityInstance.Id] = activityInstance;
            return Task.CompletedTask;
        }
    }
}
