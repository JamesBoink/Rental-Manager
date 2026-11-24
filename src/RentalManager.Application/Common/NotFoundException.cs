namespace RentalManager.Application.Common;

/// <summary>
/// Raised when a referenced entity does not exist.
/// </summary>
public class NotFoundException(string message) : Exception(message);
