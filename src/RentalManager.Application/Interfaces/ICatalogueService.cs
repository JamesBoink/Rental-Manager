using RentalManager.Application.Contracts;

namespace RentalManager.Application.Interfaces;

public interface ICatalogueService
{
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto?> UpdateCategoryAsync(
        int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<EquipmentDto>> GetEquipmentAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<EquipmentDto?> GetEquipmentAsync(int id, CancellationToken cancellationToken = default);

    Task<EquipmentDto> CreateEquipmentAsync(
        CreateEquipmentRequest request, CancellationToken cancellationToken = default);

    Task<EquipmentDto?> UpdateEquipmentAsync(
        int id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default);

    Task<EquipmentDto?> ChangeEquipmentStatusAsync(
        int id, string status, CancellationToken cancellationToken = default);
}
