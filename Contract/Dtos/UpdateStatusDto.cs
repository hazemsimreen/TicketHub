using DataAccess.Models;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Dtos
{
    public class UpdateStatusDto
    {
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Status { get; set; } = null!;
    }
}