using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using Microsoft.AspNetCore.Http;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface ICommentService
{
    Task<ServiceResult<PagedResult<CommentDto>>> GetForTicketAsync(
        Guid ticketId, PagedQuery query, CancellationToken ct = default);

    Task<ServiceResult<CommentDto>> AddAsync(
        Guid ticketId, AddCommentDto dto, CancellationToken ct = default);

    Task<ServiceResult<CommentDto>> UpdateAsync(
        Guid ticketId, Guid commentId, EditCommentDto dto, CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid ticketId, Guid commentId, CancellationToken ct = default);
}

public interface IAttachmentService
{
    Task<ServiceResult<IReadOnlyList<AttachmentDto>>> GetForTicketAsync(
        Guid ticketId, CancellationToken ct = default);

    Task<ServiceResult<AttachmentDto>> UploadAsync(
        Guid ticketId, IFormFile file, CancellationToken ct = default);

    Task<ServiceResult<AttachmentDownload>> DownloadAsync(
        Guid ticketId, Guid attachmentId, CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid ticketId, Guid attachmentId, CancellationToken ct = default);
}

public interface IRatingService
{
    Task<ServiceResult<RatingDto>> AddAsync(
        Guid ticketId, AddRatingDto dto, CancellationToken ct = default);

    Task<ServiceResult<RatingDto>> GetForTicketAsync(
        Guid ticketId, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<ServiceResult<PagedResult<NotificationDto>>> GetMineAsync(
        PagedQuery query, CancellationToken ct = default);

    Task<ServiceResult<UnreadCountDto>> GetUnreadCountAsync(
        CancellationToken ct = default);

    Task<Result> MarkReadAsync(
        Guid notificationId, CancellationToken ct = default);

    Task<Result> MarkAllReadAsync(
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid notificationId, CancellationToken ct = default);

    
    // Saves the notification row first, THEN pushes it over SignalR — the row
    // is the truth, the push is a convenience for whoever is online right now.
    // notificationTypeCode is resolved against NotificationType.Code (a lookup
    // table, same pattern as Role/Department/TicketStatus).
   
    Task NotifyAsync(
        Guid recipientUserId, string notificationTypeCode, string message,
        Guid? ticketId = null, CancellationToken ct = default);
}

public interface IReportService
{
    Task<ServiceResult<PagedResult<CategorySatisfactionDto>>> CategorySatisfactionAsync(
        PagedQuery query, CancellationToken ct = default);

    Task<ServiceResult<List<DailyVolumeDto>>> DailyVolumeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<ServiceResult<List<AgentWorkloadDto>>> AgentWorkloadAsync(
        CancellationToken ct = default);
}
