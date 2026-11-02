using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public enum UserRole
{
    Staff = 0,
    Manager = 1,
    Admin = 2
}

public class User
{
    private User()
    {
    }

    public User(string email, string passwordHash, string displayName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("User email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Display name is required.");
        }

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        DisplayName = displayName.Trim();
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
