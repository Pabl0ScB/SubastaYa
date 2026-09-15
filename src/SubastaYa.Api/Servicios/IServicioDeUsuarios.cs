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
}
