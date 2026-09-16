using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.Servicios;

public interface ITokenService
{
    /// <summary>
    /// Emite el token firmado de un usuario ya autenticado, con su expiracion en UTC.
    /// Lo que se firma aca debe coincidir con lo que valida Program.cs: mismo emisor,
    /// destinatario, clave y algoritmo.
    /// </summary>
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}
