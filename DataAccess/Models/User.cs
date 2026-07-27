namespace DataAccess.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string UserType { get; set; } = string.Empty;

        // FK → Department
        public int? PrimaryDepartmentId { get; set; }

        // Navigation Properties
        public Department? PrimaryDepartment { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<TicketWatcher> TicketWatchers { get; set; } = new List<TicketWatcher>();
        public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
        public ICollection<ConversationMessage> ConversationMessages { get; set; } = new List<ConversationMessage>();
    }
}
