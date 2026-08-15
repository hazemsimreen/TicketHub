using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public CommentService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        INotificationService notifications)
    {
        _uow = uow;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ServiceResult<PagedResult<CommentDto>>> GetForTicketAsync(
        Guid ticketId, PagedQuery query, CancellationToken ct = default)
    {
        var canSee = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query().AsNoTracking(), _currentUser)
            .AnyAsync(t => t.Id == ticketId, ct);

        if (!canSee)
        {
            return ServiceResult<PagedResult<CommentDto>>.NotFound("Ticket not found.");
        }

        var isStaff = TicketVisibility.IsStaff(_currentUser);

        var comments = _uow.Repository<TicketComment>()
            .Query()
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId && !c.IsDeleted);

        // Internal notes are stripped in SQL for citizens — never in the controller,
        // never on the front end, because the JSON already went over the wire.
        if (!isStaff)
        {
            comments = comments.Where(c => !c.IsInternal);
        }

        var totalCount = await comments.CountAsync(ct);

        var items = await comments
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id) // tie-breaker
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                AuthorUserId = c.AuthorUserId,
                AuthorNameSnapshot = c.AuthorNameSnapshot,
                Body = c.Body,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        var result = new PagedResult<CommentDto>(items, query.Page, query.PageSize, totalCount);

        return ServiceResult<PagedResult<CommentDto>>.Success(result);
    }

    public async Task<ServiceResult<CommentDto>> AddAsync(
        Guid ticketId, AddCommentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Body))
        {
            return ServiceResult<CommentDto>.BadRequest("Comment body is required.");
        }

        if (_currentUser.UserId is null || !Guid.TryParse(_currentUser.UserId, out var authorId))
        {
            return ServiceResult<CommentDto>.Unauthorized("User is not authenticated.");
        }

        var ticket = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query(), _currentUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            return ServiceResult<CommentDto>.NotFound("Ticket not found.");
        }

        var isCitizen = _currentUser.IsInRole("Citizen");

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = authorId,               // identity from the token, never from the body
            AuthorNameSnapshot = _currentUser.UserName ?? "Unknown",
            Body = dto.Body.Trim(),
            IsInternal = !isCitizen && dto.IsInternal, // forced false for citizens no matter what they sent
            CreatedBy = authorId.ToString()
        };

        await _uow.Repository<TicketComment>().AddAsync(comment, ct);
        await _uow.SaveChangesAsync(ct);

        // Public comments notify "the other party": a citizen's comment notifies the
        // assigned agent (if any); a staff comment notifies the reporter.
        if (!comment.IsInternal)
        {
            Guid? recipientId = isCitizen
                ? ticket.AssignedToUserId
                : (ticket.SubmittedByUserId == authorId ? null : ticket.SubmittedByUserId);

            if (recipientId is not null)
            {
                await _notifications.NotifyAsync(
                    recipientUserId: recipientId.Value,
                    notificationTypeCode: "TicketCommentAdded",
                    message: $"A new comment was added to your ticket {ticket.TicketNumber}",
                    ticketId: ticketId,
                    ct: ct);
            }
        }

        return ServiceResult<CommentDto>.Created(ToDto(comment));
    }

    public async Task<ServiceResult<CommentDto>> UpdateAsync(
        Guid ticketId, Guid commentId, EditCommentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Body))
        {
            return ServiceResult<CommentDto>.BadRequest("Comment body is required.");
        }

        var commentRepo = _uow.Repository<TicketComment>();

        var comment = await commentRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

        // Check BOTH ids — a comment id from a different ticket must 404, not silently succeed.
        if (comment is null || comment.TicketId != ticketId || comment.IsDeleted)
        {
            return ServiceResult<CommentDto>.NotFound("Comment not found.");
        }

        var isOwner = _currentUser.UserId is not null &&
            Guid.TryParse(_currentUser.UserId, out var currentId) &&
            comment.AuthorUserId == currentId;

        var isAdmin = _currentUser.IsInRole("Admin");

        // Ownership is not a role. [Authorize(Roles="Agent")] answers "may you edit
        // comments" — it cannot answer "may you edit THIS comment". That check has
        // to happen here, on the row.
        if (!isOwner && !isAdmin)
        {
            return ServiceResult<CommentDto>.Forbidden("You can only edit your own comments.");
        }

        comment.Body = dto.Body.Trim();

        commentRepo.Update(comment);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<CommentDto>.Success(ToDto(comment));
    }

    public async Task<Result> DeleteAsync(Guid ticketId, Guid commentId, CancellationToken ct = default)
    {
        var commentRepo = _uow.Repository<TicketComment>();

        var comment = await commentRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

        if (comment is null || comment.TicketId != ticketId)
        {
            return Result.NotFound("Comment not found.");
        }

        var isOwner = _currentUser.UserId is not null &&
            Guid.TryParse(_currentUser.UserId, out var currentId) &&
            comment.AuthorUserId == currentId;

        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
        {
            return Result.Forbidden("You can only delete your own comments.");
        }

        commentRepo.Remove(comment); // soft delete — Repository<T>.Remove sets IsDeleted/DeletedAt

        await _uow.SaveChangesAsync(ct);

        return Result.NoContent();
    }

    private static CommentDto ToDto(TicketComment c) => new()
    {
        Id = c.Id,
        TicketId = c.TicketId,
        AuthorUserId = c.AuthorUserId,
        AuthorNameSnapshot = c.AuthorNameSnapshot,
        Body = c.Body,
        IsInternal = c.IsInternal,
        CreatedAt = c.CreatedAt
    };
}
