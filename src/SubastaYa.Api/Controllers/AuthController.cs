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

    /// <summary>Inicia sesion y devuelve el token de acceso.</summary>
    /// <remarks>
    /// La ruta es un sustantivo en plural ("sessions") y no un verbo ("login"):
    /// iniciar sesion es crear un recurso de tipo sesion, y por eso es un POST sobre
    /// la coleccion.
    /// </remarks>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(SesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion([FromBody] LoginRequest solicitud)
    {
        var sesion = await _servicio.IniciarSesionAsync(solicitud);

        if (sesion is null)
        {
            // Mensaje unico y deliberadamente vago: decir cual de los dos datos fallo
            // permitiria averiguar que emails estan registrados en el sistema.
            return Unauthorized(new ErrorResponse("Email o contrasena incorrectos."));
        }

        return Ok(sesion);
    }
}
