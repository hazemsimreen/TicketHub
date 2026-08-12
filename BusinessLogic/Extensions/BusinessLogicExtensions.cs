using BusinessLogic.Abstractions;
using BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Extensions;

public static class BusinessLogicExtensions
{
    public static IServiceCollection AddBusinessLogic(
        this IServiceCollection services)
    {
        services.AddScoped<IChatService, ChatService>();

        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IRealtimeNotifier, NullRealtimeNotifier>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}