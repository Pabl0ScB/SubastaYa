using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
