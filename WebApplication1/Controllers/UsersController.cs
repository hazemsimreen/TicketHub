using BusinessLogic.Abstractions;
using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await _service.GetAllAsync(
            query,
            search,
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

    [HttpGet("lookup")]
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLookup(
        CancellationToken ct)
    {
        var result = await _service.GetLookupAsync(ct);

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

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
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
        [FromBody] CreateUserDto dto,
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

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserDto dto,
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

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(
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

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken ct)
    {
        var result = await _service.ActivateAsync(
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

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetUserPasswordDto dto,
        CancellationToken ct)
    {
        var result = await _service.ResetPasswordAsync(
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

        return NoContent();
    }
}