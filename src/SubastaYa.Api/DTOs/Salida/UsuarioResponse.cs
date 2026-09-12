namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Vista publica de un usuario.
/// NO expone PasswordHash. Esa ausencia es deliberada: si se devolviera la entidad
/// Usuario directamente, cada respuesta de la API publicaria el hash de la
/// contrasena. Es la razon principal por la que existen los DTOs de salida.
/// </summary>
public class UsuarioResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Seudonimo { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }
}
