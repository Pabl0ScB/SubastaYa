using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDePujas
{
    /// <summary>
    /// Registra una oferta del usuario sobre la subasta, con la retencion de su saldo y la
    /// liberacion del lider anterior en una sola transaccion.
    /// Lanza RecursoNoEncontradoException (404) si la subasta no existe,
    /// AccionProhibidaException (403) si el usuario es el vendedor,
    /// ReglaDeNegocioException (422) si la oferta no cumple las reglas de la subasta, y
    /// ConflictoDeConcurrenciaException (409) si otra oferta se confirmo primero.
    /// </summary>
    Task<PujaRegistradaResponse> RegistrarPujaAsync(int subastaId, int usuarioId, decimal monto);

    /// <summary>
    /// Ofertas de una subasta, de la mas reciente a la mas vieja. Lanza
    /// RecursoNoEncontradoException si la subasta no existe.
    /// </summary>
    Task<IReadOnlyList<PujaResponse>> ObtenerHistorialAsync(int subastaId);
}
