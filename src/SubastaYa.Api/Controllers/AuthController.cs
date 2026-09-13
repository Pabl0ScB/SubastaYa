using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IServicioDeAutenticacion _servicio;

    public AuthController(IServicioDeAutenticacion servicio)
    {
        _servicio = servicio;
    }

    /// <summary>
    /// Inicia sesion y devuelve el token de acceso. La ruta es un sustantivo en plural y
    /// no un verbo: iniciar sesion se modela como crear un recurso de tipo sesion.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(SesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion([FromBody] LoginRequest solicitud)
    {
        var sesion = await _servicio.IniciarSesionAsync(solicitud);

        // Mensaje unico a proposito: distinguir si fallo el email o la contrasena
        // permitiria averiguar que cuentas existen.
        return sesion is null
            ? Unauthorized(new ErrorResponse("Email o contrasena incorrectos."))
            : Ok(sesion);
    }
}
