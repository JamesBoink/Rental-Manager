using Microsoft.EntityFrameworkCore;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Infrastructure.Services;

public class AuthService(
    RentalDbContext db,
    IPasswordHasher hasher,
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Email == email && x.IsActive,
            cancellationToken);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var (token, expiresAt) = tokenGenerator.Generate(user);

        return new LoginResponse(
            token,
            expiresAt,
            new AuthenticatedUser(user.Id, user.Email, user.DisplayName, user.Role.ToString()));
    }
}
