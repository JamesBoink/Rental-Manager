using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public enum ReservationStatus
{
    Draft = 0,
    Confirmed = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}

public class Reservation
{
    private readonly List<ReservationLine> _lines = new();
    private Reservation()
    {
    }

    public Reservation(Customer customer, string reference, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException("Reservation reference is required.");
        }

        if (to <= from)
        {
            throw new DomainException("Reservation end date must be after the start date.");
        }

        Customer = customer;
        Reference = reference.Trim();
        From = from;
        To = to;
        Status = ReservationStatus.Draft;
    }

    public int Id { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public int CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public DateTime From { get; private set; }

    public DateTime To { get; private set; }

    public ReservationStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<ReservationLine> Lines => _lines.AsReadOnly();

    public decimal TotalAmount => _lines.Sum(x => x.TotalAmount);

    public void AddLine(EquipmentItem equipment, DateTime from, DateTime to, int quantity = 1)
    {
        if (Status != ReservationStatus.Draft)
        {
            throw new DomainException("Only a draft reservation can be modified.");
        }

        if (quantity < 1)
        {
            throw new DomainException("Quantity must be at least one.");
        }

        if (from < From || to > To)
        {
            throw new DomainException("Equipment period must fit inside the reservation period.");
        }

        _lines.Add(new ReservationLine(this, equipment, from, to, quantity));
    }

    public void SetNotes(string? notes)
    {
        Notes = notes?.Trim();
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Draft)
        {
            throw new DomainException("Only a draft reservation can be confirmed.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("A reservation must contain at least one equipment line.");
        }

        Status = ReservationStatus.Confirmed;
    }

    public void Start()
    {
        if (Status != ReservationStatus.Confirmed)
        {
            throw new DomainException("Only a confirmed reservation can be started.");
        }

        Status = ReservationStatus.Active;
    }

    public void Complete()
    {
        if (Status != ReservationStatus.Active)
        {
            throw new DomainException("Only an active reservation can be completed.");
        }

        Status = ReservationStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is ReservationStatus.Completed or ReservationStatus.Cancelled)
        {
            throw new DomainException("A finished reservation cannot be cancelled.");
        }

        Status = ReservationStatus.Cancelled;
    }

    public bool Overlaps(DateTime from, DateTime to)
    {
        return From < to && from < To;
    }
}
