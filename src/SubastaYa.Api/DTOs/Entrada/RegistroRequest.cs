using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

/// <summary>
/// Datos que manda el cliente para registrarse.
/// Las anotaciones validan el FORMATO: si alguna falla, [ApiController] devuelve
/// 400 automaticamente, sin que el servicio llegue a ejecutarse. Las reglas de
/// negocio (email ya registrado, seudonimo repetido) se validan en el servicio y
/// devuelven 409.
/// Los largos maximos coinciden con los de las columnas para no delegarle a la
/// base de datos un error que podemos detectar antes.
/// </summary>
public class RegistroRequest
{
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
