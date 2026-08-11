using Contract.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using BusinessLogic.ServiceResult;
using Contract.Dtos;
namespace BusinessLogic.Abstractions;

public interface ITicketService
{
  public Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(CreateTicketDto dto,CancellationToken cancellationToken = default);



}
