using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        public bool IsDepartmentScoped { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
