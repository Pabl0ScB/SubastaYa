using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Estado de la billetera del usuario autenticado.
/// </summary>
public class BilleteraResponse
{
    /// <summary>Todo el dinero cargado en la cuenta.</summary>
    public decimal SaldoTotal { get; set; }

    /// <summary>La parte comprometida en pujas que todavia lideran.</summary>
    public decimal SaldoRetenido { get; set; }

    /// <summary>Lo que se puede comprometer en una puja nueva.</summary>
    public decimal SaldoDisponible { get; set; }

    // El disponible se toma de la entidad y no se recalcula aca: la resta vive en un solo
    // lugar, y si la regla cambia, cambia para todos los que la usan.
    public static BilleteraResponse Desde(Billetera billetera) => new()
    {
        SaldoTotal = billetera.SaldoTotal,
        SaldoRetenido = billetera.SaldoRetenido,
        SaldoDisponible = billetera.SaldoDisponible
    };
}
