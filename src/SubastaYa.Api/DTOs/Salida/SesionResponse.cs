namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Respuesta del login: el token firmado y los datos del usuario autenticado.
/// Incluye al usuario para que el frontend pueda mostrar el seudonimo en la barra
/// de navegacion sin tener que hacer una segunda peticion apenas inicia sesion.
/// </summary>
public class SesionResponse
{
    public string Token { get; set; } = null!;

    /// <summary>Instante de expiracion del token, en UTC.</summary>
    public DateTime ExpiraEn { get; set; }

    public UsuarioResponse Usuario { get; set; } = null!;
}
