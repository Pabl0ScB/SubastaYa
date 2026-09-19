namespace SubastaYa.Api.DTOs.Salida;

/// <summary>Una subasta en la que el usuario oferto, con su resultado para el.</summary>
public class MiParticipacionResponse
{
    public int SubastaId { get; set; }
    public string Titulo { get; set; } = null!;
    public string UrlImagen { get; set; } = null!;
    public string Estado { get; set; } = null!;

    /// <summary>La oferta mas alta del usuario en esta subasta.</summary>
    public decimal MiOfertaMaxima { get; set; }

    /// <summary>La oferta mas alta de la subasta, sea de quien sea.</summary>
    public decimal PujaActual { get; set; }

    public DateTime FechaFin { get; set; }

    /// <summary>Liderando o Superado si sigue abierta; Ganada o No ganada si cerro.</summary>
    public string Resultado { get; set; } = null!;
}
