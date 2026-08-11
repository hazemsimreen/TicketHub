using BusinessLogic.Abstractions;
using BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Extensions;

public static class BusinessLogicExtensions
{
    public static IServiceCollection AddBusinessLogic(
        this IServiceCollection services)
    {
        
        services.AddScoped<IChatService, ChatService>();

        // سيتم تسجيل الخدمات هنا اي خدمة يتم اضافتها في طبقة الاعمال يجب تسجيلها هنا لكي يتم حقنها في الطبقات الاخرى

        services.AddScoped<IRealtimeNotifier, NullRealtimeNotifier>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
        
    }
}