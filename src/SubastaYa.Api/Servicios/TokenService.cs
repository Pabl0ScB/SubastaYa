using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.Servicios;

public class TokenService : ITokenService
{
    private static readonly JwtSecurityTokenHandler Handler = new();

    private readonly string _clave;
    private readonly string _emisor;
    private readonly string _destinatario;
    private readonly int _minutosDeVigencia;

    public TokenService(IConfiguration configuracion)
    {
        // Se leen al construir el servicio y no en cada emision: si falta un valor, el
        // error dice cual, en vez de reventar con una referencia nula mas adentro.
        _clave = Requerido(configuracion, "Jwt:Clave");
        _emisor = Requerido(configuracion, "Jwt:Issuer");
        _destinatario = Requerido(configuracion, "Jwt:Audience");
        _minutosDeVigencia = int.Parse(Requerido(configuracion, "Jwt:ExpiracionMinutos"));
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
    {
        var ahora = DateTime.UtcNow;
        var expiraEn = ahora.AddMinutes(_minutosDeVigencia);

        // El claim "sub" lleva el id. Gracias a el los endpoints "/me" y el registro de
        // pujas saben quien opera sin que el id viaje en la URL ni en el body; si viniera
        // del body, cualquiera podria actuar en nombre de otro.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _emisor,
            audience: _destinatario,
            claims: claims,
            notBefore: ahora,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (Handler.WriteToken(token), expiraEn);
    }

    private static string Requerido(IConfiguration configuracion, string clave) =>
        configuracion[clave]
        ?? throw new InvalidOperationException($"Falta la configuracion '{clave}'.");
}
