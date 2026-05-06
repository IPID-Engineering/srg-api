using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("categories")]
[Authorize(Roles = "Logistician,Admin")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await categoryService.GetAllAsync(cancellationToken));
    }

    [HttpGet("tree")]
    public async Task<ActionResult<List<CategoryTreeResponse>>> GetTree(CancellationToken cancellationToken)
    {
        return Ok(await categoryService.GetTreeAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await categoryService.CreateAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await categoryService.UpdateAsync(id, request, cancellationToken));
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await categoryService.DeleteAsync(id, cancellationToken);
            return NoContent();
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

    [HttpPost("import")]
    public async Task<ActionResult<ImportCategoriesResult>> Import(ImportCategoriesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await categoryService.ImportCategoriesAsync(request, cancellationToken));
    }
}
