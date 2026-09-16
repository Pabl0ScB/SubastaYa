using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeBilleteras
{
    /// <summary>
    /// Devuelve los saldos de la billetera del usuario. Lanza RecursoNoEncontradoException
    /// si no la tiene.
    /// </summary>
    Task<BilleteraResponse> ObtenerPorUsuarioAsync(int usuarioId);
}
