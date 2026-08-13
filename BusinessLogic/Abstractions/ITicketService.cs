
using BusinessLogic.Common;
﻿using Contract.Dtos;
using Contract.Paged;
using System;
using System.Collections.Generic;
using System.Text;
using BusinessLogic.Abstractions;
namespace BusinessLogic.Abstractions;

public interface ITicketService
{

    Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(CreateTicketDto dto, CancellationToken cancellationToken = default);


    Task<ServiceResult<TicketDetailDto>> GetTicketByIdAsync(Guid id,CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<TicketListItemDto>>> ListTicketsAsync(TicketQueryDto query,CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<TicketListItemDto>>> GetMyTicketsAsync(TicketQueryDto queryDto,CancellationToken cancellationToken = default);

    Task<ServiceResult<TicketDetailDto>> UpdateTicketAsync(Guid id,UpdateTicketDto dto,CancellationToken cancellationToken = default);

    Task<ServiceResult<TicketDetailDto>> UpdateTicketStatusAsync(Guid id,UpdateTicketStatusDto dto,CancellationToken cancellationToken = default);

    Task<ServiceResult<TicketDetailDto>> AssignTicketAsync(Guid id,AssignTicketDto dto,CancellationToken cancellationToken = default);

    Task<ServiceResult<TicketDetailDto>> AutoAssignTicketAsync(Guid id,CancellationToken cancellationToken = default);

    Task<ServiceResult<TicketDetailDto>> ReopenTicketAsync(Guid id,CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<TicketHistoryDto>>> GetTicketHistoryAsync(
    Guid ticketId,
    PagedQuery query,
    CancellationToken cancellationToken = default);


    Task<ServiceResult> DeleteTicketAsync(
    Guid id,
    CancellationToken cancellationToken = default);


    Task<ServiceResult<TicketStatisticsDto>> GetTicketStatisticsAsync(
    CancellationToken cancellationToken = default);
}
