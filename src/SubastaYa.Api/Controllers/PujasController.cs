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

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(PujaRegistradaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ConflictoPujaResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PujaRegistradaResponse>> Crear(
        int subastaId, CrearPujaRequest request)
    {
        var usuarioId = UsuarioActual.ObtenerId(User);
        var puja = await _servicio.RegistrarPujaAsync(subastaId, usuarioId, request.Monto);

        return CreatedAtAction(nameof(ObtenerHistorial), new { subastaId }, puja);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PujaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PujaResponse>>> ObtenerHistorial(int subastaId)
        => Ok(await _servicio.ObtenerHistorialAsync(subastaId));
}
