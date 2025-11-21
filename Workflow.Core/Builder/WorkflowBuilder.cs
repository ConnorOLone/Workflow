using Workflow.Core.Models;

namespace Workflow.Core.Builder;

/// <summary>
/// Fluent API for building workflow definitions
/// </summary>
public class WorkflowBuilder
{
    private readonly WorkflowDefinition _definition;
    private readonly List<ActivityBuilder> _activityBuilders = new();
    private readonly List<TransitionBuilder> _transitionBuilders = new();

    public WorkflowBuilder(string name, string? version = null)
    {
        _definition = new WorkflowDefinition
        {
            Name = name,
            Version = version ?? "1.0.0"
        };
    }

    public WorkflowBuilder WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    public WorkflowBuilder WithVariable(string name, object? defaultValue = null)
    {
        _definition.Variables[name] = defaultValue;
        return this;
    }

    public WorkflowBuilder WithMetadata(string key, string value)
    {
        _definition.Metadata[key] = value;
        return this;
    }

    public ActivityBuilder AddStartActivity(string name)
    {
        var activityBuilder = new ActivityBuilder(name, ActivityType.Start, this);
        _activityBuilders.Add(activityBuilder);

        if (_activityBuilders.Count == 1)
        {
            _definition.StartActivityId = activityBuilder.Id;
        }

        return activityBuilder;
    }

    public ActivityBuilder AddActivity(string name, ActivityType type)
    {
        var activityBuilder = new ActivityBuilder(name, type, this);
        _activityBuilders.Add(activityBuilder);
        return activityBuilder;
    }

    public ActivityBuilder AddHumanTask(string name)
    {
        return AddActivity(name, ActivityType.HumanTask);
    }

    public ActivityBuilder AddServiceTask(string name)
    {
        return AddActivity(name, ActivityType.ServiceTask);
    }

    public ActivityBuilder AddScriptTask(string name)
    {
        return AddActivity(name, ActivityType.ScriptTask);
    }

    public ActivityBuilder AddDecision(string name)
    {
        return AddActivity(name, ActivityType.Decision);
    }

    public ActivityBuilder AddEndActivity(string name)
    {
        return AddActivity(name, ActivityType.End);
    }

    public TransitionBuilder AddTransition(string fromActivityName, string toActivityName)
    {
        var transitionBuilder = new TransitionBuilder(fromActivityName, toActivityName, this);
        _transitionBuilders.Add(transitionBuilder);
        return transitionBuilder;
    }

    public WorkflowDefinition Build()
    {
        // Build all activities
        foreach (var activityBuilder in _activityBuilders)
        {
            _definition.Activities.Add(activityBuilder.Build());
        }

        // Build all transitions
        foreach (var transitionBuilder in _transitionBuilders)
        {
            var fromActivity = _definition.Activities.FirstOrDefault(a => a.Name == transitionBuilder.FromActivityName);
            var toActivity = _definition.Activities.FirstOrDefault(a => a.Name == transitionBuilder.ToActivityName);

            if (fromActivity == null)
                throw new InvalidOperationException($"Activity '{transitionBuilder.FromActivityName}' not found");

            if (toActivity == null)
                throw new InvalidOperationException($"Activity '{transitionBuilder.ToActivityName}' not found");

            var transition = transitionBuilder.Build();
            transition.FromActivityId = fromActivity.Id;
            transition.ToActivityId = toActivity.Id;

            _definition.Transitions.Add(transition);
        }

        return _definition;
    }
}

public class ActivityBuilder
{
    private readonly ActivityDefinition _definition;
    private readonly WorkflowBuilder _workflowBuilder;

    internal Guid Id => _definition.Id;

    internal ActivityBuilder(string name, ActivityType type, WorkflowBuilder workflowBuilder)
    {
        _definition = new ActivityDefinition
        {
            Name = name,
            Type = type
        };
        _workflowBuilder = workflowBuilder;
    }

    public ActivityBuilder WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    public ActivityBuilder WithConfiguration(string key, object? value)
    {
        _definition.Configuration[key] = value;
        return this;
    }

    public ActivityBuilder WithInputMapping(string activityInputName, string workflowVariableName)
    {
        _definition.InputMappings[activityInputName] = workflowVariableName;
        return this;
    }

    public ActivityBuilder WithOutputMapping(string activityOutputName, string workflowVariableName)
    {
        _definition.OutputMappings[activityOutputName] = workflowVariableName;
        return this;
    }

    public ActivityBuilder WithTimeout(int seconds)
    {
        _definition.TimeoutSeconds = seconds;
        return this;
    }

    public ActivityBuilder WithRetry(int maxRetries)
    {
        _definition.AllowRetry = true;
        _definition.MaxRetries = maxRetries;
        return this;
    }

    public ActivityBuilder WithPosition(int x, int y)
    {
        _definition.Position = new Position { X = x, Y = y };
        return this;
    }

    // Human Task specific
    public ActivityBuilder AssignTo(string userId)
    {
        return WithConfiguration("assignedTo", userId);
    }

    public ActivityBuilder AssignToGroup(string groupName)
    {
        return WithConfiguration("assignedToGroup", groupName);
    }

    // Service Task specific
    public ActivityBuilder WithService(string serviceName, string methodName)
    {
        return WithConfiguration("serviceName", serviceName)
               .WithConfiguration("methodName", methodName);
    }

    // Script Task specific
    public ActivityBuilder WithScript(string script, string language = "csharp")
    {
        return WithConfiguration("script", script)
               .WithConfiguration("language", language);
    }

    // Flow methods to continue building
    public ActivityBuilder Then(string activityName, ActivityType type)
    {
        var nextActivity = _workflowBuilder.AddActivity(activityName, type);
        _workflowBuilder.AddTransition(_definition.Name, activityName);
        return nextActivity;
    }

    public ActivityBuilder ThenHumanTask(string name)
    {
        return Then(name, ActivityType.HumanTask);
    }

    public ActivityBuilder ThenServiceTask(string name)
    {
        return Then(name, ActivityType.ServiceTask);
    }

    public ActivityBuilder ThenDecision(string name)
    {
        return Then(name, ActivityType.Decision);
    }

    public ActivityBuilder ThenEnd(string name = "End")
    {
        return Then(name, ActivityType.End);
    }

    public TransitionBuilder TransitionTo(string targetActivityName)
    {
        return _workflowBuilder.AddTransition(_definition.Name, targetActivityName);
    }

    public WorkflowBuilder EndActivity()
    {
        return _workflowBuilder;
    }

    internal ActivityDefinition Build()
    {
        return _definition;
    }
}

public class TransitionBuilder
{
    private readonly Transition _transition;
    private readonly WorkflowBuilder _workflowBuilder;

    internal string FromActivityName { get; }
    internal string ToActivityName { get; }

    internal TransitionBuilder(string fromActivityName, string toActivityName, WorkflowBuilder workflowBuilder)
    {
        FromActivityName = fromActivityName;
        ToActivityName = toActivityName;
        _workflowBuilder = workflowBuilder;
        _transition = new Transition();
    }

    public TransitionBuilder WithName(string name)
    {
        _transition.Name = name;
        return this;
    }

    public TransitionBuilder When(string condition)
    {
        _transition.Condition = condition;
        return this;
    }

    public TransitionBuilder WithPriority(int priority)
    {
        _transition.Priority = priority;
        return this;
    }

    public TransitionBuilder AsDefault()
    {
        _transition.IsDefault = true;
        return this;
    }

    public WorkflowBuilder EndTransition()
    {
        return _workflowBuilder;
    }

    internal Transition Build()
    {
        return _transition;
    }
}
