using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITicketService _ticketService;

    public TicketsController(
        AppDbContext db,
        ITicketService ticketService)
    {
        _db = db;
        _ticketService = ticketService;
    }


    // =========================================================
    // DEBUG AUTH
    // GET: /api/tickets/debug-auth
    // =========================================================

    [HttpGet("debug-auth")]
    [Authorize]
    public IActionResult DebugAuth()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,

            Name = User.Identity?.Name,

            Claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }),

            IsAdmin = User.IsInRole("Admin"),

            IsDepartmentHead = User.IsInRole("DepartmentHead"),

            IsEmployee = User.IsInRole("Employee"),

            IsCitizen = User.IsInRole("Citizen")
        });
    }


    // =========================================================
    // GET: /api/tickets
    // =========================================================
    //
    // الوصول الحقيقي يتم داخل TicketService
    // =========================================================

    [HttpGet]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<PagedResult<TicketListItemDto>>>>
        GetTickets(
            [FromQuery] TicketQueryDto query,
            CancellationToken cancellationToken = default)
    {
        var result = await _ticketService.ListTicketsAsync(
            query,
            cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // GET: /api/tickets/statistics
    // Staff only
    // =========================================================

    [HttpGet("statistics")]
    [Authorize]
    public async Task<ActionResult<ServiceResult<TicketStatisticsDto>>>
        GetTicketStatistics(
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.GetTicketStatisticsAsync(
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // GET: /api/tickets/{id}
    // =========================================================

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        GetTicketById(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.GetTicketByIdAsync(
                id,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // GET: /api/tickets/{ticketId}/history
    // =========================================================

    [HttpGet("{ticketId:guid}/history")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<PagedResult<TicketHistoryDto>>>>
        GetTicketHistory(
            Guid ticketId,
            [FromQuery] PagedQuery query,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.GetTicketHistoryAsync(
                ticketId,
                query,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // GET: /api/tickets/my
    // =========================================================

    [HttpGet("my")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<PagedResult<TicketListItemDto>>>>
        GetMyTickets(
            [FromQuery] TicketQueryDto query,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.GetMyTicketsAsync(
                query,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // POST: /api/tickets
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<
        ActionResult<TicketDetailDto>>
        CreateTicket(
            [FromBody] CreateTicketRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(
                "Title and Description are required.");
        }

        var dto = new CreateTicketDto
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId
        };

        var result =
            await _ticketService.CreateTicketAsync(
                dto,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode,
                result);
        }

        return CreatedAtAction(
            nameof(GetTicketById),
            new { id = result.Data!.Id },
            result);
    }


    // =========================================================
    // PUT: /api/tickets/{id}
    // =========================================================
    //
    // UpdateTicket أصبح من خلال Service
    // وليس مباشرة عن طريق _db
    // =========================================================

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        UpdateTicket(
            Guid id,
            [FromBody] UpdateTicketRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(
                "Title and Description are required.");
        }

        var dto = new UpdateTicketDto
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            RowVersion = request.RowVersion
        };

        var result =
            await _ticketService.UpdateTicketAsync(
                id,
                dto,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // PUT: /api/tickets/{id}/status
    // =========================================================

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        UpdateTicketStatus(
            Guid id,
            [FromBody] UpdateTicketStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewStatusCode))
        {
            return BadRequest(
                "NewStatusCode is required.");
        }

        var dto = new UpdateTicketStatusDto
        {
            NewStatusCode = request.NewStatusCode,
            Reason = request.Reason
        };

        var result =
            await _ticketService.UpdateTicketStatusAsync(
                id,
                dto,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // PUT: /api/tickets/{id}/assign
    // =========================================================
    //
    // Admin + DepartmentHead
    // =========================================================

    [HttpPatch("{id:guid}/assign")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        AssignTicket(
            Guid id,
            [FromBody] AssignTicketRequest request,
            CancellationToken cancellationToken = default)
    {
        var dto = new AssignTicketDto
        {
            AssignedToUserId = request.AssignedToUserId
        };

        var result =
            await _ticketService.AssignTicketAsync(
                id,
                dto,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // POST: /api/tickets/{id}/auto-assign
    // =========================================================

    [HttpPost("{id:guid}/auto-assign")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        AutoAssignTicket(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.AutoAssignTicketAsync(
                id,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // POST: /api/tickets/{id}/reopen
    // =========================================================

    [HttpPost("{id:guid}/reopen")]
    [Authorize]
    public async Task<
        ActionResult<ServiceResult<TicketDetailDto>>>
        ReopenTicket(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.ReopenTicketAsync(
                id,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // DELETE: /api/tickets/{id}
    // Admin only
    // =========================================================

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTicket(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _ticketService.DeleteTicketAsync(
                id,
                cancellationToken);

        return StatusCode(
            result.StatusCode,
            result);
    }


    // =========================================================
    // GET: /api/categories
    // =========================================================

    [HttpGet("/api/categories")]
    [Authorize]
    public async Task<
        ActionResult<IEnumerable<CategorySummaryResponse>>>
        GetCategories()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategorySummaryResponse
            {
                Id = category.Id,
                CategoryName = category.Name,
                TicketCount = category.Tickets.Count()
            })
            .ToListAsync();

        return Ok(categories);
    }
}


// =============================================================
// Request Models
// =============================================================

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
}


public class UpdateTicketRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int PriorityId { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}


public class UpdateTicketStatusRequest
{
    public string NewStatusCode { get; set; } = string.Empty;

    public string? Reason { get; set; }
}


public class AssignTicketRequest
{
    public Guid? AssignedToUserId { get; set; }
}


public class CreateCommentRequest
{
    public Guid AuthorUserId { get; set; }

    public Guid? StepInstanceId { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}


// =============================================================
// Response Models
// =============================================================

public class TicketResponse
{
    public Guid Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid SubmittedByUserId { get; set; }

    public int CategoryId { get; set; }

    public int DepartmentId { get; set; }

    public int PriorityId { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}


public class CommentResponse
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Guid AuthorUserId { get; set; }

    public Guid? StepInstanceId { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }
}


public class CategorySummaryResponse
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int TicketCount { get; set; }
}