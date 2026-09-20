using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/auctions/{subastaId:int}/bids")]
public class PujasController : ControllerBase
{
    private readonly IServicioDePujas _servicio;

    public PujasController(IServicioDePujas servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Registra una oferta del usuario autenticado sobre la subasta.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(PujaRegistradaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ConflictoPujaResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PujaRegistradaResponse>> Crear(
        int subastaId, CrearPujaRequest request)
    {
        var usuarioId = UsuarioActual.ObtenerId(User);
        var puja = await _servicio.RegistrarPujaAsync(subastaId, usuarioId, request.Monto);

        return CreatedAtAction(nameof(ObtenerHistorial), new { subastaId }, puja);
    }

    /// <summary>Devuelve las ofertas de la subasta, de la mas reciente a la mas vieja.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PujaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PujaResponse>>> ObtenerHistorial(int subastaId)
        => Ok(await _servicio.ObtenerHistorialAsync(subastaId));
}
