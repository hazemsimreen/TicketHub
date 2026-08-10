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


        // جديد — لازم للـ Access Filter بكل الـ services (مش بس Tickets)
        Guid? PrimaryDepartmentId { get; }
        IReadOnlyList<string> Roles { get; }
        bool IsInRole(string roleCode);
    }
}
