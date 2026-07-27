using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.Dtos
{
    public class UpdateStatusDto
    {
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Status { get; set; }
    }
}