using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
    }
}
