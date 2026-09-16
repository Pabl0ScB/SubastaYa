using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeAutenticacion : IServicioDeAutenticacion
{
    // Hash contra el que se verifica cuando el email no existe. Se genera al iniciar en
    // vez de dejarlo escrito como literal: un literal mal copiado no seria un hash valido
    // y BCrypt lanzaria una excepcion en lugar de devolver false.
    private static readonly string HashDeDescarte =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

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
        // Misma normalizacion que en el registro: sin esto, quien se dio de alta
        // escribiendo "Ana@Test.com" no podria volver a entrar.
        var email = solicitud.Email.Trim().ToLowerInvariant();

        var usuario = await _contexto.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        // Se verifica el hash aunque el usuario no exista. Si se respondiera de inmediato
        // ante un email desconocido y se tardaran los milisegundos de BCrypt cuando
        // existe, esa diferencia de tiempo permitiria averiguar que emails estan
        // registrados.
        var passwordValida = _passwords.Verificar(
            solicitud.Password,
            usuario?.PasswordHash ?? HashDeDescarte);

        if (usuario is null || !passwordValida)
        {
            return null;
        }

        var (token, expiraEn) = _tokens.GenerarToken(usuario);

        return new SesionResponse
        {
            Token = token,
            ExpiraEn = expiraEn,
            Usuario = UsuarioResponse.Desde(usuario)
        };
    }
}
