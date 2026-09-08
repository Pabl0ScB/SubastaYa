using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Infrastructure.Configuraciones;

public class PujaConfiguration : IEntityTypeConfiguration<Puja>
{
    public void Configure(EntityTypeBuilder<Puja> builder)
    {
        builder.Property(p => p.Monto)
               .HasColumnType("numeric(18,2)");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Puja_MontoPositivo", "\"Monto\" > 0"));

        builder.HasIndex(p => new { p.SubastaId, p.FechaPuja })
               .IsDescending(false, true);

        builder.HasIndex(p => p.CompradorId);

        builder.HasOne(p => p.Subasta)
               .WithMany(s => s.Pujas)
               .HasForeignKey(p => p.SubastaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Comprador)
               .WithMany()
               .HasForeignKey(p => p.CompradorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}