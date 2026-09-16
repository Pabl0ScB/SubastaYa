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
}
