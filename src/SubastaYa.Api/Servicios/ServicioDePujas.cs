using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Domain.Repositorios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDePujas : IServicioDePujas
{
    private readonly AppDbContext _contexto;
    private readonly IAsientoLedgerRepository _ledger;
    private readonly IServicioDeAuditoria _auditoria;

    public ServicioDePujas(
        AppDbContext contexto,
        IAsientoLedgerRepository ledger,
        IServicioDeAuditoria auditoria)
    {
        _contexto = contexto;
        _ledger = ledger;
        _auditoria = auditoria;
    }

    // Dos etapas separadas: primero se rechaza todo lo que no puede ser una oferta valida,
    // sin tocar plata; recien despues se abre la transaccion que mueve saldos.
    public async Task<PujaRegistradaResponse> RegistrarPujaAsync(int subastaId, int usuarioId, decimal monto)
    {
        var (subasta, billetera) = await ValidarAsync(subastaId, usuarioId, monto);
        return await EjecutarTransaccionAsync(subasta, billetera, usuarioId, monto);
    }

    public async Task<IReadOnlyList<PujaResponse>> ObtenerHistorialAsync(int subastaId)
    {
        var existe = await _contexto.Subastas.AnyAsync(s => s.Id == subastaId);
        if (!existe)
        {
            throw new RecursoNoEncontradoException("La subasta no existe.");
        }

        return await _contexto.Pujas
            .AsNoTracking()
            .Where(p => p.SubastaId == subastaId)
            .OrderByDescending(p => p.FechaPuja)
            .ThenByDescending(p => p.Id)
            .Select(p => new PujaResponse
            {
                Id = p.Id,
                Monto = p.Monto,
                FechaPuja = p.FechaPuja,
                Seudonimo = p.Comprador.Seudonimo
            })
            .ToListAsync();
    }

    // Devuelve la subasta y la billetera del postor cargadas CON seguimiento de cambios,
    // porque la transaccion las modifica: su Version leida aca es la que se compara al
    // guardar, y eso es lo que detecta una oferta simultanea.
    //
    // Las seis validaciones van antes de abrir la transaccion, en el orden en que un
    // usuario esperaria que se le expliquen: primero si puede pujar, recien despues si
    // la oferta alcanza. El 403 va segundo, antes de mirar el estado de la subasta: a un
    // vendedor que puja en lo suyo hay que decirle "no podes", no "la subasta vencio".
    private async Task<(Subasta Subasta, Billetera Billetera)> ValidarAsync(
        int subastaId, int usuarioId, decimal monto)
    {
        var subasta = await _contexto.Subastas
            .FirstOrDefaultAsync(s => s.Id == subastaId)
            ?? throw new RecursoNoEncontradoException("La subasta no existe.");

        if (subasta.VendedorId == usuarioId)
        {
            await Auditar(subasta.Id, usuarioId, monto, "El vendedor no puede pujar en su propia subasta.");
            throw new AccionProhibidaException("No podés pujar en tu propia subasta.");
        }

        if (subasta.Estado != EstadoSubasta.Activa)
        {
            await Auditar(subasta.Id, usuarioId, monto, "La subasta no está activa.");
            throw new ReglaDeNegocioException("La subasta no está activa.");
        }

        if (DateTime.UtcNow >= subasta.FechaFin)
        {
            await Auditar(subasta.Id, usuarioId, monto, "La subasta ya venció.");
            throw new ReglaDeNegocioException("La subasta ya venció.");
        }

        if (subasta.LiderId == usuarioId)
        {
            await Auditar(subasta.Id, usuarioId, monto, "El usuario ya es el líder de la subasta.");
            throw new ReglaDeNegocioException("Ya sos el líder de esta subasta.");
        }

        var montoMinimo = subasta.PujaActual + subasta.IncrementoMinimo;
        if (monto < montoMinimo)
        {
            await Auditar(subasta.Id, usuarioId, monto, $"El monto no alcanza el minimo de {montoMinimo}.");
            throw new ReglaDeNegocioException($"La oferta debe ser de al menos ${montoMinimo}.");
        }

        var billetera = await _contexto.Billeteras
            .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId)
            ?? throw new InvalidOperationException("El usuario no tiene billetera.");

        if (billetera.SaldoDisponible < monto)
        {
            await _auditoria.RegistrarAsync(
                EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.PujaRechazadaSaldo,
                usuarioId, new { monto, disponible = billetera.SaldoDisponible });
            throw new ReglaDeNegocioException(
                $"Saldo insuficiente. Disponible: ${billetera.SaldoDisponible}.");
        }

        return (subasta, billetera);
    }

    private Task Auditar(int subastaId, int usuarioId, decimal monto, string motivo)
        => _auditoria.RegistrarAsync(
            EntidadesAuditables.Subasta, subastaId, AccionesAuditoria.PujaRechazadaValidacion,
            usuarioId, new { monto, motivo });

    private Task<PujaRegistradaResponse> EjecutarTransaccionAsync(
        Subasta subasta, Billetera billetera, int usuarioId, decimal monto)
        => throw new NotImplementedException();
}
