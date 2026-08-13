namespace Contract.Dtos;

public class TicketHistoryDto
{
    public Guid Id { get; set; }

    // StatusChanged أو FieldChanged
    public string Type { get; set; } = string.Empty;

    public string? FieldName { get; set; }

    public string? FromStatusCode { get; set; }

    public string? ToStatusCode { get; set; }

    public string ChangedByName { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }
}