namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Cuerpo del 409 cuando otra oferta gano la carrera. Trae el estado actualizado de la
/// subasta para que el cliente pueda ofrecer el reintento sin hacer otra peticion.
/// </summary>
public class ConflictoPujaResponse
{
    // Va en el propio cuerpo porque, cuando la excepcion trae detalle, el middleware
    // responde solo con el detalle: sin este campo el cliente se quedaria sin mensaje.
    public string Mensaje { get; set; } = null!;

    public decimal PujaActual { get; set; }

    /// <summary>PujaActual mas el incremento minimo: la oferta mas baja que se acepta ahora.</summary>
    public decimal MontoMinimo { get; set; }

    /// <summary>Puede haber cambiado si la oferta ganadora disparo la extension de tiempo.</summary>
    public DateTime FechaFin { get; set; }
}
