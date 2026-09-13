using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeAutenticacion
{
    /// <summary>
    /// Valida las credenciales y devuelve el token con los datos del usuario.
    /// Devuelve null si el email no existe o la contrasena no coincide: los dos casos
    /// se tratan igual a proposito, para no revelar cual de los dos fallo.
    /// </summary>
    Task<SesionResponse?> IniciarSesionAsync(LoginRequest solicitud);
}
