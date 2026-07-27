using Contract.Dtos;
using DataAccess.Models;
using System.Net.Sockets;
using System.Xml.Linq;

namespace BusinessLogic.Services
{
    public class TicketService : ITicketService
    {
        private readonly List<Ticket> _tickets =
            new List<Ticket>
            {
                new Ticket
                {
                    Id = 1,
                    Title = "Broken street light",
                    Description =
                        "The street light is not working.",
                    Category =
                        TicketCategory.Infrastructure,
                    Priority = "High",
                    Status = TicketStatus.InProgress,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                },

                new Ticket
                {
                    Id = 2,
                    Title = "Garbage collection problem",
                    Description =
                        "Garbage has not been collected.",
                    Category =
                        TicketCategory.Sanitation,
                    Priority = "Medium",
                    Status = TicketStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                }
            };

        private readonly List<Comment> _comments =
            new List<Comment>
            {
                new Comment
                {
                    Id = 1,
                    TicketId = 1,
                    Author = "Citizen",
                    Text = "The problem is still not fixed.",
                    CreatedAt = DateTime.UtcNow
                }
            };

        public IReadOnlyList<Ticket> GetTickets(
            TicketStatus? status,
            TicketCategory? category)
        {
            IEnumerable<Ticket> result = _tickets;

            if (status.HasValue)
            {
                result = result.Where(ticket =>
                    ticket.Status == status.Value);
            }

            if (category.HasValue)
            {
                result = result.Where(ticket =>
                    ticket.Category == category.Value);
            }

            return result.ToList();
        }

        public Ticket? GetTicketById(int id)
        {
            return _tickets.FirstOrDefault(ticket =>
                ticket.Id == id);
        }

        public Ticket CreateTicket(CreateTicketDto request)
        {
            int newId;

            if (_tickets.Count == 0)
            {
                newId = 1;
            }
            else
            {
                newId = _tickets.Max(ticket =>
                    ticket.Id) + 1;
            }

            var ticket = new Ticket
            {
                Id = newId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _tickets.Add(ticket);

            return ticket;
        }

        public Ticket? UpdateTicket(
            int id,
            UpdateTicketDto request)
        {
            var ticket = _tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return null;
            }

            ticket.Title = request.Title;
            ticket.Description = request.Description;
            ticket.Category = request.Category;
            ticket.Priority = request.Priority;
            ticket.UpdatedAt = DateTime.UtcNow;

            return ticket;
        }

        public Ticket? UpdateTicketStatus(
            int id,
            TicketStatus status)
        {
            var ticket = _tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return null;
            }

            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;

            return ticket;
        }

        public bool DeleteTicket(int id)
        {
            var ticket = _tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return false;
            }

            _tickets.Remove(ticket);

            _comments.RemoveAll(comment =>
                comment.TicketId == id);

            return true;
        }

        public IReadOnlyList<Comment>? GetComments(
            int ticketId)
        {
            var ticketExists = _tickets.Any(ticket =>
                ticket.Id == ticketId);

            if (!ticketExists)
            {
                return null;
            }

            return _comments
                .Where(comment =>
                    comment.TicketId == ticketId)
                .ToList();
        }

        public Comment? AddComment(
            int ticketId,
            CreateCommentDto request)
        {
            var ticketExists = _tickets.Any(ticket =>
                ticket.Id == ticketId);

            if (!ticketExists)
            {
                return null;
            }

            int newCommentId;

            if (_comments.Count == 0)
            {
                newCommentId = 1;
            }
            else
            {
                newCommentId = _comments.Max(comment =>
                    comment.Id) + 1;
            }

            var comment = new Comment
            {
                Id = newCommentId,
                TicketId = ticketId,
                Author = request.Author,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow
            };

            _comments.Add(comment);

            return comment;
        }

        public IReadOnlyList<CategorySummary> GetCategories()
        {
            return Enum.GetValues<TicketCategory>()
                .Select(category => new CategorySummary
                {
                    Category = category.ToString(),

                    TicketCount = _tickets.Count(ticket =>
                        ticket.Category == category)
                })
                .ToList();
        }
    }
}