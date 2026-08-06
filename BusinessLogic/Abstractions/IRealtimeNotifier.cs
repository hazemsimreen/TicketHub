using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Abstractions;

public interface IRealtimeNotifier
{
    Task NotifyAsync(
        string userId,
        string message);
}