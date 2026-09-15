using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SubastaYa.Api.Servicios;

/// <summary>Obtiene el id del usuario autenticado a partir del claim "sub" del token.</summary>
public static class UsuarioActual
{
    public static int ObtenerId(ClaimsPrincipal usuario)
    {
        var sub = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub);

        // Solo puede fallar si el token no lo emitio TokenService, asi que seria un error
        // del servidor y no del cliente: por eso no se traduce a un 401 ni a un 422.
        return int.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("El token no trae un claim 'sub' numerico.");
    }
}
