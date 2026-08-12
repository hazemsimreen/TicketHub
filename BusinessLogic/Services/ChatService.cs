using BusinessLogic.Common;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Contract.Dtos;
using Microsoft.AspNetCore.Identity;
namespace BusinessLogic.Services;
using Result = BusinessLogic.Common.ServiceResult;

public interface IChatService
{
    Task<Result> LeaveConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default);
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
    Task<ServiceResult<Guid>> DeleteMessageAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken ct = default);
    Task<ServiceResult<Guid>> CreateConversationAsync(
        Guid ticketId,
        Guid requestingUserId,
        List<Guid> participantIds,
        CancellationToken ct = default);
    


}


public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;

    public ChatService(
        IUnitOfWork uow,
        UserManager<User> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }
    private async Task<bool> IsActiveParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        return await _uow.Repository<ConversationParticipant>()
            .Query()
            .AnyAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == userId &&
                     !p.IsDeleted,
                ct);
    }
    public async Task<ServiceResult<Guid>> CreateConversationAsync(
        Guid ticketId,
        Guid requestingUserId,
        List<Guid> participantIds,
        CancellationToken ct = default)
    {
        var ticket = await _uow.Repository<Ticket>()
            .Query()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return ServiceResult<Guid>.NotFound("Ticket not found.");

        var canAccess =
            ticket.SubmittedByUserId == requestingUserId ||
            await _uow.Repository<UserRole>()
                .Query()
                .AnyAsync(
                    ur => ur.UserId == requestingUserId &&
                          ur.DepartmentId == ticket.DepartmentId,
                    ct);

        if (!canAccess)
            return ServiceResult<Guid>.Forbidden(
                "You cannot create a conversation for this ticket.");

        var alreadyExists = await _uow.Repository<Conversation>()
            .Query()
            .AnyAsync(c => c.TicketId == ticketId, ct);

        if (alreadyExists)
            return ServiceResult<Guid>.Conflict(
                "A conversation already exists for this ticket.");

        var usersToAdd = participantIds
            .Append(requestingUserId)
            .Distinct()
            .ToList();

        var existingUserIds = await _userManager.Users
            .Where(u => usersToAdd.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (existingUserIds.Count != usersToAdd.Count)
        {
            return ServiceResult<Guid>
                .NotFound("One or more participants were not found.");
        }

        var conversationId = Guid.NewGuid();

        var conversation = new Conversation
        {
            Id = conversationId,
            TicketId = ticketId
        };

        await _uow.Repository<Conversation>()
            .AddAsync(conversation, ct);

        foreach (var userId in usersToAdd)
        {
            var participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId
            };

            await _uow.Repository<ConversationParticipant>()
                .AddAsync(participant, ct);
        }

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<Guid>.Created(conversationId);
    }
    public async Task<ServiceResult<Guid>> DeleteMessageAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken ct = default)
    {
        var message = await _uow.Repository<ConversationMessage>()
            .Query()
            .FirstOrDefaultAsync(
                m => m.Id == messageId && !m.IsDeleted,
                ct);

        if (message is null)
            return ServiceResult<Guid>.NotFound(
                "Message not found.");

        var isSender = message.SenderUserId == requestingUserId;

        var isAdmin = await _uow.Repository<UserRole>()
            .Query()
            .AnyAsync(
                ur => ur.UserId == requestingUserId &&
                      !ur.IsDeleted &&
                      ur.Role.Code == "Admin",
                ct);

        if (!isSender && !isAdmin)
            return ServiceResult<Guid>.Forbidden(
                "You cannot delete this message.");

        var conversationId = message.ConversationId;

        _uow.Repository<ConversationMessage>()
            .Remove(message);

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<Guid>.Success(conversationId);
    }
    public async Task<Result> LeaveConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var participant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .FirstOrDefaultAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == userId &&
                     !p.IsDeleted,
                ct);

        if (participant is null)
            return Result.NotFound("Conversation not found.");

        _uow.Repository<ConversationParticipant>()
            .Remove(participant);

        await _uow.SaveChangesAsync(ct);

        return Result.NoContent();
    }

   
    public async Task<ServiceResult<bool>> AddParticipantAsync(
        Guid conversationId,
        Guid requestingUserId,
        Guid userIdToAdd,
        CancellationToken ct = default)
    {
        var conversationExists = await _uow.Repository<Conversation>()
            .Query()
            .AnyAsync(
                c => c.Id == conversationId,
                ct);

        if (!conversationExists)
        {
            return ServiceResult<bool>
                .NotFound("Conversation not found.");
        }

        var requesterIsParticipant = await IsActiveParticipantAsync(
            conversationId,
            requestingUserId,
            ct);

        if (!requesterIsParticipant)
        {
            return ServiceResult<bool>
                .NotFound("Conversation not found.");
        }
        var userToAdd = await _userManager.FindByIdAsync(
            userIdToAdd.ToString());

        if (userToAdd is null)
        {
            return ServiceResult<bool>
                .NotFound("User not found.");
        }

        var existingParticipant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .FirstOrDefaultAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == userIdToAdd,
                ct);

        if (existingParticipant is not null)
        {
            if (!existingParticipant.IsDeleted)
            {
                return ServiceResult<bool>
                    .Conflict("User is already a participant.");
            }

            existingParticipant.IsDeleted = false;
            existingParticipant.DeletedAt = null;
            existingParticipant.DeletedBy = null;
            existingParticipant.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>
                .Created(true);
        }

        var participant = new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userIdToAdd
        };

        await _uow.Repository<ConversationParticipant>()
            .AddAsync(participant, ct);

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<bool>
            .Created(true);
    }
    public async Task<ServiceResult<ConversationDetailsDto>> GetConversationByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var conversation = await _uow.Repository<Conversation>()
            .Query()
            .Include(c => c.Ticket)
            .Include(c => c.Participants.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return ServiceResult<ConversationDetailsDto>
                .NotFound("Conversation not found.");

        var isParticipant = conversation.Participants
            .Any(p => p.UserId == userId);

        if (!isParticipant)
            return ServiceResult<ConversationDetailsDto>
                .NotFound("Conversation not found.");

        var dto = new ConversationDetailsDto
        {
            Id = conversation.Id,
            TicketId = conversation.TicketId,
            TicketTitle = conversation.Ticket.Title,
            CreatedAt = conversation.CreatedAt,

            Participants = conversation.Participants
                .Select(p => new ConversationParticipantDto
                {
                    UserId = p.UserId,
                    LastReadAt = p.LastReadAt
                })
                .ToList()
        };

        return ServiceResult<ConversationDetailsDto>.Success(dto);
    }

    public async Task<ServiceResult<ConversationMessage>> SendMessageAsync(
        Guid conversationId,
        Guid senderUserId,
        string body,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ServiceResult<ConversationMessage>
                .BadRequest("Message cannot be empty.");
        }

        var conversationExists = await _uow.Repository<Conversation>()
            .Query()
            .AnyAsync(
                c => c.Id == conversationId,
                ct);

        if (!conversationExists)
        {
            return ServiceResult<ConversationMessage>
                .NotFound("Conversation not found.");
        }

        var isParticipant = await IsActiveParticipantAsync(
            conversationId,
            senderUserId,
            ct);

        if (!isParticipant)
        {
            return ServiceResult<ConversationMessage>
                .NotFound("Conversation not found.");
        }

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Body = body.Trim(),
            IsSystemGenerated = false
        };

        await _uow.Repository<ConversationMessage>()
            .AddAsync(message, ct);

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<ConversationMessage>
            .Created(message);
    }

   public async Task<ServiceResult<Guid>> GetOrCreateConversation(
    Guid ticketId,
    Guid userId,
    CancellationToken ct = default)
{
    var ticket = await _uow.Repository<Ticket>()
        .Query()
        .FirstOrDefaultAsync(
            t => t.Id == ticketId,
            ct);

    if (ticket is null)
    {
        return ServiceResult<Guid>
            .NotFound("Ticket not found.");
    }

    var canAccess =
        ticket.SubmittedByUserId == userId ||
        await _uow.Repository<UserRole>()
            .Query()
            .AnyAsync(
                ur => ur.UserId == userId &&
                      ur.DepartmentId == ticket.DepartmentId,
                ct);

    if (!canAccess)
    {
        return ServiceResult<Guid>
            .Forbidden("You cannot access this ticket's conversation.");
    }

    var existingConversation = await _uow.Repository<Conversation>()
        .Query()
        .FirstOrDefaultAsync(
            c => c.TicketId == ticketId,
            ct);

    if (existingConversation is not null)
    {
        var existingParticipant =
            await _uow.Repository<ConversationParticipant>()
                .Query()
                .FirstOrDefaultAsync(
                    p => p.ConversationId == existingConversation.Id &&
                         p.UserId == userId,
                    ct);

        if (existingParticipant is null)
        {
            var participant = new ConversationParticipant
            {
                ConversationId = existingConversation.Id,
                UserId = userId
            };

            await _uow.Repository<ConversationParticipant>()
                .AddAsync(participant, ct);

            await _uow.SaveChangesAsync(ct);
        }
        else if (existingParticipant.IsDeleted)
        {
            existingParticipant.IsDeleted = false;
            existingParticipant.DeletedAt = null;
            existingParticipant.DeletedBy = null;
            existingParticipant.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }

        return ServiceResult<Guid>
            .Success(existingConversation.Id);
    }

    var conversation = new Conversation
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId
    };

    await _uow.Repository<Conversation>()
        .AddAsync(conversation, ct);

    var newParticipant = new ConversationParticipant
    {
        ConversationId = conversation.Id,
        UserId = userId
    };

    await _uow.Repository<ConversationParticipant>()
        .AddAsync(newParticipant, ct);

    await _uow.SaveChangesAsync(ct);

    return ServiceResult<Guid>
        .Success(conversation.Id);
}


    public async Task<ServiceResult<List<ConversationMessage>>> GetMessages(
        Guid conversationId,
        Guid userId,
        Guid? beforeMessageId,
        int take = 20,
        CancellationToken ct = default)
    {
        var conversationExists = await _uow.Repository<Conversation>()
            .Query()
            .AnyAsync(
                c => c.Id == conversationId,
                ct);

        if (!conversationExists)
        {
            return ServiceResult<List<ConversationMessage>>
                .NotFound("Conversation not found.");
        }

        var isParticipant = await IsActiveParticipantAsync(
            conversationId,
            userId,
            ct);

        if (!isParticipant)
        {
            return ServiceResult<List<ConversationMessage>>
                .NotFound("Conversation not found.");
        }

        var query = _uow.Repository<ConversationMessage>()
            .Query()
            .Where(m =>
                m.ConversationId == conversationId &&
                !m.IsDeleted);

        if (beforeMessageId is not null)
        {
            var cursor = await _uow.Repository<ConversationMessage>()
                .Query()
                .FirstOrDefaultAsync(
                    m => m.Id == beforeMessageId &&
                         m.ConversationId == conversationId &&
                         !m.IsDeleted,
                    ct);

            if (cursor is not null)
            {
                query = query.Where(
                    m => m.CreatedAt < cursor.CreatedAt);
            }
        }

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return ServiceResult<List<ConversationMessage>>
            .Success(messages);
    }

    public async Task<ServiceResult<List<ConversationSummaryDto>>> GetMyConversations(
        Guid userId,
        CancellationToken ct = default)
    {
        var conversations = await _uow.Repository<Conversation>()
            .Query()
            .Where(c =>
                c.Participants.Any(
                    p => p.UserId == userId &&
                         !p.IsDeleted))
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                TicketTitle = c.Ticket.Title,
                CreatedAt = c.CreatedAt,

                LastMessage = c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),

                LastMessageAt = c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefault(),

                UnreadCount = c.Messages.Count(m =>
                    !m.IsDeleted &&
                    m.SenderUserId != userId &&
                    m.CreatedAt >
                    (
                        c.Participants
                            .Where(p =>
                                p.UserId == userId &&
                                !p.IsDeleted)
                            .Select(p => p.LastReadAt)
                            .FirstOrDefault()
                        ?? DateTime.MinValue
                    ))
            })
            .OrderByDescending(c =>
                c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(ct);

        return ServiceResult<List<ConversationSummaryDto>>
            .Success(conversations);
    }
    public async Task<ServiceResult<DateTime>> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var conversationExists = await _uow.Repository<Conversation>()
            .Query()
            .AnyAsync(
                c => c.Id == conversationId,
                ct);

        if (!conversationExists)
        {
            return ServiceResult<DateTime>
                .NotFound("Conversation not found.");
        }

        var participant = await _uow.Repository<ConversationParticipant>()
            .Query()
            .FirstOrDefaultAsync(
                p => p.ConversationId == conversationId &&
                     p.UserId == userId &&
                     !p.IsDeleted,
                ct);

        if (participant is null)
        {
            return ServiceResult<DateTime>
                .NotFound("Conversation not found.");
        }

        var readAt = DateTime.UtcNow;

        participant.LastReadAt = readAt;
        participant.UpdatedAt = readAt;

        await _uow.SaveChangesAsync(ct);

        return ServiceResult<DateTime>
            .Success(readAt);
    }
   
}