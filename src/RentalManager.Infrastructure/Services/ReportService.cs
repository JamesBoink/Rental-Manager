using Microsoft.EntityFrameworkCore;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Infrastructure.Services;

public sealed class ReportService(RentalDbContext db) : IReportService
{
    public async Task<MonthlyReportDto> GetMonthlyReportAsync(
        MonthlyReportQuery query, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(query.Year, query.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var asOf = query.AsOf ?? DateTime.UtcNow;

        var reservations = await db.Reservations
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Equipment).ThenInclude(x => x.Category)
            .Where(x => x.To >= start && x.To < end &&
                        x.Status != ReservationStatus.Draft &&
                        x.Status != ReservationStatus.Cancelled)
            .OrderBy(x => x.To)
            .ToListAsync(cancellationToken);

        var lines = reservations.SelectMany(x => x.Lines).ToList();

        var byCategory = lines
            .GroupBy(x => x.Equipment.Category.Name)
            .Select(group => new CategoryRevenueDto(
                group.Key,
                group.Count(),
                group.Sum(x => x.Days * x.Quantity),
                group.Sum(x => x.TotalAmount)))
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var overdue = reservations
            .Where(x => x.Status == ReservationStatus.Overdue)
            .Select(x => new OverdueReservationDto(
                x.Id,
                x.Reference,
                x.Customer.Name,
                x.To,
                Math.Max(0, (int)(asOf - x.To).TotalDays),
                x.TotalAmount))
            .ToList();

        return new MonthlyReportDto(
            query.Year,
            query.Month,
            reservations.Count,
            reservations.Count(x => x.Status == ReservationStatus.Completed),
            overdue.Count,
            lines.Sum(x => x.TotalAmount),
            lines.Sum(x => x.Days * x.Quantity),
            byCategory,
            overdue);
    }
}
