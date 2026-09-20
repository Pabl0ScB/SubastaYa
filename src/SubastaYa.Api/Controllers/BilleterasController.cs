using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/wallets")]
[Authorize]
public class BilleterasController : ControllerBase
{
    private readonly IServicioDeBilleteras _servicio;

    public BilleterasController(IServicioDeBilleteras servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Devuelve los saldos de la billetera del usuario autenticado.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(BilleteraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BilleteraResponse>> ObtenerActual()
    {
        // La ruta dice "me" y no recibe un id: asi no hay forma de pedir la billetera de
        // otro. Con /wallets/{id} habria que comparar ese id contra el del token en cada
        // endpoint, y el dia que alguien olvide la comparacion queda expuesto el saldo ajeno.
        var usuarioId = UsuarioActual.ObtenerId(User);
        return Ok(await _servicio.ObtenerPorUsuarioAsync(usuarioId));
    }

    /// <summary>Acredita saldo en la billetera del usuario autenticado.</summary>
    [HttpPost("me/deposits")]
    [ProducesResponseType(typeof(BilleteraResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BilleteraResponse>> Depositar([FromBody] DepositoRequest solicitud)
    {
        var usuarioId = UsuarioActual.ObtenerId(User);
        var billetera = await _servicio.DepositarAsync(usuarioId, solicitud.Monto);

        // 201 con Location hacia los saldos: lo que se devuelve es el estado resultante
        // de la billetera, que es donde se ve el efecto del deposito.
        return CreatedAtAction(nameof(ObtenerActual), billetera);
    }

    /// <summary>Devuelve los movimientos de la billetera, del mas reciente al mas viejo.</summary>
    [HttpGet("me/entries")]
    [ProducesResponseType(typeof(PaginaResponse<MovimientoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResponse<MovimientoResponse>>> ObtenerMovimientos(
        [FromQuery] PaginacionRequest paginacion)
    {
        var usuarioId = UsuarioActual.ObtenerId(User);
        return Ok(await _servicio.ObtenerMovimientosAsync(usuarioId, paginacion));
    }
}
