using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("sections")]
[Authorize]
public class SectionsController(ISectionService sectionService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman,Logistician")]
    public async Task<ActionResult<List<SectionResponse>>> GetByProject(
        [FromQuery] Guid projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sectionService.GetByProjectAsync(projectId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<SectionResponse>> Create(
        CreateSectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await sectionService.CreateSectionAsync(request, cancellationToken));
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

    [HttpGet("{sectionId:guid}/installations")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<InstallationResponse>>> GetInstallations(
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sectionService.GetInstallationsBySectionAsync(sectionId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{sectionId:guid}/installations")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<InstallationResponse>> CreateInstallation(
        Guid sectionId,
        CreateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var fullRequest = request with { SectionId = sectionId };
            return Created(string.Empty, await sectionService.CreateInstallationAsync(fullRequest, cancellationToken));
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
}
