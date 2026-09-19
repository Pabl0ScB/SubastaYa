using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.DTOs.Salida;

/// <summary>Un movimiento del historial de la billetera.</summary>
public class MovimientoResponse
{
    public int Id { get; set; }

    /// <summary>Deposito, Retencion, Liberacion, Pago o Cobro.</summary>
    public string Tipo { get; set; } = null!;

    /// <summary>Siempre positivo: el sentido del movimiento lo da el tipo.</summary>
    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>Subasta que origino el movimiento. Nulo en los depositos.</summary>
    public int? SubastaId { get; set; }

    public string? TituloSubasta { get; set; }

    public static MovimientoResponse Desde(AsientoLedger asiento) => new()
    {
        Id = asiento.Id,
        Tipo = asiento.Tipo.ToString(),
        Monto = asiento.Monto,
        Descripcion = asiento.Descripcion,
        Fecha = asiento.Fecha,
        SubastaId = asiento.SubastaId,
        TituloSubasta = asiento.Subasta?.Titulo
    };
}
