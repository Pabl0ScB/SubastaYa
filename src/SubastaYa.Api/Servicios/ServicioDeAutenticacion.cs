using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeAutenticacion : IServicioDeAutenticacion
{
    /// <summary>
    /// Hash valido de descarte, usado cuando el email no existe. Verificar contra el
    /// cuesta lo mismo que verificar contra uno real, que es justamente el punto:
    /// ver la nota sobre el tiempo de respuesta mas abajo.
    /// </summary>
    private const string HashDeDescarte =
        "$2a$11$3S1kU7VWkXqZ9c5nZ0bZ8OqQ2m9wH1Yk8rJ1sYyB3cQeF6vT0aXqW";

    private readonly AppDbContext _contexto;
    private readonly IServicioDePasswords _passwords;
    private readonly ITokenService _tokens;

    public ServicioDeAutenticacion(
        AppDbContext contexto,
        IServicioDePasswords passwords,
        ITokenService tokens)
    {
        _contexto = contexto;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<SesionResponse?> IniciarSesionAsync(LoginRequest solicitud)
    {
        // Misma normalizacion que en el registro. Sin esto, quien se registro
        // escribiendo "Ana@Test.com" no podria volver a entrar nunca.
        var email = solicitud.Email.Trim().ToLowerInvariant();

        var usuario = await _contexto.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        // Se verifica el hash SIEMPRE, incluso si el usuario no existe.
        // Si se respondiera de inmediato ante un email inexistente y se tardara los
        // milisegundos de BCrypt cuando existe, un atacante podria distinguir los dos
        // casos midiendo el tiempo de respuesta y deducir que emails estan registrados.
        var hash = usuario?.PasswordHash ?? HashDeDescarte;
        var passwordValida = _passwords.Verificar(solicitud.Password, hash);

        if (usuario is null || !passwordValida)
        {
            return null;
        }

        var (token, expiraEn) = _tokens.GenerarToken(usuario);

        return new SesionResponse
        {
            Token = token,
            ExpiraEn = expiraEn,
            Usuario = new UsuarioResponse
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Nombre = usuario.Nombre,
                Seudonimo = usuario.Seudonimo,
                FechaRegistro = usuario.FechaRegistro
            }
        };
    }
}
