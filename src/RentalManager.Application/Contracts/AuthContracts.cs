namespace RentalManager.Application.Contracts;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthenticatedUser User);

public record AuthenticatedUser(
    int Id,
    string Email,
    string DisplayName,
    string Role);
