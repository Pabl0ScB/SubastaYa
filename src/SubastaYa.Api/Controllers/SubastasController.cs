using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;
using Microsoft.AspNetCore.Authorization;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Domain.Entidades;

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
    public async Task<ActionResult<PaginaResponse<SubastaTarjetaResponse>>> ObtenerCatalogo(
        [FromQuery] FiltroSubastasRequest filtro)
        => Ok(await _servicio.ObtenerCatalogoAsync(filtro));
    [HttpGet("{id}")]
    public async Task<ActionResult<SubastaDetalleResponse>> ObtenerPorId(int id)
    {
        var respuesta = await _servicio.ObtenerDetalleAsync(id);
        return Ok(respuesta);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Subasta>> Publicar(PublicarSubastaRequest request)
    {
        var vendedorId = UsuarioActual.ObtenerId(User);
        var subasta = await _servicio.PublicarAsync(request, vendedorId);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = subasta.Id }, subasta);
    }
}