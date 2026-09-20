using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
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
            throw new RecursoDuplicadoException("Ese seudónimo ya está en uso.");

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

    public async Task<IReadOnlyList<MiParticipacionResponse>> ObtenerParticipacionesAsync(
        int usuarioId)
    {
        // Una fila por subasta y no por puja: la pantalla lista subastas donde participo.
        // El detalle de cada oferta ya esta en la sala en vivo y en la billetera.
        var subastas = await _contexto.Subastas
            .AsNoTracking()
            .Where(s => s.Pujas.Any(p => p.CompradorId == usuarioId))
            .Select(s => new
            {
                s.Id,
                s.Titulo,
                s.UrlImagen,
                s.Estado,
                s.PujaActual,
                s.FechaFin,
                s.LiderId,
                MiOfertaMaxima = s.Pujas
                    .Where(p => p.CompradorId == usuarioId)
                    .Max(p => p.Monto)
            })
            .ToListAsync();

        // El orden se resuelve en memoria: son pocas filas por usuario, y en SQL haria
        // falta una fecha de relleno para ordenar distinto las abiertas y las cerradas.
        var abiertas = subastas
            .Where(s => s.Estado == EstadoSubasta.Activa)
            .OrderBy(s => s.FechaFin);
        var cerradas = subastas
            .Where(s => s.Estado != EstadoSubasta.Activa)
            .OrderByDescending(s => s.FechaFin);

        return abiertas.Concat(cerradas)
            .Select(s => new MiParticipacionResponse
            {
                SubastaId = s.Id,
                Titulo = s.Titulo,
                UrlImagen = s.UrlImagen,
                Estado = s.Estado.ToString(),
                MiOfertaMaxima = s.MiOfertaMaxima,
                PujaActual = s.PujaActual,
                FechaFin = s.FechaFin,
                Resultado = CalcularResultado(s.Estado, s.LiderId == usuarioId)
            })
            .ToList();
    }

    // Se usa LiderId y no se busca la puja mas alta: el lider lo actualiza el motor en la
    // misma transaccion que la puja, y recalcularlo aca seria tener dos fuentes para el
    // mismo dato. Todo sale del Estado y no de la fecha, igual que en el catalogo: una
    // subasta vencida que el worker todavia no cerro sigue abierta.
    private static string CalcularResultado(EstadoSubasta estado, bool esLider) =>
        estado == EstadoSubasta.Finalizada
            ? (esLider ? "Ganada" : "No ganada")
            : (esLider ? "Liderando" : "Superado");
}
