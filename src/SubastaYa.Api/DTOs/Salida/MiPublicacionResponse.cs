namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Una subasta vista por su vendedor: ademas de los datos de la tarjeta, cuanto recaudo
/// y como termino.
/// </summary>
public class MiPublicacionResponse
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string UrlImagen { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public decimal PujaActual { get; set; }
    public int CantidadOfertas { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal Recaudado { get; set; }
    public string EstadoAdjudicacion { get; set; } = null!;
    public string? SeudonimoGanador { get; set; }
}
