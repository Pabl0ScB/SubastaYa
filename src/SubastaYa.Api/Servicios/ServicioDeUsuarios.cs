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
        // El email se normaliza a minusculas: sin esto, "Ana@test.com" y "ana@test.com"
        // serian dos cuentas distintas y el indice unico no serviria de nada.
        var email = solicitud.Email.Trim().ToLowerInvariant();
        var seudonimo = solicitud.Seudonimo.Trim();

        // Reglas de negocio: se validan aca, no en el controlador, y no son lo mismo
        // que las anotaciones del DTO (esas ya rechazaron el formato con un 400).
        if (await _contexto.Usuarios.AnyAsync(u => u.Email == email))
            throw new RecursoDuplicadoException("Ya existe una cuenta registrada con ese email.");

        if (await _contexto.Usuarios.AnyAsync(u => u.Seudonimo == seudonimo))
            throw new RecursoDuplicadoException("Ese seudonimo ya esta en uso.");

        // Transaccion explicita: el usuario y su billetera se crean juntos o no se crea
        // ninguno. Un usuario sin billetera reventaria mas adelante en el motor de pujas,
        // que la busca para retener saldo y se encontraria con null.
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

        return new UsuarioResponse
        {
            Id = usuario.Id,
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Seudonimo = usuario.Seudonimo,
            FechaRegistro = usuario.FechaRegistro
        };
    }
}
