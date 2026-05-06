using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("grv")]
[Authorize(Roles = "Logistician")]
public class GoodsReceivedVouchersController(IGoodsReceivedVoucherService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> Create(
        CreateGoodsReceivedVoucherRequest request,
        CancellationToken cancellationToken)
    {
        return await WriteAction(() => service.CreateAsync(request, User.GetUserId(), cancellationToken), created: true);
    }

    [HttpGet]
    public async Task<ActionResult<List<GoodsReceivedVoucherResponse>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> AddItem(
        Guid id,
        AddGoodsReceivedVoucherItemRequest request,
        CancellationToken cancellationToken)
    {
        return await WriteAction(() => service.AddItemAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        return await WriteAction(() => service.ConfirmAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return await WriteAction(() => service.CancelAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPost("import")]
    public async Task<ActionResult<GoodsReceivedVoucherResponse>> Import(
        ImportGrvRequest request,
        CancellationToken cancellationToken)
    {
        return await WriteAction(() => service.ImportAsync(request, User.GetUserId(), cancellationToken), created: true);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(id, User.GetUserId(), cancellationToken);
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

    private async Task<ActionResult<GoodsReceivedVoucherResponse>> WriteAction(
        Func<Task<GoodsReceivedVoucherResponse>> action,
        bool created = false)
    {
        try
        {
            var response = await action();
            return created ? Created(string.Empty, response) : Ok(response);
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
