using System;
using System.Collections.Generic;
using System.Text;

namespace Contract.Dtos
{
    public class TicketStatusCountDto
    {
        public int StatusId { get; init; }

        public string StatusCode { get; init; } = string.Empty;

        public int Count { get; init; }
    }

}
