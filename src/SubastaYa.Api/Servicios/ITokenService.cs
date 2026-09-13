using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.Servicios;

/// <summary>
/// Emite los JWT que la API acepta.
/// Es la contraparte de la validacion configurada en Program.cs: lo que se firma aca
/// tiene que coincidir con lo que alli se valida (mismo emisor, mismo destinatario,
/// misma clave y mismo algoritmo). Si las dos mitades se desincronizan, el login
/// devuelve un token con exito pero cualquier peticion posterior responde 401.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera el token firmado para un usuario ya autenticado, junto con el instante
    /// en que expira (en UTC).
    /// </summary>
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}
