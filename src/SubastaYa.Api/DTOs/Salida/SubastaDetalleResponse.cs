namespace SubastaYa.Api.DTOs.Salida;

public class SubastaDetalleResponse
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string UrlImagen { get; set; } = null!;
    public string NombreCategoria { get; set; } = null!;
    public decimal PrecioBase { get; set; }
    public decimal IncrementoMinimo { get; set; }
    public decimal PujaActual { get; set; }
    public int CantidadOfertas { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = null!;
    public string SeudonimoVendedor { get; set; } = null!;
    public string? SeudonimoLider { get; set; }
}