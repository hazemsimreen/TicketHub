using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimeNotifier _realtime;

    public NotificationService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IRealtimeNotifier realtime)
    {
        _uow = uow;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(_currentUser.UserId, out userId);

    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetMineAsync(
        PagedQuery query, CancellationToken ct = default)
    {
        // Mine only. There is no user-id parameter, by design — identity comes
        // from the token, never from the query string.
        if (!TryGetCurrentUserId(out var userId))
        {
            return ServiceResult<PagedResult<NotificationDto>>.Unauthorized("User is not authenticated.");
        }

        var notifications = _uow.Repository<Notification>()
            .Query()
            .AsNoTracking()
            .Include(n => n.NotificationType)
            .Where(n => n.RecipientUserId == userId && !n.IsDeleted);

        var totalCount = await notifications.CountAsync(ct);

        var items = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                NotificationTypeCode = n.NotificationType.Code,
                Message = n.Message,
                TicketId = n.TicketId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);

        var result = new PagedResult<NotificationDto>(items, query.Page, query.PageSize, totalCount);

        return ServiceResult<PagedResult<NotificationDto>>.Success(result);
    }

    public async Task<ServiceResult<UnreadCountDto>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        // Its own endpoint — the badge is polled far more than the list is opened,
        // so this stays a single COUNT(*), not a full fetch.
        if (!TryGetCurrentUserId(out var userId))
        {
            return ServiceResult<UnreadCountDto>.Unauthorized("User is not authenticated.");
        }

        var count = await _uow.Repository<Notification>()
            .Query()
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead && !n.IsDeleted, ct);

        return ServiceResult<UnreadCountDto>.Success(new UnreadCountDto { Count = count });
    }

    public async Task<Result> MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Result.Unauthorized("User is not authenticated.");
        }

        var repo = _uow.Repository<Notification>();

        var notification = await repo.Query()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, ct);

        if (notification is null)
        {
            return Result.NotFound("Notification not found.");
        }

        // Idempotent — marking an already-read notification as read again is a no-op success.
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            repo.Update(notification);
            await _uow.SaveChangesAsync(ct);
        }

        return Result.NoContent();
    }

    public async Task<Result> MarkAllReadAsync(CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Result.Unauthorized("User is not authenticated.");
        }

        // One UPDATE statement, not a fetch-then-loop-then-save.
        await _uow.Repository<Notification>()
            .Query()
            .Where(n => n.RecipientUserId == userId && !n.IsRead && !n.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), ct);

        return Result.NoContent();
    }

    public async Task<Result> DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Result.Unauthorized("User is not authenticated.");
        }

        var repo = _uow.Repository<Notification>();

        var notification = await repo.Query()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, ct);

        if (notification is null)
        {
            return Result.NotFound("Notification not found.");
        }

        repo.Remove(notification); // soft delete — "dismiss"
        await _uow.SaveChangesAsync(ct);

        return Result.NoContent();
    }

    public async Task NotifyAsync(
        Guid recipientUserId, string notificationTypeCode, string message,
        Guid? ticketId = null, CancellationToken ct = default)
    {
        var notificationTypeId = await _uow.Repository<NotificationType>()
            .Query()
            .AsNoTracking()
            .Where(nt => nt.Code == notificationTypeCode)
            .Select(nt => (Guid?)nt.Id)
            .FirstOrDefaultAsync(ct);

        if (notificationTypeId is null)
        {
            throw new InvalidOperationException(
                $"NotificationType with Code '{notificationTypeCode}' does not exist. Seed it first.");
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            NotificationTypeId = notificationTypeId.Value,
            TicketId = ticketId,
            IsRead = false,
            Message = message
        };

        // Save the notification row, THEN push it — the row is the truth, the
        // real-time push is a convenience. If we only pushed, anyone offline
        // right now would never find out.
        await _uow.Repository<Notification>().AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(recipientUserId.ToString(), message);
    }
}
