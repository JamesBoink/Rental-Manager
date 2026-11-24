using RentalManager.Application.Contracts;

namespace RentalManager.Application.Interfaces;

public interface IReservationService
{
    Task<PagedResult<ReservationDto>> GetReservationsAsync(
        int page,
        int pageSize,
        string? status = null,
        int? customerId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<ReservationDto?> GetReservationAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationDto?> AddLineAsync(
        int id, CreateReservationLineRequest request, CancellationToken cancellationToken = default);

    Task<ReservationDto?> ConfirmAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto?> StartAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto?> CompleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto?> CancelAsync(int id, CancellationToken cancellationToken = default);

    Task<AvailabilityResultDto> CheckAvailabilityAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
