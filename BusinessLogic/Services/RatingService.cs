using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;

namespace BusinessLogic.Services;

public class RatingService : IRatingService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public RatingService(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<RatingDto>> AddAsync(
        Guid ticketId, AddRatingDto dto, CancellationToken ct = default)
    {
        if (dto.Stars is < 1 or > 5)
        {
            return ServiceResult<RatingDto>.BadRequest("Stars must be between 1 and 5.");
        }

        if (_currentUser.UserId is null || !Guid.TryParse(_currentUser.UserId, out var raterId))
        {
            return ServiceResult<RatingDto>.Unauthorized("User is not authenticated.");
        }

        var ticket = await _uow.Repository<Ticket>()
            .Query()
            .AsNoTracking()
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            return ServiceResult<RatingDto>.NotFound("Ticket not found.");
        }

        // Only the reporter may rate their own ticket — not any citizen, not staff.
        if (ticket.SubmittedByUserId != raterId)
        {
            return ServiceResult<RatingDto>.Forbidden("Only the person who reported this ticket can rate it.");
        }

        if (ticket.Status.Code != "Resolved")
        {
            return ServiceResult<RatingDto>.Conflict("Only resolved tickets can be rated.");
        }

        var ratingRepo = _uow.Repository<Rating>();

        var alreadyRated = await ratingRepo.ExistsAsync(r => r.TicketId == ticketId, ct);
        if (alreadyRated)
        {
            return ServiceResult<RatingDto>.Conflict("This ticket has already been rated.");
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Stars = dto.Stars,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            RatedByUserId = raterId,
            CreatedBy = raterId.ToString()
        };

        await ratingRepo.AddAsync(rating, ct);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index on TicketId is what actually prevents two concurrent
            // posts from both succeeding — the ExistsAsync check above just makes
            // the common case a friendly 409 instead of a raw 500.
            return ServiceResult<RatingDto>.Conflict("This ticket has already been rated.");
        }

        return ServiceResult<RatingDto>.Created(ToDto(rating));
    }

    public async Task<ServiceResult<RatingDto>> GetForTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var canSee = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query().AsNoTracking(), _currentUser)
            .AnyAsync(t => t.Id == ticketId, ct);

        if (!canSee)
        {
            return ServiceResult<RatingDto>.NotFound("Ticket not found.");
        }

        var rating = await _uow.Repository<Rating>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TicketId == ticketId, ct);

        if (rating is null)
        {
            return ServiceResult<RatingDto>.NotFound("This ticket has not been rated yet.");
        }

        return ServiceResult<RatingDto>.Success(ToDto(rating));
    }

    private static RatingDto ToDto(Rating r) => new()
    {
        Id = r.Id,
        TicketId = r.TicketId,
        Stars = r.Stars,
        Comment = r.Comment,
        RatedByUserId = r.RatedByUserId,
        CreatedAt = r.CreatedAt
    };
}
