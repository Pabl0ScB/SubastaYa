namespace SubastaYa.Api.DTOs.Salida;

// Lo que se muestra en cada tarjeta del catálogo — datos resumidos, no el detalle completo.
public class SubastaTarjetaResponse
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string UrlImagen { get; set; } = null!;
    public string NombreCategoria { get; set; } = null!;
    public decimal PujaActual { get; set; }
    public int CantidadOfertas { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = null!;
}