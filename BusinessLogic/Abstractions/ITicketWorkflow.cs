namespace BusinessLogic.Abstractions;

public interface ITicketWorkflow
{
    bool CanTransition(string fromStatusCode, string toStatusCode);

    IReadOnlyList<string> GetAllowedTransitions(string fromStatusCode);
}