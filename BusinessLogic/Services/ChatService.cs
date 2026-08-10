using BusinessLogic.ServiceResult;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;

namespace BusinessLogic.Services;

public interface IChatService
{
    Task<ServiceResult<ConversationMessage>> SendMessageAsync(
        Guid conversationId,
        Guid senderUserId,
        string body,
        CancellationToken ct = default);

    Task<ServiceResult<Guid>> GetOrCreateConversation(
        Guid ticketId, Guid requestingUserId, CancellationToken ct = default);

    Task<ServiceResult<List<ConversationMessage>>> GetMessages(
        Guid conversationId, Guid userId, Guid? beforeMessageId, int take = 20, CancellationToken ct = default);

    Task<ServiceResult<List<Conversation>>> GetMyConversations(Guid userId, CancellationToken ct = default);
}

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
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

    public async Task<ServiceResult<List<Conversation>>> GetMyConversations(Guid userId, CancellationToken ct = default)
    {
        var myDepartmentIds = await _uow.Repository<UserRole>()
            .Query()
            .Where(ur => ur.UserId == userId && ur.DepartmentId != null)
            .Select(ur => ur.DepartmentId!.Value)
            .ToListAsync(ct);

        var conversations = await _uow.Repository<Conversation>()
            .Query()
            .Include(c => c.Ticket)
            .Where(c => c.Ticket.SubmittedByUserId == userId
                        || myDepartmentIds.Contains(c.Ticket.DepartmentId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return ServiceResult<List<Conversation>>.Success(conversations);
    }
}