namespace SubastaYa.Domain.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Seudonimo { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }

    public Billetera Billetera { get; set; } = null!;
}