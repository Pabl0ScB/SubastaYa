using SubastaYa.Domain.Enums;

namespace SubastaYa.Domain.Entidades;

public class Subasta
{
    public int Id { get; set; }
    public int VendedorId { get; set; }
    public int CategoriaId { get; set; }
    public string Titulo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string UrlImagen { get; set; } = null!;
    public decimal PrecioBase { get; set; }
    public decimal IncrementoMinimo { get; set; }
    public decimal PujaActual { get; set; }
    public int? LiderId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoSubasta Estado { get; set; }
    public int Version { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Vendedor { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
    public Usuario? Lider { get; set; }
    public ICollection<Puja> Pujas { get; set; } = new List<Puja>();
}