using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Infrastructure.Configuraciones;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.Email)
               .HasMaxLength(150);

        builder.Property(u => u.PasswordHash)
               .HasMaxLength(255);

        builder.Property(u => u.Nombre)
               .HasMaxLength(100);

        builder.Property(u => u.Seudonimo)
               .HasMaxLength(50);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Seudonimo).IsUnique();
    }
}