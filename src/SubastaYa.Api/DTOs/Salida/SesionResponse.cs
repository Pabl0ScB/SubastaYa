namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Respuesta del login. Incluye al usuario para que el frontend pueda mostrar el
/// seudonimo apenas inicia sesion, sin una segunda peticion.
/// </summary>
public class SesionResponse
{
    public string Token { get; set; } = null!;

    /// <summary>Instante de expiracion del token, en UTC.</summary>
    public DateTime ExpiraEn { get; set; }

    public UsuarioResponse Usuario { get; set; } = null!;
}
