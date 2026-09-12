using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Infrastructure.Configuraciones;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.Property(c => c.Nombre)
               .HasMaxLength(50);

        builder.Property(c => c.UrlIcono)
               .HasMaxLength(255);

        builder.HasIndex(c => c.Nombre)
               .IsUnique();
    }
}