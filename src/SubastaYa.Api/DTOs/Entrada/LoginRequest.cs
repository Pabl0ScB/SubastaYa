using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

/// <summary>
/// Credenciales para iniciar sesion.
/// A proposito NO lleva [MinLength] en la contrasena: las reglas de complejidad
/// solo se exigen al registrarse. Aplicarlas aca le revelaria a un atacante cual
/// es la politica de contrasenas, y ademas rechazaria antes de tiempo a usuarios
/// registrados bajo una politica anterior.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato valido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    public string Password { get; set; } = null!;
}
