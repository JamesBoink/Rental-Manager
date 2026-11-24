namespace RentalManager.Application.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record CategoryDto(int Id, string Name, string? Description);

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record UpdateCategoryRequest(string Name, string? Description);

public sealed record EquipmentDto(
    int Id,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    decimal DailyRate,
    int Stock,
    string Status);

public sealed record CreateEquipmentRequest(
    int CategoryId,
    string Name,
    decimal DailyRate,
    string? Description);

public sealed record UpdateEquipmentRequest(
    string Name,
    decimal DailyRate,
    string? Description);

public sealed record UpdateEquipmentStatusRequest(string Status);
