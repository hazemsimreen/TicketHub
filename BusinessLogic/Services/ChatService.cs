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
}