using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetTickets(
        [FromQuery] Guid? statusId,
        [FromQuery] Guid? categoryId)
    {
        IQueryable<Ticket> query = _db.Tickets.AsNoTracking();

        if (statusId.HasValue)
        {
            query = query.Where(ticket =>
                ticket.StatusId == statusId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(ticket =>
                ticket.CategoryId == categoryId.Value);
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenByDescending(ticket => ticket.Id)
            .Select(ticket => new TicketResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                SubmittedByUserId = ticket.SubmittedByUserId,
                CategoryId = ticket.CategoryId,
                DepartmentId = ticket.DepartmentId,
                PriorityId = ticket.PriorityId,
                StatusId = ticket.StatusId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt
            })
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> GetTicketById(Guid id)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id)
            .Select(ticket => new TicketResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                SubmittedByUserId = ticket.SubmittedByUserId,
                CategoryId = ticket.CategoryId,
                DepartmentId = ticket.DepartmentId,
                PriorityId = ticket.PriorityId,
                StatusId = ticket.StatusId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> CreateTicket(
        [FromBody] CreateTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Title and Description are required.");
        }

        var userExists = await _db.Users
            .AnyAsync(user => user.Id == request.SubmittedByUserId);

        if (!userExists)
        {
            return BadRequest("Submitted user was not found.");
        }

        var categoryExists = await _db.Categories
            .AnyAsync(category => category.Id == request.CategoryId);

        if (!categoryExists)
        {
            return BadRequest("Category was not found.");
        }

        var departmentExists = await _db.Departments
            .AnyAsync(department => department.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest("Department was not found.");
        }

        var priorityExists = await _db.TicketPriorities
            .AnyAsync(priority => priority.Id == request.PriorityId);

        if (!priorityExists)
        {
            return BadRequest("Priority was not found.");
        }

        Guid statusId;

        if (request.StatusId.HasValue)
        {
            var statusExists = await _db.TicketStatuses
                .AnyAsync(status => status.Id == request.StatusId.Value);

            if (!statusExists)
            {
                return BadRequest("Status was not found.");
            }

            statusId = request.StatusId.Value;
        }
        else
        {
            var openStatus = await _db.TicketStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(status => status.Code == "Open");

            if (openStatus == null)
            {
                return BadRequest(
                    "The Open status does not exist in the database.");
            }

            statusId = openStatus.Id;
        }

        var ticketNumber = string.IsNullOrWhiteSpace(request.TicketNumber)
            ? $"TKT-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : request.TicketNumber.Trim();

        var numberExists = await _db.Tickets
            .IgnoreQueryFilters()
            .AnyAsync(ticket =>
                ticket.TicketNumber == ticketNumber);

        if (numberExists)
        {
            return BadRequest("Ticket number already exists.");
        }

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SubmittedByUserId = request.SubmittedByUserId,
            CategoryId = request.CategoryId,
            DepartmentId = request.DepartmentId,
            PriorityId = request.PriorityId,
            StatusId = statusId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            ResolvedAt = null
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var response = MapTicket(ticket);

        return CreatedAtAction(
            nameof(GetTicketById),
            new { id = ticket.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> UpdateTicket(
        Guid id,
        [FromBody] UpdateTicketRequest request)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Title and Description are required.");
        }

        var categoryExists = await _db.Categories
            .AnyAsync(category => category.Id == request.CategoryId);

        if (!categoryExists)
        {
            return BadRequest("Category was not found.");
        }

        var departmentExists = await _db.Departments
            .AnyAsync(department => department.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest("Department was not found.");
        }

        var priorityExists = await _db.TicketPriorities
            .AnyAsync(priority => priority.Id == request.PriorityId);

        if (!priorityExists)
        {
            return BadRequest("Priority was not found.");
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.CategoryId = request.CategoryId;
        ticket.DepartmentId = request.DepartmentId;
        ticket.PriorityId = request.PriorityId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapTicket(ticket));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<TicketResponse>> UpdateTicketStatus(
        Guid id,
        [FromBody] UpdateTicketStatusRequest request)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var status = await _db.TicketStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status =>
                status.Id == request.StatusId);

        if (status == null)
        {
            return BadRequest("Status was not found.");
        }

        ticket.StatusId = request.StatusId;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status.Code == "Resolved" ||
            status.Code == "Closed")
        {
            ticket.ResolvedAt = DateTime.UtcNow;
        }
        else
        {
            ticket.ResolvedAt = null;
        }

        await _db.SaveChangesAsync();

        return Ok(MapTicket(ticket));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        ticket.IsDeleted = true;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{ticketId:guid}/comments")]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetComments(
        Guid ticketId)
    {
        var ticketExists = await _db.Tickets
            .AnyAsync(ticket => ticket.Id == ticketId);

        if (!ticketExists)
        {
            return NotFound();
        }

        var comments = await _db.TicketComments
            .AsNoTracking()
            .Where(comment => comment.TicketId == ticketId)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id)
            .Select(comment => new CommentResponse
            {
                Id = comment.Id,
                TicketId = comment.TicketId,
                AuthorUserId = comment.AuthorUserId,
                StepInstanceId = comment.StepInstanceId,
                ParentCommentId = comment.ParentCommentId,
                Body = comment.Body,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost("{ticketId:guid}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(
        Guid ticketId,
        [FromBody] CreateCommentRequest request)
    {
        var ticketExists = await _db.Tickets
            .AnyAsync(ticket => ticket.Id == ticketId);

        if (!ticketExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest("Comment body is required.");
        }

        var authorExists = await _db.Users
            .AnyAsync(user => user.Id == request.AuthorUserId);

        if (!authorExists)
        {
            return BadRequest("Author user was not found.");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _db.TicketComments
                .AnyAsync(comment =>
                    comment.Id == request.ParentCommentId.Value &&
                    comment.TicketId == ticketId);

            if (!parentExists)
            {
                return BadRequest(
                    "Parent comment was not found for this ticket.");
            }
        }

        var comment = new TicketComment
        {
            TicketId = ticketId,
            AuthorUserId = request.AuthorUserId,
            StepInstanceId = request.StepInstanceId,
            ParentCommentId = request.ParentCommentId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync();

        var response = new CommentResponse
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorUserId = comment.AuthorUserId,
            StepInstanceId = comment.StepInstanceId,
            ParentCommentId = comment.ParentCommentId,
            Body = comment.Body,
            IsInternal = comment.IsInternal,
            CreatedAt = comment.CreatedAt
        };

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CategorySummaryResponse>>>
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

    private static TicketResponse MapTicket(Ticket ticket)
    {
        return new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            SubmittedByUserId = ticket.SubmittedByUserId,
            CategoryId = ticket.CategoryId,
            DepartmentId = ticket.DepartmentId,
            PriorityId = ticket.PriorityId,
            StatusId = ticket.StatusId,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt
        };
    }
}

public class CreateTicketRequest
{
    public string? TicketNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid SubmittedByUserId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid? StatusId { get; set; }
}

public class UpdateTicketRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid PriorityId { get; set; }
}

public class UpdateTicketStatusRequest
{
    public Guid StatusId { get; set; }
}

public class CreateCommentRequest
{
    public Guid AuthorUserId { get; set; }

    public Guid? StepInstanceId { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}

public class TicketResponse
{
    public Guid Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid SubmittedByUserId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid StatusId { get; set; }

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
    public Guid Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int TicketCount { get; set; }
}