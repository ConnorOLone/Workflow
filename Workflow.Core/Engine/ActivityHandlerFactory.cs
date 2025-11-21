using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Core.Engine;

/// <summary>
/// Default implementation of activity handler factory
/// </summary>
public class ActivityHandlerFactory : IActivityHandlerFactory
{
    private readonly Dictionary<ActivityType, IActivityHandler> _handlers = new();

    public ActivityHandlerFactory()
    {
        // Register default handlers
        RegisterHandler(new StartActivityHandler());
        RegisterHandler(new EndActivityHandler());
    }

    public IActivityHandler GetHandler(ActivityType activityType)
    {
        if (_handlers.TryGetValue(activityType, out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException($"No handler registered for activity type: {activityType}");
    }

    public void RegisterHandler(IActivityHandler handler)
    {
        _handlers[handler.SupportedType] = handler;
    }
}
