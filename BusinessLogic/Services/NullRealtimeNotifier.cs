using System;
using System.Collections.Generic;
using System.Text;

using BusinessLogic.Abstractions;

namespace BusinessLogic.Services;

public class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyAsync(
        string userId,
        string message)
    {
        return Task.CompletedTask;
    }
}