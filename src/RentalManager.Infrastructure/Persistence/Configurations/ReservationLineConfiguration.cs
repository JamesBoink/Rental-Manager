using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalManager.Domain.Entities;

namespace RentalManager.Infrastructure.Persistence.Configurations;

public class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    public void Configure(EntityTypeBuilder<ReservationLine> builder)
    {
        builder.ToTable("reservation_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice).HasPrecision(10, 2);

        builder.Ignore(x => x.Days);
        builder.Ignore(x => x.TotalAmount);

        builder.HasOne(x => x.Reservation)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Equipment)
            .WithMany(x => x.ReservationLines)
            .HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReservationId).HasDatabaseName("ix_reservation_lines_reservation_id");
        builder.HasIndex(x => x.EquipmentId).HasDatabaseName("ix_reservation_lines_equipment_id");
    }
}
