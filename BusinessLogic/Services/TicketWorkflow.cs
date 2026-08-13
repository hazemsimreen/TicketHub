using BusinessLogic.Abstractions;

namespace BusinessLogic.Services;

public class TicketWorkflow : ITicketWorkflow
{
    private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Open"] = ["InProgress", "OnHold", "Cancelled"],
        ["InProgress"] = ["OnHold", "Resolved", "Cancelled"],
        ["OnHold"] = ["InProgress", "Cancelled"],
        ["Resolved"] = ["Closed", "InProgress"],
        ["Closed"] = [],
        ["Cancelled"] = []
    };

    public bool CanTransition(string fromStatusCode, string toStatusCode)
    {
        if (!Allowed.TryGetValue(fromStatusCode, out var allowedTargets))
            return false;

        return allowedTargets.Contains(toStatusCode, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetAllowedTransitions(string fromStatusCode)
    {
        return Allowed.TryGetValue(fromStatusCode, out var allowedTargets)
            ? allowedTargets
            : Array.Empty<string>();
    }
}