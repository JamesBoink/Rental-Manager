namespace RentalManager.Application.Contracts;

public sealed record ReservationLineDto(
    int Id,
    int EquipmentId,
    string EquipmentName,
    DateTime From,
    DateTime To,
    int Quantity,
    int Days,
    decimal UnitPrice,
    decimal TotalAmount);

public sealed record ReservationDto(
    int Id,
    string Reference,
    int CustomerId,
    string CustomerName,
    DateTime From,
    DateTime To,
    string Status,
    string? Notes,
    decimal TotalAmount,
    IReadOnlyList<ReservationLineDto> Lines);

public sealed record CreateReservationLineRequest(
    int EquipmentId,
    DateTime From,
    DateTime To,
    int Quantity);

public sealed record CreateReservationRequest(
    int CustomerId,
    DateTime From,
    DateTime To,
    string? Notes,
    IReadOnlyList<CreateReservationLineRequest> Lines);

public sealed record EquipmentAvailabilityDto(
    int EquipmentId,
    string Name,
    int Stock,
    int Booked,
    int Available);

public sealed record AvailabilityQuery(DateTime From, DateTime To);

public sealed record AvailabilityResultDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<EquipmentAvailabilityDto> Items);

public sealed record OverdueSweepDto(int Marked);
