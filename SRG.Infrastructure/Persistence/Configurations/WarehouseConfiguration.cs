using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(warehouse => warehouse.Id);
        builder.Property(warehouse => warehouse.Name).HasMaxLength(200).IsRequired();
        builder.Property(warehouse => warehouse.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(warehouse => warehouse.Type).IsUnique().HasFilter("\"Type\" = 'Main'");
        builder.HasIndex(warehouse => new { warehouse.Type, warehouse.OwnerId })
            .IsUnique()
            .HasFilter("\"OwnerId\" IS NOT NULL AND \"Type\" = 'Sub'");

        builder.HasData(new Warehouse
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Warehouse glowny",
            Type = WarehouseType.Main,
            OwnerId = null,
        });
    }
}
