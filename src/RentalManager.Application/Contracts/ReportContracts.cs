namespace RentalManager.Application.Contracts;

public sealed record MonthlyReportQuery(int Year, int Month, DateTime? AsOf);

public sealed record CategoryRevenueDto(
    string Category,
    int Lines,
    int RentalDays,
    decimal Revenue);

public sealed record OverdueReservationDto(
    int Id,
    string Reference,
    string CustomerName,
    DateTime To,
    int DaysOverdue,
    decimal TotalAmount);

public sealed record MonthlyReportDto(
    int Year,
    int Month,
    int ReservationCount,
    int CompletedCount,
    int OverdueCount,
    decimal TotalRevenue,
    int RentalDays,
    IReadOnlyList<CategoryRevenueDto> ByCategory,
    IReadOnlyList<OverdueReservationDto> Overdue);
