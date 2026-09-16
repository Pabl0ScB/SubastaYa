using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Vista publica de un usuario. No expone PasswordHash: devolver la entidad Usuario
/// directamente publicaria el hash en cada respuesta.
/// </summary>
public class UsuarioResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Seudonimo { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }

    // Mapeo a mano, sin AutoMapper: es explicito y deja a la vista que campos salen.
    public static UsuarioResponse Desde(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Email = usuario.Email,
        Nombre = usuario.Nombre,
        Seudonimo = usuario.Seudonimo,
        FechaRegistro = usuario.FechaRegistro
    };
}
