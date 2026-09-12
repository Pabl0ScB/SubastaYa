namespace SubastaYa.Domain.Entidades;

public class RegistroAuditoria
{
    public int Id { get; set; }
    public string Entidad { get; set; } = null!;
    public int EntidadId { get; set; }
    public string Accion { get; set; } = null!;
    public int? UsuarioId { get; set; }
    public string DetalleJson { get; set; } = null!;
    public DateTime Fecha { get; set; }

    public Usuario? Usuario { get; set; }
}