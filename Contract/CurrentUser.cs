using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic
{
    public class CurrentUser : ICurrentUser
    {
        public string? UserId => null;
        public string? UserName => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
        public Guid? PrimaryDepartmentId => null;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public bool IsInRole(string roleCode) => false;
    }
}
