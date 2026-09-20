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

    public Task<IReadOnlyList<PujaResponse>> ObtenerHistorialAsync(int subastaId)
        => throw new NotImplementedException();

    // Devuelve la subasta y la billetera del postor cargadas CON seguimiento de cambios,
    // porque la transaccion las modifica: su Version leida aca es la que se compara al
    // guardar, y eso es lo que detecta una oferta simultanea.
    private Task<(Subasta Subasta, Billetera Billetera)> ValidarAsync(
        int subastaId, int usuarioId, decimal monto)
        => throw new NotImplementedException();

    private Task<PujaRegistradaResponse> EjecutarTransaccionAsync(
        Subasta subasta, Billetera billetera, int usuarioId, decimal monto)
        => throw new NotImplementedException();
}
