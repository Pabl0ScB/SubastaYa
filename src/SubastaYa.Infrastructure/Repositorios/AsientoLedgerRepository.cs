using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Repositorios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Infrastructure.Repositorios;

/// <summary>
/// Acceso al libro mayor. Solo agrega y consulta: la interfaz no declara actualizar ni
/// borrar, y esta implementacion tampoco los ofrece. Un asiento que se puede editar deja
/// de servir como respaldo de un saldo.
/// </summary>
public class AsientoLedgerRepository : IAsientoLedgerRepository
{
    private readonly AppDbContext _contexto;

    public AsientoLedgerRepository(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <summary>
    /// Suma el asiento al contexto sin confirmarlo. Quien llama decide cuando escribir,
    /// porque un asiento nunca va solo: acompana un cambio de saldo y los dos tienen que
    /// entrar o no entrar juntos. Si este metodo confirmara por su cuenta, tambien
    /// escribiria cualquier cambio a medio hacer que hubiera en el contexto.
    /// </summary>
    public Task AgregarAsync(AsientoLedger asiento)
    {
        _contexto.AsientosLedger.Add(asiento);
        return Task.CompletedTask;
    }

    /// <summary>Historial de movimientos de una billetera, del mas reciente al mas viejo.</summary>
    public async Task<IReadOnlyList<AsientoLedger>> ObtenerPorBilleteraAsync(
        int billeteraId, int pagina, int tamanoPagina)
    {
        return await _contexto.AsientosLedger
            .AsNoTracking()
            .Where(a => a.BilleteraId == billeteraId)
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
    }

    /// <summary>
    /// Saldo total reconstruido a partir de los movimientos. Es la contraparte de
    /// <c>Billetera.SaldoTotal</c>: si los dos valores no coinciden, hay un movimiento sin
    /// asiento o un asiento sin movimiento.
    /// </summary>
    public async Task<decimal> SumarPorBilleteraAsync(int billeteraId)
    {
        // Retencion y Liberacion no aparecen: mueven el saldo retenido, no el total. La
        // plata retenida sigue siendo del usuario hasta que la subasta se liquida.
        var total = await _contexto.AsientosLedger
            .AsNoTracking()
            .Where(a => a.BilleteraId == billeteraId)
            .SumAsync(a => (decimal?)(
                a.Tipo == TipoAsiento.Deposito || a.Tipo == TipoAsiento.Cobro ? a.Monto
                : a.Tipo == TipoAsiento.Pago ? -a.Monto
                : 0m));

        // SUM sobre cero filas devuelve NULL en SQL, no cero: le pasa a toda billetera
        // recien creada, que todavia no tiene ningun movimiento.
        return total ?? 0m;
    }
}
