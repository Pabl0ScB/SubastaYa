namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Respuesta a quien acaba de ofertar: la oferta guardada mas el estado en que quedo la
/// subasta. Con esto la pantalla del postor se actualiza y avisa una extension de tiempo
/// sin depender de que llegue la notificacion en vivo.
/// </summary>
public class PujaRegistradaResponse : PujaResponse
{
    /// <summary>Fecha de cierre despues de la oferta: cambia si hubo extension.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>La oferta mas baja que se acepta a partir de ahora.</summary>
    public decimal MontoMinimo { get; set; }

    /// <summary>True si esta oferta disparo la regla anti-sniping.</summary>
    public bool TiempoExtendido { get; set; }
}
