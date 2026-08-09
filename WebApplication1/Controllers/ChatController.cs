using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
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
}