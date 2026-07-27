namespace DataAccess.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public int? DefaultPriorityId { get; set; }

        public Department Department { get; set; } = null!;
        public TicketPriority? DefaultPriority { get; set; }
    }
}
