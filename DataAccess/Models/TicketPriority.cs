namespace DataAccess.Models
{
    public class TicketPriority
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        // Navigation Properties
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
