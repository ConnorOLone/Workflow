using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Workflow.Core.Interfaces;
using Workflow.Core.Models;
using Workflow.Core.Persistence.Entities;

namespace Workflow.Core.Persistence;

/// <summary>
/// SQL Server implementation of workflow repository using Entity Framework Core
/// </summary>
public class SqlServerWorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlServerWorkflowRepository(WorkflowDbContext context)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    #region Workflow Definitions

    public async Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(Guid id)
    {
        var entity = await _context.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);

        return entity != null ? MapToWorkflowDefinition(entity) : null;
    }

    public async Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        var entities = await _context.WorkflowDefinitions
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(MapToWorkflowDefinition);
    }

    public async Task SaveWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        var entity = MapToWorkflowDefinitionEntity(definition);

        var existing = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == definition.Id);

        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(entity);
            existing.ActivitiesJson = entity.ActivitiesJson;
            existing.TransitionsJson = entity.TransitionsJson;
            existing.VariablesJson = entity.VariablesJson;
            existing.MetadataJson = entity.MetadataJson;
        }
        else
        {
            await _context.WorkflowDefinitions.AddAsync(entity);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteWorkflowDefinitionAsync(Guid id)
    {
        var entity = await _context.WorkflowDefinitions.FindAsync(id);
        if (entity != null)
        {
            _context.WorkflowDefinitions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Workflow Instances

    public async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid id)
    {
        var entity = await _context.WorkflowInstances
            .Include(w => w.ActivityInstances)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);

        return entity != null ? MapToWorkflowInstance(entity) : null;
    }

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesByDefinitionAsync(Guid definitionId)
    {
        var entities = await _context.WorkflowInstances
            .Include(w => w.ActivityInstances)
            .AsNoTracking()
            .Where(w => w.WorkflowDefinitionId == definitionId)
            .ToListAsync();

        return entities.Select(MapToWorkflowInstance);
    }

    public async Task<IEnumerable<WorkflowInstance>> GetActiveWorkflowInstancesAsync()
    {
        var entities = await _context.WorkflowInstances
            .Include(w => w.ActivityInstances)
            .AsNoTracking()
            .Where(w => w.State == WorkflowState.Running || w.State == WorkflowState.Suspended)
            .ToListAsync();

        return entities.Select(MapToWorkflowInstance);
    }

    public async Task SaveWorkflowInstanceAsync(WorkflowInstance instance)
    {
        var entity = MapToWorkflowInstanceEntity(instance);

        var existing = await _context.WorkflowInstances
            .Include(w => w.ActivityInstances)
            .FirstOrDefaultAsync(w => w.Id == instance.Id);

        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(entity);
            existing.VariablesJson = entity.VariablesJson;
            existing.MetadataJson = entity.MetadataJson;

            // Update activity instances separately
            foreach (var activity in instance.ActiveActivities.Concat(instance.History))
            {
                await SaveActivityInstanceAsync(activity);
            }
        }
        else
        {
            await _context.WorkflowInstances.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Save activity instances after workflow instance is created
            foreach (var activity in instance.ActiveActivities.Concat(instance.History))
            {
                await SaveActivityInstanceAsync(activity);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteWorkflowInstanceAsync(Guid id)
    {
        var entity = await _context.WorkflowInstances.FindAsync(id);
        if (entity != null)
        {
            _context.WorkflowInstances.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Activity Instances

    public async Task<ActivityInstance?> GetActivityInstanceAsync(Guid id)
    {
        var entity = await _context.ActivityInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity != null ? MapToActivityInstance(entity) : null;
    }

    public async Task<IEnumerable<ActivityInstance>> GetActiveActivitiesAsync(Guid workflowInstanceId)
    {
        var entities = await _context.ActivityInstances
            .AsNoTracking()
            .Where(a => a.WorkflowInstanceId == workflowInstanceId &&
                       (a.State == ActivityState.Running || a.State == ActivityState.WaitingForInput))
            .ToListAsync();

        return entities.Select(MapToActivityInstance);
    }

    public async Task SaveActivityInstanceAsync(ActivityInstance activityInstance)
    {
        var entity = MapToActivityInstanceEntity(activityInstance);

        var existing = await _context.ActivityInstances
            .FirstOrDefaultAsync(a => a.Id == activityInstance.Id);

        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(entity);
            existing.InputJson = entity.InputJson;
            existing.OutputJson = entity.OutputJson;
        }
        else
        {
            await _context.ActivityInstances.AddAsync(entity);
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Mapping Methods

    private WorkflowDefinition MapToWorkflowDefinition(WorkflowDefinitionEntity entity)
    {
        return new WorkflowDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            StartActivityId = entity.StartActivityId,
            IsActive = entity.IsActive,
            Activities = JsonSerializer.Deserialize<List<ActivityDefinition>>(entity.ActivitiesJson, _jsonOptions) ?? new(),
            Transitions = JsonSerializer.Deserialize<List<Transition>>(entity.TransitionsJson, _jsonOptions) ?? new(),
            Variables = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.VariablesJson, _jsonOptions) ?? new(),
            Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.MetadataJson, _jsonOptions) ?? new()
        };
    }

    private WorkflowDefinitionEntity MapToWorkflowDefinitionEntity(WorkflowDefinition definition)
    {
        return new WorkflowDefinitionEntity
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            Version = definition.Version,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            StartActivityId = definition.StartActivityId,
            IsActive = definition.IsActive,
            ActivitiesJson = JsonSerializer.Serialize(definition.Activities, _jsonOptions),
            TransitionsJson = JsonSerializer.Serialize(definition.Transitions, _jsonOptions),
            VariablesJson = JsonSerializer.Serialize(definition.Variables, _jsonOptions),
            MetadataJson = JsonSerializer.Serialize(definition.Metadata, _jsonOptions)
        };
    }

    private WorkflowInstance MapToWorkflowInstance(WorkflowInstanceEntity entity)
    {
        var instance = new WorkflowInstance
        {
            Id = entity.Id,
            WorkflowDefinitionId = entity.WorkflowDefinitionId,
            State = entity.State,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            InitiatedBy = entity.InitiatedBy,
            BusinessKey = entity.BusinessKey,
            ErrorMessage = entity.ErrorMessage,
            ParentWorkflowInstanceId = entity.ParentWorkflowInstanceId,
            Variables = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.VariablesJson, _jsonOptions) ?? new(),
            Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.MetadataJson, _jsonOptions) ?? new()
        };

        // Map activity instances
        var activityInstances = entity.ActivityInstances.Select(MapToActivityInstance).ToList();

        // Separate active activities from history
        instance.ActiveActivities = activityInstances
            .Where(a => a.State == ActivityState.Running || a.State == ActivityState.WaitingForInput || a.State == ActivityState.Ready)
            .ToList();

        instance.History = activityInstances
            .Where(a => a.State == ActivityState.Completed || a.State == ActivityState.Failed ||
                       a.State == ActivityState.Cancelled || a.State == ActivityState.Skipped)
            .ToList();

        return instance;
    }

    private WorkflowInstanceEntity MapToWorkflowInstanceEntity(WorkflowInstance instance)
    {
        return new WorkflowInstanceEntity
        {
            Id = instance.Id,
            WorkflowDefinitionId = instance.WorkflowDefinitionId,
            State = instance.State,
            CreatedAt = instance.CreatedAt,
            StartedAt = instance.StartedAt,
            CompletedAt = instance.CompletedAt,
            InitiatedBy = instance.InitiatedBy,
            BusinessKey = instance.BusinessKey,
            ErrorMessage = instance.ErrorMessage,
            ParentWorkflowInstanceId = instance.ParentWorkflowInstanceId,
            VariablesJson = JsonSerializer.Serialize(instance.Variables, _jsonOptions),
            MetadataJson = JsonSerializer.Serialize(instance.Metadata, _jsonOptions)
        };
    }

    private ActivityInstance MapToActivityInstance(ActivityInstanceEntity entity)
    {
        return new ActivityInstance
        {
            Id = entity.Id,
            ActivityDefinitionId = entity.ActivityDefinitionId,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            State = entity.State,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            AssignedTo = entity.AssignedTo,
            AssignedToGroup = entity.AssignedToGroup,
            RetryCount = entity.RetryCount,
            ErrorMessage = entity.ErrorMessage,
            ErrorStackTrace = entity.ErrorStackTrace,
            Input = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.InputJson, _jsonOptions) ?? new(),
            Output = JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.OutputJson, _jsonOptions) ?? new()
        };
    }

    private ActivityInstanceEntity MapToActivityInstanceEntity(ActivityInstance activity)
    {
        return new ActivityInstanceEntity
        {
            Id = activity.Id,
            ActivityDefinitionId = activity.ActivityDefinitionId,
            WorkflowInstanceId = activity.WorkflowInstanceId,
            State = activity.State,
            CreatedAt = activity.CreatedAt,
            StartedAt = activity.StartedAt,
            CompletedAt = activity.CompletedAt,
            AssignedTo = activity.AssignedTo,
            AssignedToGroup = activity.AssignedToGroup,
            RetryCount = activity.RetryCount,
            ErrorMessage = activity.ErrorMessage,
            ErrorStackTrace = activity.ErrorStackTrace,
            InputJson = JsonSerializer.Serialize(activity.Input, _jsonOptions),
            OutputJson = JsonSerializer.Serialize(activity.Output, _jsonOptions)
        };
    }

    #endregion
}
