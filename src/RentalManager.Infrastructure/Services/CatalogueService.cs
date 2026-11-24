using Microsoft.EntityFrameworkCore;
using RentalManager.Application.Common;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Common;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Infrastructure.Services;

public sealed class CatalogueService(RentalDbContext db) : ICatalogueService
{
    public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var total = await db.EquipmentCategories.CountAsync(cancellationToken);

        var items = await db.EquipmentCategories
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description))
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(items, page, pageSize, total);
    }

    public async Task<CategoryDto?> GetCategoryAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var category = await db.EquipmentCategories.FindAsync([id], cancellationToken);

        return category is null ? null : Map(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await db.EquipmentCategories.AnyAsync(
                x => x.Name.ToLower() == name.ToLowerInvariant(), cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new EquipmentCategory(name, request.Description);
        db.EquipmentCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Map(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(
        int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await db.EquipmentCategories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return null;
        }

        var name = request.Name.Trim();

        if (await db.EquipmentCategories.AnyAsync(
                x => x.Name.ToLower() == name.ToLowerInvariant() && x.Id != id, cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Rename(name, request.Description);
        await db.SaveChangesAsync(cancellationToken);

        return Map(category);
    }

    public async Task<PagedResult<EquipmentDto>> GetEquipmentAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var query = db.EquipmentItems.Include(x => x.Category).AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EquipmentStatus>(status, true, out var parsed))
            {
                throw new DomainException($"Unknown equipment status '{status}'.");
            }

            query = query.Where(x => x.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EquipmentDto(
                x.Id,
                x.Name,
                x.Description,
                x.CategoryId,
                x.Category.Name,
                x.DailyRate,
                x.Stock,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<EquipmentDto>(items, page, pageSize, total);
    }

    public async Task<EquipmentDto?> GetEquipmentAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var item = await db.EquipmentItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return item is null ? null : Map(item);
    }

    public async Task<EquipmentDto> CreateEquipmentAsync(
        CreateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var category = await db.EquipmentCategories.FindAsync([request.CategoryId], cancellationToken)
            ?? throw new NotFoundException($"Category {request.CategoryId} does not exist.");

        var item = new EquipmentItem(category, request.Name, request.DailyRate, request.Description);
        db.EquipmentItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    public async Task<EquipmentDto?> UpdateEquipmentAsync(
        int id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var item = await db.EquipmentItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.Rename(request.Name, request.Description);
        item.ChangeDailyRate(request.DailyRate);
        await db.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    public async Task<EquipmentDto?> ChangeEquipmentStatusAsync(
        int id, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<EquipmentStatus>(status, true, out var parsed))
        {
            throw new DomainException($"Unknown equipment status '{status}'.");
        }

        var item = await db.EquipmentItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.ChangeStatus(parsed);
        await db.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    private static CategoryDto Map(EquipmentCategory category) =>
        new(category.Id, category.Name, category.Description);

    private static EquipmentDto Map(EquipmentItem item) =>
        new(
            item.Id,
            item.Name,
            item.Description,
            item.CategoryId,
            item.Category.Name,
            item.DailyRate,
            item.Stock,
            item.Status.ToString());

    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => 20,
        > 100 => 100,
        _ => pageSize
    };
}
