using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Security;

namespace RentalManager.Tests.Security;

public class JwtTokenGeneratorTests
{
    private static JwtOptions Options() => new()
    {
        Key = "test-signing-key-that-is-long-enough-for-hs256",
        Issuer = "RentalManager",
        Audience = "RentalManagerClients",
        ExpiresInMinutes = 45
    };

    private static User User(UserRole role = UserRole.Admin) =>
        new("admin@rentalmanager.local", "hash", "Administrator", role);

    [Fact]
    public void TokenContainsIdentityAndRoleClaims()
    {
        var generator = new JwtTokenGenerator(Options().ToOptions());

        var (token, _) = generator.Generate(User());
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();

        Assert.Equal("0", claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin@rentalmanager.local", claims.Single(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Administrator", claims.Single(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("Admin", claims.Single(x => x.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void ExpiryMatchesTheConfiguredLifetime()
    {
        var options = Options();
        var generator = new JwtTokenGenerator(options.ToOptions());
        var before = DateTime.UtcNow;

        var (_, expiresAt) = generator.Generate(User());

        var expected = before.AddMinutes(options.ExpiresInMinutes);
        Assert.InRange(expiresAt, expected.AddSeconds(-2), expected.AddSeconds(2));
    }

    [Fact]
    public void StaffRoleIsWrittenAsStaff()
    {
        var generator = new JwtTokenGenerator(Options().ToOptions());

        var (token, _) = generator.Generate(User(UserRole.Staff));

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
        Assert.Equal("Staff", claims.Single(x => x.Type == ClaimTypes.Role).Value);
    }
}

internal static class JwtOptionsExtensions
{
    public static IOptions<JwtOptions> ToOptions(this JwtOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);
}
