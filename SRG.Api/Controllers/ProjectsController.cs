using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController(
    IProjectService projectService,
    IProjectSubcontractorService projectSubcontractorService,
    ISubcontractorWorkerService subcontractorWorkerService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await projectService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<ProjectResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await projectService.GetByIdAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var project = await projectService.CreateProjectAsync(request, User.GetUserId(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/subcontractors")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<ProjectSubcontractorResponse>>> GetSubcontractors(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await projectSubcontractorService.GetByProjectAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/subcontractors")]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<ProjectSubcontractorResponse>> AssignSubcontractor(
        Guid id,
        AssignSubcontractorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await projectSubcontractorService.AssignAsync(id, request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/subcontractor-workers")]
    [Authorize(Roles = "Foreman,SubcontractorForeman,PM,SPM")]
    public async Task<ActionResult<List<SubcontractorWorkerResponse>>> GetSubcontractorWorkers(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await subcontractorWorkerService.GetByProjectAsync(id, cancellationToken));
    }
}
