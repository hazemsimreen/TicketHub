using API.Auth;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BusinessLogic.Extensions;
using DataAccess.Extensions;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Key)
                   && Encoding.UTF8.GetByteCount(o.Key) >= 32,
        "Jwt:Key is missing or shorter than 256 bits.")
    .Validate(o => o.AccessTokenMinutes is > 0 and <= 60,
        "Jwt:AccessTokenMinutes must be between 1 and 60.")
    .ValidateOnStart();




builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddControllers();




builder.Services.AddBusinessLogic();
builder.Services.AddDataAccess();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();