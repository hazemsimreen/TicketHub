using DataAccess.Models;

namespace BusinessLogic.Services;

/// <summary>
/// Mirrors TicketService's private ApplyAccessFilter (Role 3) so Comment/
/// Attachment/Rating services enforce the exact same "can this user even see
/// this ticket" rule before doing anything ticket-scoped — Admin sees
/// everything, Supervisor sees their department, Agent sees what's assigned
/// to them, Citizen sees what they submitted, anything else is fail-closed.
///
/// NOTE for whoever owns Role 3: TicketService.ApplyAccessFilter itself
/// checks IsInRole("DepartmentHead") / IsInRole("Employee") — but the actual
/// seeded Role.Code values in the database are "Supervisor" / "Agent" (see
/// AppRoles in Contracts.Security, and DbSeeder). That mismatch means a real
/// Supervisor or Agent currently falls through every branch in
/// TicketService and hits "Unknown Role — fail closed", i.e. they see ZERO
/// tickets today. This helper intentionally uses the correct/current
/// "Supervisor" / "Agent" so Role 4 doesn't inherit that bug, but it's worth
/// flagging to Role 3 — same fix either way (align on one set of names).
/// </summary>
public static class TicketVisibility
{
    public static IQueryable<Ticket> Apply(IQueryable<Ticket> tickets, ICurrentUser user)
    {
        if (user.IsInRole("Admin"))
        {
            return tickets;
        }

        if (user.IsInRole("Supervisor"))
        {
            if (user.PrimaryDepartmentId is null)
            {
                return tickets.Where(_ => false);
            }

            return tickets.Where(t => t.DepartmentId == user.PrimaryDepartmentId.Value);
        }

        if (user.IsInRole("Agent"))
        {
            if (user.UserId is null || !Guid.TryParse(user.UserId, out var agentId))
            {
                return tickets.Where(_ => false);
            }

            return tickets.Where(t => t.AssignedToUserId == agentId);
        }

        if (user.IsInRole("Citizen"))
        {
            if (user.UserId is null || !Guid.TryParse(user.UserId, out var citizenId))
            {
                return tickets.Where(_ => false);
            }

            return tickets.Where(t => t.SubmittedByUserId == citizenId);
        }

        // Unknown role — fail closed.
        return tickets.Where(_ => false);
    }

    public static bool IsStaff(ICurrentUser user) =>
        user.IsInRole("Admin") || user.IsInRole("Supervisor") || user.IsInRole("Agent");
}
