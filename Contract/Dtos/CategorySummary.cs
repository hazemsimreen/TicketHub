namespace Contract.Dtos
{
    public class CategorySummary
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TicketCount { get; set; }
    }
}