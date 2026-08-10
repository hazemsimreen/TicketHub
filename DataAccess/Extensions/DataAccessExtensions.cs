// <<<<<<< feature/Tickets
// ﻿using DataAccess.Context;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using TicketHub.DataAccess.Repositories;
// =======
// ﻿using TicketHub.DataAccess.Repositories;
// using Microsoft.Extensions.DependencyInjection;
// >>>>>>> main

// namespace DataAccess.Extensions;

// public static class DataAccessExtensions
// {
//     public static IServiceCollection AddDataAccess(
//         this IServiceCollection services)
//     {
// <<<<<<< feature/Tickets
//         // بيخلي أي كود بيطلب DbContext عادي (زي Repository<T>) يحصل نفس الـ AppDbContext
//         // المسجّل أصلاً بـ Program.cs
//         services.AddScoped<DbContext>(sp =>
//             sp.GetRequiredService<AppDbContext>());

//         services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// =======
// >>>>>>> main
//         services.AddScoped<IUnitOfWork, UnitOfWork>();

//         return services;
//     }
// }



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