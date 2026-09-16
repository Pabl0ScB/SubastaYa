using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeUsuarios : IServicioDeUsuarios
{
    private readonly AppDbContext _contexto;
    private readonly IServicioDePasswords _passwords;

    public ServicioDeUsuarios(AppDbContext contexto, IServicioDePasswords passwords)
    {
        _contexto = contexto;
        _passwords = passwords;
    }

    public async Task<UsuarioResponse> RegistrarAsync(RegistroRequest solicitud)
    {
        // Se normaliza el email: sin esto "Ana@test.com" y "ana@test.com" serian dos
        // cuentas distintas y el indice unico no cumpliria su funcion.
        var email = solicitud.Email.Trim().ToLowerInvariant();
        var seudonimo = solicitud.Seudonimo.Trim();

        if (await _contexto.Usuarios.AnyAsync(u => u.Email == email))
            throw new RecursoDuplicadoException("Ya existe una cuenta registrada con ese email.");

        if (await _contexto.Usuarios.AnyAsync(u => u.Seudonimo == seudonimo))
            throw new RecursoDuplicadoException("Ese seudonimo ya esta en uso.");

        // Transaccion explicita: el usuario y su billetera se crean juntos o no se crea
        // ninguno. Un usuario sin billetera fallaria al pujar, cuando se busque su saldo.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = _passwords.Hashear(solicitud.Password),
            Nombre = solicitud.Nombre.Trim(),
            Seudonimo = seudonimo,
            FechaRegistro = DateTime.UtcNow,
            Billetera = new Billetera
            {
                SaldoTotal = 0m,
                SaldoRetenido = 0m,
                Version = 1
            }
        };

        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        return UsuarioResponse.Desde(usuario);
    }

    public async Task<UsuarioResponse> ObtenerPorIdAsync(int id)
    {
        var usuario = await _contexto.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
        {
            throw new RecursoNoEncontradoException("El usuario no existe.");
        }

        return UsuarioResponse.Desde(usuario);
    }
}
