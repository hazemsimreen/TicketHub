using Contract.Dtos;
using DataAccess.Models;

namespace BusinessLogic.Services
{
    public interface ITicketService
    {
        IReadOnlyList<Ticket> GetTickets(
            int? statusId,
            int? categoryId);

        Ticket? GetTicketById(int id);

        Ticket CreateTicket(CreateTicketDto request);

        Ticket? UpdateTicket(
            int id,
            UpdateTicketDto request);

        Ticket? UpdateTicketStatus(
            int id,
            int statusId);

        bool DeleteTicket(int id);

        IReadOnlyList<TicketComment>? GetComments(int ticketId);

        TicketComment? AddComment(
            int ticketId,
            CreateCommentDto request);

        IReadOnlyList<CategorySummary> GetCategories();
    }
}