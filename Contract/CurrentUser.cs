using System;
using System.Collections.Generic;

namespace BusinessLogic
{
    public class CurrentUser : ICurrentUser
    {
        public string? UserId => null;
        public string? UserName => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
        public Guid? DepartmentId => null;
        public int? PrimaryDepartmentId => null;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public bool IsInRole(string roleCode) => false;
    }
}
