using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Hubs;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(IChatService chatService, IHubContext<ChatHub> hub)
    {
        _chatService = chatService;
        _hub = hub;
    }

    [HttpPost("tickets/{ticketId}/conversation")]
    public async Task<ActionResult<Guid>> GetOrCreateConversation(Guid ticketId)
    {
       
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      

        var result = await _chatService.GetOrCreateConversation(ticketId, userId);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return Ok(new { conversationId = result.Data });
    }
    
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<ActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] Guid? before,
        [FromQuery] int take = 20)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.GetMessages(conversationId, userId, before, take);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        var messages = result.Data!.Select(m => new
        {
            id = m.Id,
            conversationId = m.ConversationId,
            senderUserId = m.SenderUserId,
            body = m.Body,
            createdAt = m.CreatedAt
        });

        return Ok(messages);
    }
    [HttpGet("conversations")]
    public async Task<ActionResult> GetMyConversations()
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.GetMyConversations(userId);

        if (!result.IsSuccess)
            return StatusCode(
                result.StatusCode,
                new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
    
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<ActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.SendMessageAsync(conversationId, userId, request.Body);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        var response = new
        {
            id = result.Data!.Id,
            conversationId = result.Data.ConversationId,
            senderUserId = result.Data.SenderUserId,
            body = result.Data.Body,
            createdAt = result.Data.CreatedAt
        };

        await _hub.Clients.Group(conversationId.ToString())
            .SendAsync("ReceiveMessage", response);

        return StatusCode(StatusCodes.Status201Created, response);
    }
    
    [HttpPatch("conversations/{conversationId}/read")]
    public async Task<ActionResult> MarkConversationAsRead(
        Guid conversationId)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.MarkConversationAsReadAsync(
            conversationId,
            userId);

        if (!result.IsSuccess)
            return StatusCode(
                result.StatusCode,
                new { message = result.ErrorMessage });

        return Ok(new
        {
            conversationId,
            lastReadAt = result.Data
        });
    }
    [HttpGet("conversations/{conversationId}")]
    public async Task<ActionResult> GetConversationById(
        Guid conversationId)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.GetConversationByIdAsync(
            conversationId,
            userId);

        if (!result.IsSuccess)
            return StatusCode(
                result.StatusCode,
                new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
    [HttpPost("conversations/{conversationId}/participants")]
    public async Task<ActionResult> AddParticipant(
        Guid conversationId,
        [FromBody] AddParticipantRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _chatService.AddParticipantAsync(
            conversationId,
            userId,
            request.UserId);

        if (!result.IsSuccess)
            return StatusCode(
                result.StatusCode,
                new { message = result.ErrorMessage });

        return StatusCode(StatusCodes.Status201Created, new
        {
            conversationId,
            userId = request.UserId
        });
    }
    public class SendMessageRequest
    {
        public string Body { get; set; } = string.Empty;
    }
    public class AddParticipantRequest
    {
        public Guid UserId { get; set; }
    }
}