using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeUsuarios
{
    /// <summary>
    /// Da de alta un usuario y su billetera. Lanza RecursoDuplicadoException si el email o
    /// el seudonimo ya estan en uso.
    /// </summary>
    Task<UsuarioResponse> RegistrarAsync(RegistroRequest solicitud);

    /// <summary>Lanza RecursoNoEncontradoException si el usuario no existe.</summary>
    Task<UsuarioResponse> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Devuelve una fila por cada subasta en la que el usuario oferto: primero las
    /// abiertas, de la que cierra antes a la que cierra despues, y al final las cerradas,
    /// de la mas reciente a la mas vieja.
    /// </summary>
    Task<IReadOnlyList<MiParticipacionResponse>> ObtenerParticipacionesAsync(int usuarioId);
}
