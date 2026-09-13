using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeAutenticacion
{
    /// <summary>
    /// Valida las credenciales y devuelve el token. Devuelve null si el email no existe o
    /// la contrasena no coincide: los dos casos se tratan igual para no revelar cual fallo.
    /// </summary>
    Task<SesionResponse?> IniciarSesionAsync(LoginRequest solicitud);
}
