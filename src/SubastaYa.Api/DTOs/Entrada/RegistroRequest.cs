using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

/// <summary>
/// Datos de alta de un usuario. Las anotaciones validan el formato y [ApiController]
/// responde 400 automaticamente si alguna falla, antes de llegar al servicio. Las reglas
/// de negocio (email o seudonimo ya usados) se validan alli y responden 409.
/// </summary>
public class RegistroRequest
{
    // Los largos maximos son los mismos de las columnas, para rechazar acá y no con un
    // error de la base de datos.
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato valido.")]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El seudonimo es obligatorio.")]
    [MaxLength(50)]
    public string Seudonimo { get; set; } = null!;
}
