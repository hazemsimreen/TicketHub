using BusinessLogic.Common;
using Contract.Dtos;

namespace BusinessLogic.Abstractions;

public interface ITicketService
{
    Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(CreateTicketDto dto, CancellationToken cancellationToken = default);
}
