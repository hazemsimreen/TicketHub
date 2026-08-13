using System.Security.Claims;
using BusinessLogic.Abstractions;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contract.Paged;
namespace WebApplication1.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _service;

    public AgentsController(IAgentService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? departmentId,
        [FromQuery] bool? active,
        [FromQuery] bool? hasCapacity,
        [FromQuery] string? skill,
        CancellationToken ct)
    {
        var result = await _service.GetAllAsync(
            departmentId,
            active,
            hasCapacity,
            skill,
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

    [HttpGet("me")]
    [Authorize(Roles = "Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(
        CancellationToken ct)
    {
        var userIdText =
            User.FindFirstValue("sub");

        if (!Guid.TryParse(
                userIdText,
                out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _service.GetByUserIdAsync(
                userId,
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
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var result =
            await _service.GetByIdAsync(
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAgentDto dto,
        CancellationToken ct)
    {
        var result =
            await _service.CreateAsync(
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
    [Authorize(Roles = "Admin,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAgentDto dto,
        CancellationToken ct)
    {
        var result =
            await _service.UpdateAsync(
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

    [HttpPut("{id:int}/profile")]
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        int id,
        [FromBody] UpdateAgentProfileDto dto,
        CancellationToken ct)
    {
        if (User.IsInRole("Agent") &&
            !User.IsInRole("Admin") &&
            !User.IsInRole("Supervisor"))
        {
            var userIdText =
                User.FindFirstValue("sub");

            if (!Guid.TryParse(
                    userIdText,
                    out var userId))
            {
                return Unauthorized();
            }

            var ownAgent =
                await _service.GetByUserIdAsync(
                    userId,
                    ct);

            if (!ownAgent.IsSuccess ||
                ownAgent.Data is null)
            {
                return Forbid();
            }

            if (ownAgent.Data.Id != id)
            {
                return Forbid();
            }
        }

        var result =
            await _service.UpdateProfileAsync(
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
        var result =
            await _service.DeleteAsync(
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

    [HttpGet("/api/skills")]
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSkills(
        CancellationToken ct)
    {
        var result =
            await _service.GetSkillsAsync(ct);

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
}