using SubastaYa.Domain.Enums;

namespace SubastaYa.Domain.Entidades;

public class AsientoLedger
{
    public int Id { get; set; }
    public int BilleteraId { get; set; }
    public TipoAsiento Tipo { get; set; }
    public decimal Monto { get; set; }
    public int? SubastaId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }

    public Billetera Billetera { get; set; } = null!;
    public Subasta? Subasta { get; set; }
}