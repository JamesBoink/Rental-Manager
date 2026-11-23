using RentalManager.Domain.Entities;

namespace RentalManager.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}
