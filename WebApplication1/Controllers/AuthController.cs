using API.Auth;
using Contract.Dtos;
using DataAccess.Models;
using DataAccess.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _db;

    public AuthController(UserManager<User> userManager, ITokenService tokens, AppDbContext db)
    {
        _userManager = userManager;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var user = new User
        {
            Email = req.Email,
            UserName = req.Email,
            UserType = "Citizen"
        };

        var result = await _userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        _db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = 1 });
        await _db.SaveChangesAsync();

        user.UserRoles.Add(new UserRole { RoleId = 1, Role = new Role { Code = "Citizen" } });

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(BuildAuthResponse(user));
    }
    private AuthResponse BuildAuthResponse(User user)
    {
        var (accessToken, expires) = _tokens.CreateAccessToken(user);
        var refreshToken = _tokens.CreateRefreshToken();

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();

        return new AuthResponse(accessToken, refreshToken, expires, user.Email ?? string.Empty, roles);
    }
}