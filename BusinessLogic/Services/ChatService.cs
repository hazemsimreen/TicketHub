using BusinessLogic.ServiceResult;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Contract.Dtos;
namespace BusinessLogic.Services;

public interface IChatService
{
    Task<ServiceResult<bool>> AddParticipantAsync(
        Guid conversationId,
        Guid requestingUserId,
        Guid userIdToAdd,
        CancellationToken ct = default);
    Task<ServiceResult<ConversationMessage>> SendMessageAsync(
        Guid conversationId,
        Guid senderUserId,
        string body,
        CancellationToken ct = default);

    Task<ServiceResult<Guid>> GetOrCreateConversation(
        Guid ticketId, Guid requestingUserId, CancellationToken ct = default);

    Task<ServiceResult<List<ConversationMessage>>> GetMessages(
        Guid conversationId, Guid userId, Guid? beforeMessageId, int take = 20, CancellationToken ct = default);

    Task<ServiceResult<List<ConversationSummaryDto>>> GetMyConversations(
        Guid userId,
        CancellationToken ct = default);


    Task<ServiceResult<DateTime>> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default);

    Task<ServiceResult<ConversationDetailsDto>> GetConversationByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default);
}

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<ServiceResult<bool>> AddParticipantAsync(
        Guid conversationId,
        Guid requestingUserId,
        Guid userIdToAdd,
        CancellationToken ct = default)
    {
        var callerIsParticipant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .AnyAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == requestingUserId,
                ct);

        if (!callerIsParticipant)
            return ServiceResult<bool>.NotFound("Conversation not found.");

        var alreadyParticipant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .AnyAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == userIdToAdd,
                ct);

        if (alreadyParticipant)
            return ServiceResult<bool>.Conflict(
                "User is already a participant.");

        var participant = new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userIdToAdd
        };

        await _uow.Repository<ConversationParticipant>()
            .AddAsync(participant, ct);

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<bool>.Created(true);
    }

    public async Task<ServiceResult<ConversationDetailsDto>> GetConversationByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var conversation = await _uow.Repository<Conversation>()
            .Query()
            .Where(c =>
                c.Id == conversationId &&
                c.Participants.Any(p => p.UserId == userId))
            .Select(c => new ConversationDetailsDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                TicketTitle = c.Ticket.Title,
                CreatedAt = c.CreatedAt,

                Participants = c.Participants
                    .Select(p => new ConversationParticipantDto
                    {
                        UserId = p.UserId,
                        LastReadAt = p.LastReadAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
            return ServiceResult<ConversationDetailsDto>
                .NotFound("Conversation not found.");

        return ServiceResult<ConversationDetailsDto>
            .Success(conversation);
    }

    public async Task<ServiceResult<ConversationMessage>> SendMessageAsync(
        Guid conversationId,
        Guid senderUserId,
        string body,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return ServiceResult<ConversationMessage>.BadRequest("Message cannot be empty.");

        var conversation = await _uow.Repository<Conversation>()
            .Query()
            .Include(c => c.Ticket)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return ServiceResult<ConversationMessage>.NotFound("Conversation not found.");

        var canSend = conversation.Ticket.SubmittedByUserId == senderUserId
            || await _uow.Repository<UserRole>()
                .Query()
                .AnyAsync(ur => ur.UserId == senderUserId
                             && ur.DepartmentId == conversation.Ticket.DepartmentId, ct);

        if (!canSend)
            return ServiceResult<ConversationMessage>.Forbidden("You cannot post in this conversation.");

        var message = new ConversationMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Body = body,
            IsSystemGenerated = false
        };

        await _uow.Repository<ConversationMessage>().AddAsync(message, ct);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<ConversationMessage>.Created(message);
    }

    public async Task<ServiceResult<Guid>> GetOrCreateConversation(
        Guid ticketId, Guid userId, CancellationToken ct = default)
    {
        var ticket = await _uow.Repository<Ticket>()
            .Query()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return ServiceResult<Guid>.NotFound("Ticket not found.");

        var canAccess = ticket.SubmittedByUserId == userId
                        || await _uow.Repository<UserRole>()
                            .Query()
                            .AnyAsync(ur => ur.UserId == userId && ur.DepartmentId == ticket.DepartmentId, ct);

        if (!canAccess)
            return ServiceResult<Guid>.Forbidden("You cannot access this ticket's conversation.");

        var existing = await _uow.Repository<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.TicketId == ticketId, ct);

        if (existing is not null)
            return ServiceResult<Guid>.Success(existing.Id);

        var conversation = new Conversation { TicketId = ticketId };
        await _uow.Repository<Conversation>().AddAsync(conversation, ct);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<Guid>.Success(conversation.Id);
    }


    public async Task<ServiceResult<List<ConversationMessage>>> GetMessages(
        Guid conversationId, Guid userId, Guid? beforeMessageId, int take = 20, CancellationToken ct = default)
    {
        var conversation = await _uow.Repository<Conversation>()
            .Query()
            .Include(c => c.Ticket)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return ServiceResult<List<ConversationMessage>>.NotFound("Conversation not found.");

        var canAccess = conversation.Ticket.SubmittedByUserId == userId
                        || await _uow.Repository<UserRole>()
                            .Query()
                            .AnyAsync(ur => ur.UserId == userId && ur.DepartmentId == conversation.Ticket.DepartmentId, ct);


        if (!canAccess)
            return ServiceResult<List<ConversationMessage>>.NotFound("Conversation not found.");

        var query = _uow.Repository<ConversationMessage>()
            .Query()
            .Where(m => m.ConversationId == conversationId);

        if (beforeMessageId is not null)
        {
            var cursor = await _uow.Repository<ConversationMessage>()
                .Query()
                .FirstOrDefaultAsync(m => m.Id == beforeMessageId, ct);

            if (cursor is not null)
                query = query.Where(m => m.CreatedAt < cursor.CreatedAt);
        }

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return ServiceResult<List<ConversationMessage>>.Success(messages);
    }

    public async Task<ServiceResult<List<ConversationSummaryDto>>> GetMyConversations(
        Guid userId,
        CancellationToken ct = default)
    {
        var myDepartmentIds = await _uow.Repository<UserRole>()
            .Query()
            .Where(ur => ur.UserId == userId && ur.DepartmentId != null)
            .Select(ur => ur.DepartmentId!.Value)
            .ToListAsync(ct);

        var conversations = await _uow.Repository<Conversation>()
            .Query()
            .Where(c =>
                c.Ticket.SubmittedByUserId == userId ||
                myDepartmentIds.Contains(c.Ticket.DepartmentId))
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                TicketTitle = c.Ticket.Title,
                CreatedAt = c.CreatedAt,

                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),

                LastMessageAt = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefault(),

                UnreadCount = c.Messages.Count(m =>
                    m.SenderUserId != userId &&
                    m.CreatedAt >
                    (
                        c.Participants
                            .Where(p => p.UserId == userId)
                            .Select(p => p.LastReadAt)
                            .FirstOrDefault()
                        ?? DateTime.MinValue
                    ))
            })
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(ct);

        return ServiceResult<List<ConversationSummaryDto>>
            .Success(conversations);
    }
    public async Task<ServiceResult<DateTime>> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var conversation = await _uow.Repository<Conversation>()
            .Query()
            .Include(c => c.Ticket)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return ServiceResult<DateTime>.NotFound("Conversation not found.");

        var canAccess =
            conversation.Ticket.SubmittedByUserId == userId
            || await _uow.Repository<UserRole>()
                .Query()
                .AnyAsync(
                    ur => ur.UserId == userId
                          && ur.DepartmentId == conversation.Ticket.DepartmentId,
                    ct);

        if (!canAccess)
            return ServiceResult<DateTime>.Forbidden(
                "You cannot access this conversation.");

        var participant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .FirstOrDefaultAsync(
                p => p.ConversationId == conversationId
                     && p.UserId == userId,
                ct);

        var readAt = DateTime.UtcNow;

        if (participant is null)
        {
            participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                LastReadAt = readAt
            };

            await _uow.Repository<ConversationParticipant>()
                .AddAsync(participant, ct);
        }
        else
        {
            participant.LastReadAt = readAt;
            participant.UpdatedAt = readAt;
        }

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<DateTime>.Success(readAt);
    }
}