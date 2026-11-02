using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalManager.Domain.Entities;

namespace RentalManager.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Ignore(x => x.TotalAmount);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Reference).IsUnique().HasDatabaseName("ix_reservations_reference");
        builder.HasIndex(x => new { x.CustomerId, x.From }).HasDatabaseName("ix_reservations_customer_from");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_reservations_status");
    }
}
