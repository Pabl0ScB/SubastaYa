using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Infrastructure.Configuraciones;

public class BilleteraConfiguration : IEntityTypeConfiguration<Billetera>
{
    public void Configure(EntityTypeBuilder<Billetera> builder)
    {
        builder.Property(b => b.SaldoTotal)
               .HasColumnType("numeric(18,2)");

        builder.Property(b => b.SaldoRetenido)
               .HasColumnType("numeric(18,2)");

        // Optimistic Locking: protege contra la actualizacion perdida
        // cuando un mismo usuario opera en dos subastas a la vez.
        builder.Property(b => b.Version)
               .IsConcurrencyToken();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Billetera_SaldoTotalNoNegativo", "\"SaldoTotal\" >= 0");
            t.HasCheckConstraint("CK_Billetera_SaldoRetenidoNoNegativo", "\"SaldoRetenido\" >= 0");
        });

        // Unico por usuario: es lo que hace que la relacion sea 1 a 1.
        builder.HasIndex(b => b.UsuarioId)
               .IsUnique();

        builder.HasOne(b => b.Usuario)
               .WithOne(u => u.Billetera)
               .HasForeignKey<Billetera>(b => b.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
