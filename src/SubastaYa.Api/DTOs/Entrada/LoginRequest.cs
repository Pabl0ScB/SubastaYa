using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

public class LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato valido.")]
    public string Email { get; set; } = null!;

    // Sin [MinLength], a diferencia del registro: exigir la politica de contrasenas al
    // iniciar sesion la revelaria, y dejaria afuera a usuarios registrados con reglas
    // anteriores.
    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    public string Password { get; set; } = null!;
}
