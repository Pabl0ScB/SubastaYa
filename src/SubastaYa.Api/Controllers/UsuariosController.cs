using Microsoft.AspNetCore.Authorization;
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
        var usuario = await _servicio.RegistrarAsync(solicitud);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    /// <summary>Devuelve los datos del usuario autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerActual()
    {
        // El id no se recibe por parametro: sale del token.
        var id = UsuarioActual.ObtenerId(User);
        return Ok(await _servicio.ObtenerPorIdAsync(id));
    }
}
