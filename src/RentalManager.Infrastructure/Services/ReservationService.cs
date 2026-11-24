using Microsoft.EntityFrameworkCore;
using RentalManager.Application.Common;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Common;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Infrastructure.Services;

public sealed class ReservationService(RentalDbContext db) : IReservationService
{
    public async Task<PagedResult<ReservationDto>> GetReservationsAsync(
        int page,
        int pageSize,
        string? status = null,
        int? customerId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = db.Reservations
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Equipment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ReservationStatus>(status, true, out var parsed))
            {
                throw new DomainException($"Unknown reservation status '{status}'.");
            }

            query = query.Where(x => x.Status == parsed);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.To > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.From < to.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var reservations = await query
            .OrderByDescending(x => x.From)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationDto>(
            reservations.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ReservationDto?> GetReservationAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var reservation = await LoadReservationAsync(id, cancellationToken);

        return reservation is null ? null : Map(reservation);
    }

    public async Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.To <= request.From)
        {
            throw new DomainException("Reservation end date must be after the start date.");
        }

        var customer = await db.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new NotFoundException($"Customer {request.CustomerId} does not exist.");

        var reservation = new Reservation(
            customer,
            await NextReferenceAsync(request.From, cancellationToken),
            request.From,
            request.To);

        reservation.SetNotes(request.Notes);

        foreach (var line in request.Lines)
        {
            var equipment = await db.EquipmentItems.FindAsync([line.EquipmentId], cancellationToken)
                ?? throw new NotFoundException($"Equipment {line.EquipmentId} does not exist.");

            await EnsureAvailableAsync(
                line.EquipmentId, line.From, line.To, line.Quantity, cancellationToken);

            reservation.AddLine(equipment, line.From, line.To, line.Quantity);
        }

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        return Map(reservation);
    }

    public async Task<ReservationDto?> AddLineAsync(
        int id, CreateReservationLineRequest request, CancellationToken cancellationToken = default)
    {
        var reservation = await LoadReservationAsync(id, cancellationToken);
        if (reservation is null)
        {
            return null;
        }

        var equipment = await db.EquipmentItems.FindAsync([request.EquipmentId], cancellationToken)
            ?? throw new NotFoundException($"Equipment {request.EquipmentId} does not exist.");

        await EnsureAvailableAsync(
            request.EquipmentId, request.From, request.To, request.Quantity, cancellationToken);

        reservation.AddLine(equipment, request.From, request.To, request.Quantity);
        await db.SaveChangesAsync(cancellationToken);

        return Map(reservation);
    }

    public Task<ReservationDto?> ConfirmAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Confirm(), cancellationToken);

    public Task<ReservationDto?> StartAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Start(), cancellationToken);

    public Task<ReservationDto?> CompleteAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Complete(), cancellationToken);

    public Task<ReservationDto?> CancelAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Cancel(), cancellationToken);

    public async Task<AvailabilityResultDto> CheckAvailabilityAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new DomainException("Reservation end date must be after the start date.");
        }

        var booked = await db.ReservationLines
            .Where(x => (x.Reservation.Status == ReservationStatus.Confirmed ||
                         x.Reservation.Status == ReservationStatus.Active) &&
                        x.From < to && from < x.To)
            .GroupBy(x => x.EquipmentId)
            .Select(x => new { EquipmentId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToListAsync(cancellationToken);

        var bookedByEquipment = booked.ToDictionary(x => x.EquipmentId, x => x.Quantity);

        var equipment = await db.EquipmentItems
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Stock })
            .ToListAsync(cancellationToken);

        var items = equipment
            .Select(x =>
            {
                var booked = bookedByEquipment.GetValueOrDefault(x.Id);
                return new EquipmentAvailabilityDto(x.Id, x.Name, x.Stock, booked, x.Stock - booked);
            })
            .ToList();

        return new AvailabilityResultDto(from, to, items);
    }

    private async Task<ReservationDto?> ChangeStatusAsync(
        int id, Action<Reservation> transition, CancellationToken cancellationToken)
    {
        var reservation = await LoadReservationAsync(id, cancellationToken);
        if (reservation is null)
        {
            return null;
        }

        transition(reservation);
        await db.SaveChangesAsync(cancellationToken);

        return Map(reservation);
    }

    private async Task<Reservation?> LoadReservationAsync(
        int id, CancellationToken cancellationToken) =>
        await db.Reservations
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Equipment)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task EnsureAvailableAsync(
        int equipmentId,
        DateTime from,
        DateTime to,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (to <= from)
        {
            throw new DomainException("Rental end date must be after the start date.");
        }

        if (quantity < 1)
        {
            throw new DomainException("Quantity must be at least one.");
        }

        var equipment = await db.EquipmentItems.FindAsync([equipmentId], cancellationToken)
            ?? throw new NotFoundException($"Equipment {equipmentId} does not exist.");

        if (equipment.Status == EquipmentStatus.Retired)
        {
            throw new ConflictException($"Equipment {equipmentId} is retired.");
        }

        if (equipment.Status == EquipmentStatus.Maintenance)
        {
            throw new ConflictException($"Equipment {equipmentId} is under maintenance.");
        }

        var booked = await db.ReservationLines
            .Where(x => x.EquipmentId == equipmentId &&
                        (x.Reservation.Status == ReservationStatus.Confirmed ||
                         x.Reservation.Status == ReservationStatus.Active) &&
                        x.From < to && from < x.To)
            .SumAsync(x => (int?)x.Quantity, cancellationToken) ?? 0;

        if (booked + quantity > equipment.Stock)
        {
            throw new ConflictException(
                $"Only {equipment.Stock - booked} unit(s) of {equipment.Name} are available for that period.");
        }
    }

    private async Task<string> NextReferenceAsync(
        DateTime from, CancellationToken cancellationToken)
    {
        var prefix = $"R-{from.Year}-";

        var last = await db.Reservations
            .Where(x => x.Reference.StartsWith(prefix))
            .OrderByDescending(x => x.Reference)
            .Select(x => x.Reference)
            .FirstOrDefaultAsync(cancellationToken);

        var next = last is not null && int.TryParse(last[prefix.Length..], out var number)
            ? number + 1
            : 1;

        return $"{prefix}{next:D4}";
    }

    private static ReservationDto Map(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.Reference,
            reservation.CustomerId,
            reservation.Customer.Name,
            reservation.From,
            reservation.To,
            reservation.Status.ToString(),
            reservation.Notes,
            reservation.TotalAmount,
            reservation.Lines
                .OrderBy(x => x.From)
                .Select(x => new ReservationLineDto(
                    x.Id,
                    x.EquipmentId,
                    x.Equipment.Name,
                    x.From,
                    x.To,
                    x.Quantity,
                    x.Days,
                    x.UnitPrice,
                    x.TotalAmount))
                .ToList());
}
