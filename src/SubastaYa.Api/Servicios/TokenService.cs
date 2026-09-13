using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.Servicios;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuracion;

    public TokenService(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
    {
        var minutos = int.Parse(_configuracion["Jwt:ExpiracionMinutos"]!);
        var expiraEn = DateTime.UtcNow.AddMinutes(minutos);

        // El claim "sub" (subject) lleva el id del usuario. Es la pieza central del
        // sistema: gracias a el, endpoints como /wallets/me y el motor de pujas saben
        // quien esta operando sin que el id viaje nunca en la URL ni en el body.
        // Si viniera del body, cualquiera podria pujar en nombre de otro.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre)
        };

        // La firma usa la misma clave y el mismo algoritmo que la validacion.
        var clave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuracion["Jwt:Clave"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Issuer"],
            audience: _configuracion["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
