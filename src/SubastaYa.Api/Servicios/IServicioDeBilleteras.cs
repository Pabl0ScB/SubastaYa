using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeBilleteras
{
    /// <summary>
    /// Devuelve los saldos de la billetera del usuario. Lanza RecursoNoEncontradoException
    /// si no la tiene.
    /// </summary>
    Task<BilleteraResponse> ObtenerPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Acredita el monto en la billetera del usuario y devuelve los saldos resultantes.
    /// Lanza ConflictoDeConcurrenciaException si la billetera cambio mientras se procesaba.
    /// </summary>
    Task<BilleteraResponse> DepositarAsync(int usuarioId, decimal monto);

    /// <summary>
    /// Devuelve una pagina de los movimientos de la billetera del usuario, del mas reciente
    /// al mas viejo. Lanza RecursoNoEncontradoException si no tiene billetera.
    /// </summary>
    Task<PaginaResponse<MovimientoResponse>> ObtenerMovimientosAsync(
        int usuarioId, PaginacionRequest paginacion);
}
