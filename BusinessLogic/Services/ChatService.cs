using BusinessLogic.Common;
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
}