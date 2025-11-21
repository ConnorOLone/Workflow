using Workflow.Core.Models;

namespace Workflow.Core.Interfaces;

/// <summary>
/// Factory for creating activity handlers
/// </summary>
public interface IActivityHandlerFactory
{
    IActivityHandler GetHandler(ActivityType activityType);
    void RegisterHandler(IActivityHandler handler);
}
