using Contract.Dtos;
using DataAccess.Models;
using System.Net.Sockets;
using System.Xml.Linq;

namespace BusinessLogic.Services
{
    public interface ITicketService
    {
        IReadOnlyList<Ticket> GetTickets(
            TicketStatus? status,
            TicketCategory? category);

        Ticket? GetTicketById(int id);

        Ticket CreateTicket(CreateTicketDto request);

        Ticket? UpdateTicket(
            int id,
            UpdateTicketDto request);

        Ticket? UpdateTicketStatus(
            int id,
            TicketStatus status);

        bool DeleteTicket(int id);

        IReadOnlyList<Comment>? GetComments(int ticketId);

        Comment? AddComment(
            int ticketId,
            CreateCommentDto request);

        IReadOnlyList<CategorySummary> GetCategories();
    }
}