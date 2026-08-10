using API.Auth;
using BusinessLogic.Abstractions;
using BusinessLogic.Extensions;
using DataAccess.Context;
using DataAccess.Extensions;
using DataAccess.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApplication1.Hubs;
using WebApplication1.Realtime;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentityCore<User>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Key)
             && Encoding.UTF8.GetByteCount(o.Key) >= 32,
        "Jwt:Key is missing or shorter than 256 bits.")
    .Validate(
        o => o.AccessTokenMinutes is > 0 and <= 60,
        "Jwt:AccessTokenMinutes must be between 1 and 60.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;

        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RequireExpirationTime = true,

                RoleClaimType = "role",
                NameClaimType = "sub"
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = async context =>
            {
                var sub = context.Principal?
                    .FindFirst("sub")?
                    .Value;

                var stamp = context.Principal?
                    .FindFirst("stamp")?
                    .Value;

                if (!Guid.TryParse(
                        sub,
                        out var userId) ||
                    string.IsNullOrWhiteSpace(stamp))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var db = context.HttpContext
                    .RequestServices
                    .GetRequiredService<AppDbContext>();

                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        u.IsActive,
                        u.IsDeleted,
                        u.SecurityStamp
                    })
                    .SingleOrDefaultAsync(
                        context.HttpContext.RequestAborted);

                if (user is null ||
                    !user.IsActive ||
                    user.IsDeleted ||
                    user.SecurityStamp != stamp)
                {
                    context.Fail(
                        "Token is no longer valid.");
                }
            }
        };
    });

builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddControllers();

builder.Services.AddBusinessLogic();

builder.Services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();

builder.Services.AddDataAccess();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Description =
                "Enter your JWT access token (without the word 'Bearer')"
        });

    options.AddSecurityRequirement(
        document =>
            new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [
                    new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                        "Bearer",
                        document)
                ] = new List<string>()
            });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();