using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/auctions")]
public class SubastasController : ControllerBase
{
    private readonly IServicioDeSubastas _servicio;

    public SubastasController(IServicioDeSubastas servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResponse<SubastaTarjetaResponse>>> ObtenerCatalogo(
        [FromQuery] FiltroSubastasRequest filtro)
        => Ok(await _servicio.ObtenerCatalogoAsync(filtro));

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubastaDetalleResponse>> ObtenerPorId(int id)
    {
        var respuesta = await _servicio.ObtenerDetalleAsync(id);
        return Ok(respuesta);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubastaDetalleResponse>> Publicar(PublicarSubastaRequest request)
    {
        var vendedorId = UsuarioActual.ObtenerId(User);
        var subasta = await _servicio.PublicarAsync(request, vendedorId);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = subasta.Id }, subasta);
    }

    [Authorize]
    [HttpGet("~/api/v1/users/me/auctions")]
    [ProducesResponseType(typeof(IReadOnlyList<MiPublicacionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MiPublicacionResponse>>> ObtenerMisPublicaciones()
    {
        var vendedorId = UsuarioActual.ObtenerId(User);
        return Ok(await _servicio.ObtenerMisPublicacionesAsync(vendedorId));
    }
}