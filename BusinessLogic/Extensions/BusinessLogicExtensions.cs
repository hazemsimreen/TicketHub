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

        // تسجيل خدمات Business Logic هنا

        services.AddScoped<IRealtimeNotifier, NullRealtimeNotifier>();

        services.AddScoped<ITicketService, TicketService>();


        services.AddSingleton<ITicketWorkflow, TicketWorkflow>();


        // Role 4 — Collaboration, Files & Insight
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}