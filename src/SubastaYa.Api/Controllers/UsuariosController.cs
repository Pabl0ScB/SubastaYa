using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsuariosController : ControllerBase
{
    private readonly IServicioDeUsuarios _servicio;

    public UsuariosController(IServicioDeUsuarios servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Registra un usuario nuevo y le crea su billetera.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest solicitud)
    {
        // Sin try/catch: si el servicio lanza RecursoDuplicadoException, el middleware
        // global la traduce a 409. El controlador solo se ocupa de HTTP.
        var usuario = await _servicio.RegistrarAsync(solicitud);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }
}
