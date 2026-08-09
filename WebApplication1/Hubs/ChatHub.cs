using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }
    public async Task SendMessage(Guid conversationId, string body)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);
        var result = await _chatService.SendMessageAsync(conversationId, userId, body);

        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.ErrorMessage);
            return;
        }

        await Clients.Group(conversationId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                id = result.Data!.Id,
                conversationId = result.Data.ConversationId,
                senderUserId = result.Data.SenderUserId,
                body = result.Data.Body,
                createdAt = result.Data.CreatedAt
            });
    }
    

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }
}