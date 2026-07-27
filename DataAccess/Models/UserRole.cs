namespace DataAccess.Models
{
    public class UserRole
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }

        public int? DepartmentId { get; set; }

        // Navigation Properties
        public Role Role { get; set; } = null!;
    }
}
