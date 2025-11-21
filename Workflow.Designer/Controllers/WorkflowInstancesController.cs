using Microsoft.AspNetCore.Mvc;
using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Designer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowInstancesController : ControllerBase
{
    private readonly IWorkflowEngine _engine;
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<WorkflowInstancesController> _logger;

    public WorkflowInstancesController(
        IWorkflowEngine engine,
        IWorkflowRepository repository,
        ILogger<WorkflowInstancesController> logger)
    {
        _engine = engine;
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowInstance>>> GetAll()
    {
        var instances = await _repository.GetActiveWorkflowInstancesAsync();
        return Ok(instances);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowInstance>> Get(Guid id)
    {
        var instance = await _engine.GetWorkflowInstanceAsync(id);
        if (instance == null)
        {
            return NotFound();
        }
        return Ok(instance);
    }

    [HttpPost("start")]
    public async Task<ActionResult<WorkflowInstance>> Start([FromBody] StartWorkflowRequest request)
    {
        var instance = await _engine.StartWorkflowAsync(
            request.WorkflowDefinitionId,
            request.InitialVariables,
            request.InitiatedBy);

        return CreatedAtAction(nameof(Get), new { id = instance.Id }, instance);
    }

    [HttpPost("{id}/suspend")]
    public async Task<ActionResult> Suspend(Guid id)
    {
        await _engine.SuspendWorkflowAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/resume")]
    public async Task<ActionResult> Resume(Guid id)
    {
        await _engine.ResumeWorkflowAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelWorkflowRequest? request)
    {
        await _engine.CancelWorkflowAsync(id, request?.Reason);
        return NoContent();
    }

    [HttpPost("activities/{activityId}/complete")]
    public async Task<ActionResult> CompleteActivity(
        Guid activityId,
        [FromBody] CompleteActivityRequest request)
    {
        await _engine.CompleteActivityAsync(activityId, request.Output, request.CompletedBy);
        return NoContent();
    }
}

public class StartWorkflowRequest
{
    public Guid WorkflowDefinitionId { get; set; }
    public Dictionary<string, object?>? InitialVariables { get; set; }
    public string? InitiatedBy { get; set; }
}

public class CompleteActivityRequest
{
    public Dictionary<string, object?>? Output { get; set; }
    public string? CompletedBy { get; set; }
}

public class CancelWorkflowRequest
{
    public string? Reason { get; set; }
}
