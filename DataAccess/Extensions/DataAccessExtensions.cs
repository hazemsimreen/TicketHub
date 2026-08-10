using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.DataAccess.Repositories;

namespace DataAccess.Extensions;

public static class DataAccessExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services)
    {
        // بيخلي أي كود بيطلب DbContext عادي (زي Repository<T>) يحصل نفس الـ AppDbContext
        // المسجّل أصلاً بـ Program.cs
        services.AddScoped<DbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}