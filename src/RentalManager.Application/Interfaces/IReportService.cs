using RentalManager.Application.Contracts;

namespace RentalManager.Application.Interfaces;

public interface IReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(
        MonthlyReportQuery query, CancellationToken cancellationToken = default);
}
