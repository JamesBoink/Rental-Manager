namespace RentalManager.Application.Common;

/// <summary>
/// Raised when a command would violate a uniqueness rule.
/// </summary>
public class ConflictException(string message) : Exception(message);
