using API.Auth;
using API.Middleware;
using API.Services;
using BusinessLogic;
using BusinessLogic.Abstractions;
using BusinessLogic.Auth;
using BusinessLogic.Extensions;
using Contracts.Security;
using DataAccess.Context;
using DataAccess.Extensions;
using DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebApplication1.Hubs;
using WebApplication1.Realtime;
var builder = WebApplication.CreateBuilder(args);


// =========================================================
// Database
// =========================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<DbContext>(
    sp => sp.GetRequiredService<AppDbContext>());


// =========================================================
// SignalR
// =========================================================

builder.Services.AddSignalR();


// =========================================================
// Identity
// =========================================================

builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);

    options.Lockout.MaxFailedAccessAttempts = 5;

    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


// =========================================================
// JWT Options
// =========================================================

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        o =>
            !string.IsNullOrWhiteSpace(o.Key) &&
            Encoding.UTF8.GetByteCount(o.Key) >= 32,
        "Jwt:Key is missing or shorter than 256 bits.")
    .Validate(
        o => o.AccessTokenMinutes is > 0 and <= 60,
        "Jwt:AccessTokenMinutes must be between 1 and 60.")
    .ValidateOnStart();


// =========================================================
// JWT Authentication
// =========================================================

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var jwtCfg =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtCfg.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwtCfg.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtCfg.Audience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = AppClaimTypes.Name,

                RoleClaimType = AppClaimTypes.Role,

            };


        // =====================================================
        // SignalR JWT
        // =====================================================

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
            }
        };
    });

// =========================================================
// Claims Transformation
// =========================================================

// يحوّل الـ claims المطولة (اللي بتنتج تلقائياً من JsonWebTokenHandler)
// إلى الصيغة القصيرة الأصلية (AppClaimTypes) بدون التأثير على TokenService
// أو على DefaultInboundClaimTypeMap

builder.Services.AddTransient<IClaimsTransformation, RoleClaimNormalizationTransformer>();



// =========================================================
// Authorization
// =========================================================

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());


// =========================================================
// Rate Limiting
// =========================================================

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
});


// =========================================================
// CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy
            .WithOrigins(
                builder.Configuration
                    .GetSection("AllowedOrigins")
                    .Get<string[]>()
                ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


// =========================================================
// Dependency Injection
// =========================================================

// HttpContext
builder.Services.AddHttpContextAccessor();


// Current User
// مهم جداً:
// API يستخدم HttpCurrentUser وليس CurrentUser
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();


// Authentication / Authorization Services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, BusinessLogic.Services.SmtpEmailSender>();


// Business Logic
builder.Services.AddBusinessLogic();


// =========================================================
// Realtime
// =========================================================

// نستخدم SignalRNotifier في الـ API
builder.Services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();


// Data Access
builder.Services.AddDataAccess();


// Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


// =========================================================
// Swagger
// =========================================================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "TicketHub API",
            Version = "v1"
        });


    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Paste your JWT access token from " +
                "/api/auth/login below " +
                "(without Bearer prefix)."
        });


    options.AddSecurityRequirement(doc =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    doc)
            ] = new List<string>()
        });
});


var app = builder.Build();


// =========================================================
// Middleware Pipeline
// =========================================================

app.UseMiddleware<ExceptionHandlingMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseRateLimiter();


// Authentication يجب أن يأتي قبل Authorization
app.UseAuthentication();

app.UseAuthorization();

app.UseStaticFiles();


// =========================================================
// Endpoints
// =========================================================

app.MapControllers();

app.MapHub<ChatHub>("/hubs/chat");


// =========================================================
// Database Seeding
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await DbSeeder.SeedAsync(db);
}


app.Run();