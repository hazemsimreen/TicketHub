using API.Auth;
using Contract.Dtos;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int CitizenRoleId = 1;

    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<User> userManager,
        ITokenService tokens,
        AppDbContext db)
    {
        _userManager = userManager;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest req)
    {
        var user = new User
        {
            Email = req.Email,
            UserName = req.Email,
            UserType = "Citizen"
        };

        var result = await _userManager.CreateAsync(
            user,
            req.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _db.Set<UserRole>().Add(new UserRole
        {
            UserId = user.Id,
            RoleId = CitizenRoleId
        });

        await _db.SaveChangesAsync();

        var (accessToken, expires) =
            _tokens.CreateAccessToken(user);

        var refreshToken =
            _tokens.CreateRefreshToken();

        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            expires,
            user.Email ?? string.Empty,
            new List<string> { "Citizen" }));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest req)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user is null ||
            !user.IsActive ||
            !await _userManager.CheckPasswordAsync(
                user,
                req.Password))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (accessToken, expires) =
            _tokens.CreateAccessToken(user);

        var refreshToken =
            _tokens.CreateRefreshToken();

        var roles = user.UserRoles
            .Where(ur =>
                !ur.IsDeleted &&
                ur.Role is not null)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToList();

        return new AuthResponse(
            accessToken,
            refreshToken,
            expires,
            user.Email ?? string.Empty,
            roles);
    }
}