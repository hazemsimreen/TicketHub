using System;
using System.Collections.Generic;
using System.Text;

namespace Contract.Dtos;

public class TicketDetailDto : TicketListItemDto
{
    public string Description { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}
