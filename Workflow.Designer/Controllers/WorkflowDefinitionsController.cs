using Microsoft.AspNetCore.Mvc;
using Workflow.Core.Interfaces;
using Workflow.Core.Models;

namespace Workflow.Designer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowDefinitionsController : ControllerBase
{
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<WorkflowDefinitionsController> _logger;

    public WorkflowDefinitionsController(
        IWorkflowRepository repository,
        ILogger<WorkflowDefinitionsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowDefinition>>> GetAll()
    {
        var definitions = await _repository.GetAllWorkflowDefinitionsAsync();
        return Ok(definitions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDefinition>> Get(Guid id)
    {
        var definition = await _repository.GetWorkflowDefinitionAsync(id);
        if (definition == null)
        {
            return NotFound();
        }
        return Ok(definition);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinition>> Create([FromBody] WorkflowDefinition definition)
    {
        if (definition.Id == Guid.Empty)
        {
            definition.Id = Guid.NewGuid();
        }
        definition.CreatedAt = DateTime.UtcNow;

        await _repository.SaveWorkflowDefinitionAsync(definition);
        return CreatedAtAction(nameof(Get), new { id = definition.Id }, definition);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] WorkflowDefinition definition)
    {
        var existing = await _repository.GetWorkflowDefinitionAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        definition.Id = id;
        definition.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveWorkflowDefinitionAsync(definition);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await _repository.GetWorkflowDefinitionAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _repository.DeleteWorkflowDefinitionAsync(id);
        return NoContent();
    }
}
