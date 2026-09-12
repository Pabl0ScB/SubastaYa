using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Infrastructure.Configuraciones;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.Property(r => r.Entidad)
               .HasMaxLength(50);

        builder.Property(r => r.Accion)
               .HasMaxLength(50);

        builder.HasIndex(r => new { r.Entidad, r.EntidadId });

        builder.HasOne(r => r.Usuario)
               .WithMany()
               .HasForeignKey(r => r.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}