using System.ComponentModel.DataAnnotations.Schema;

namespace SubastaYa.Domain.Entidades;

public class Billetera
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public decimal SaldoTotal { get; set; }
    public decimal SaldoRetenido { get; set; }
    public int Version { get; set; }

    [NotMapped]
    public decimal SaldoDisponible => SaldoTotal - SaldoRetenido;

    public Usuario Usuario { get; set; } = null!;
}