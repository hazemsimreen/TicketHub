using BusinessLogic.Abstractions;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Hubs;

namespace WebApplication1.Realtime;

public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(string userId, string message)
    {
        await _hubContext.Clients.User(userId).SendAsync("Notify", message);
    }
}