using System.ComponentModel.DataAnnotations;


namespace Contract.Dtos
{
    public class UpdateStatusDto
    {
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Status { get; set; }
    }
}