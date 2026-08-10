using BusinessLogic.Abstractions;
using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;

    public DepartmentsController(IDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        CancellationToken ct)
    {
        var result = await _service.GetAllAsync(
            query,
            ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                new
                {
                    message = result.ErrorMessage
                });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(
            id,
            ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                new
                {
                    message = result.ErrorMessage
                });
        }

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentDto dto,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(
            dto,
            ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                new
                {
                    message = result.ErrorMessage
                });
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Data!.Id
            },
            result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateDepartmentDto dto,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(
            id,
            dto,
            ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                new
                {
                    message = result.ErrorMessage
                });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var result = await _service.DeleteAsync(
            id,
            ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                new
                {
                    message = result.ErrorMessage
                });
        }

        return NoContent();
    }
}