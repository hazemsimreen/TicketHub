using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataAccess.Models
{
    public class UpdateStatusRequest
    {
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Status { get; set; }
    }
}

