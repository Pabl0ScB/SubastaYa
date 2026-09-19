using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Domain.Repositorios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeBilleteras : IServicioDeBilleteras
{
    // Todo usuario recibe su billetera al registrarse, en la misma transaccion. Que falte
    // significa que esa transaccion se rompio, no que el usuario no cargo saldo.
    private const string SinBilletera = "El usuario no tiene una billetera asociada.";

    private readonly AppDbContext _contexto;
    private readonly IAsientoLedgerRepository _ledger;
    private readonly IServicioDeAuditoria _auditoria;

    public ServicioDeBilleteras(
        AppDbContext contexto,
        IAsientoLedgerRepository ledger,
        IServicioDeAuditoria auditoria)
    {
        _contexto = contexto;
        _ledger = ledger;
        _auditoria = auditoria;
    }

    public async Task<BilleteraResponse> ObtenerPorUsuarioAsync(int usuarioId)
    {
        var billetera = await _contexto.Billeteras
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId)
            ?? throw new RecursoNoEncontradoException(SinBilletera);

        return BilleteraResponse.Desde(billetera);
    }

    public async Task<BilleteraResponse> DepositarAsync(int usuarioId, decimal monto)
    {
        // El saldo y su asiento contable entran juntos o no entra ninguno. Un saldo sin
        // asiento seria plata sin origen, y un asiento sin saldo, plata que no aparece.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var billetera = await _contexto.Billeteras
            .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId)
            ?? throw new RecursoNoEncontradoException(SinBilletera);

        var saldoAnterior = billetera.SaldoTotal;
        billetera.SaldoTotal += monto;

        // EF no incrementa solo un token de concurrencia de tipo int. Sin esta linea el
        // UPDATE ... WHERE Version = @leida siempre coincide, y dos operaciones simultaneas
        // sobre la misma billetera se pisarian sin que nadie lo detecte.
        billetera.Version++;

        await _ledger.AgregarAsync(new AsientoLedger
        {
            BilleteraId = billetera.Id,
            Tipo = TipoAsiento.Deposito,
            Monto = monto,
            Descripcion = "Deposito manual",
            Fecha = DateTime.UtcNow
        });

        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException(
                "El saldo cambio mientras se procesaba el deposito. Volve a intentarlo.");
        }

        // Dentro de la transaccion a proposito: si el deposito no llega a confirmarse, no
        // tiene que quedar registrada una acreditacion que no ocurrio. Es lo contrario de
        // la auditoria de un rechazo, que va fuera porque el rollback la borraria.
        await _auditoria.RegistrarAsync(
            EntidadesAuditables.Billetera,
            billetera.Id,
            AccionesAuditoria.AcreditacionManual,
            usuarioId,
            new { monto, saldoAnterior, saldoNuevo = billetera.SaldoTotal });

        await transaccion.CommitAsync();

        return BilleteraResponse.Desde(billetera);
    }
}
