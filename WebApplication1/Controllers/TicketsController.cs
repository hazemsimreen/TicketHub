using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private static readonly List<Ticket> Tickets =
        [
            new Ticket
            {
                Id = 1,
                Title = "Broken street light",
                Description = "The street light is not working.",
                Category = TicketCategory.Infrastructure,
                Priority = "High",
                Status = TicketStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            },

            new Ticket
            {
                Id = 2,
                Title = "Garbage collection problem",
                Description = "Garbage has not been collected.",
                Category = TicketCategory.Sanitation,
                Priority = "Medium",
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow
            }
        ];

        private static readonly List<Comment> Comments =
        [
            new Comment
            {
                Id = 1,
                TicketId = 1,
                Author = "Citizen",
                Text = "The problem is still not fixed.",
                CreatedAt = DateTime.UtcNow
            }
        ];

        
        [HttpGet]
        public ActionResult<IEnumerable<Ticket>> GetTickets(
            TicketStatus? status = null,
            TicketCategory? category = null)
        {
            IEnumerable<Ticket> result = Tickets;

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

            return Ok(result.ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Ticket> GetTicketById(int id)
        {
            var ticket = Tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }

        [HttpPost]
        public ActionResult<Ticket> CreateTicket(
            [FromBody] Ticket incomingTicket)
        {
            if (string.IsNullOrWhiteSpace(incomingTicket.Title) ||
                string.IsNullOrWhiteSpace(incomingTicket.Description) ||
                string.IsNullOrWhiteSpace(incomingTicket.Priority))
            {
                return BadRequest(
                    "Title, Description, and Priority are required.");
            }

            int newId;

            if (Tickets.Count == 0)
            {
                newId = 1;
            }
            else
            {
                newId = Tickets.Max(ticket => ticket.Id) + 1;
            }

            incomingTicket.Id = newId;
            incomingTicket.Status = TicketStatus.Open;
            incomingTicket.CreatedAt = DateTime.UtcNow;
            incomingTicket.UpdatedAt = null;

            Tickets.Add(incomingTicket);

            return CreatedAtAction(
                nameof(GetTicketById),
                new { id = incomingTicket.Id },
                incomingTicket);
        }

        [HttpPut("{id:int}")]
        public ActionResult<Ticket> UpdateTicket(
            int id,
            [FromBody] Ticket incomingTicket)
        {
            var existingTicket = Tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (existingTicket == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(incomingTicket.Title) ||
                string.IsNullOrWhiteSpace(incomingTicket.Description) ||
                string.IsNullOrWhiteSpace(incomingTicket.Priority))
            {
                return BadRequest(
                    "Title, Description, and Priority are required.");
            }

            existingTicket.Title = incomingTicket.Title;
            existingTicket.Description = incomingTicket.Description;
            existingTicket.Category = incomingTicket.Category;
            existingTicket.Priority = incomingTicket.Priority;
            existingTicket.UpdatedAt = DateTime.UtcNow;

            return Ok(existingTicket);
        }

        [HttpPut("{id:int}/status")]
        public ActionResult<Ticket> UpdateTicketStatus(
            int id,
            [FromBody] UpdateStatusRequest request)
        {
            var ticket = Tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = request.Status;
            ticket.UpdatedAt = DateTime.UtcNow;

            return Ok(ticket);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteTicket(int id)
        {
            var ticket = Tickets.FirstOrDefault(ticket =>
                ticket.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            Tickets.Remove(ticket);

            Comments.RemoveAll(comment =>
                comment.TicketId == id);

            return NoContent();
        }

        [HttpGet("{ticketId:int}/comments")]
        public ActionResult<IEnumerable<Comment>> GetComments(
            int ticketId)
        {
            var ticketExists = Tickets.Any(ticket =>
                ticket.Id == ticketId);

            if (!ticketExists)
            {
                return NotFound();
            }

            var ticketComments = Comments
                .Where(comment => comment.TicketId == ticketId)
                .ToList();

            return Ok(ticketComments);
        }

        [HttpPost("{ticketId:int}/comments")]
        public ActionResult<Comment> AddComment(
            int ticketId,
            [FromBody] Comment incomingComment)
        {
            var ticketExists = Tickets.Any(ticket =>
                ticket.Id == ticketId);

            if (!ticketExists)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(incomingComment.Author) ||
                string.IsNullOrWhiteSpace(incomingComment.Text))
            {
                return BadRequest(
                    "Author and Text are required.");
            }

            int newCommentId;

            if (Comments.Count == 0)
            {
                newCommentId = 1;
            }
            else
            {
                newCommentId =
                    Comments.Max(comment => comment.Id) + 1;
            }

            incomingComment.Id = newCommentId;
            incomingComment.TicketId = ticketId;
            incomingComment.CreatedAt = DateTime.UtcNow;

            Comments.Add(incomingComment);

            return StatusCode(
                StatusCodes.Status201Created,
                incomingComment);
        }

        [HttpGet("/api/categories")]
        public ActionResult<IEnumerable<CategorySummary>> GetCategories()
        {
            var categories = Enum.GetValues<TicketCategory>()
                .Select(category => new CategorySummary
                {
                    Category = category.ToString(),

                    TicketCount = Tickets.Count(ticket =>
                        ticket.Category == category)
                })
                .ToList();

            return Ok(categories);
        }
    }
}