using DataAccess.Models;

namespace BusinessLogic.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresUtc) CreateAccessToken(User user);
    string CreateRefreshToken();
}
