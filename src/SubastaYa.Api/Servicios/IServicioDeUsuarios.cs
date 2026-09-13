using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeUsuarios
{
    /// <summary>
    /// Registra un usuario nuevo junto con su billetera.
    /// Lanza RecursoDuplicadoException si el email o el seudonimo ya estan en uso.
    /// </summary>
    Task<UsuarioResponse> RegistrarAsync(RegistroRequest solicitud);
}
