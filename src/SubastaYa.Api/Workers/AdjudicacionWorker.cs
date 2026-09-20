using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubastaYa.Api.Configuracion;
using SubastaYa.Api.Hubs;
using SubastaYa.Api.Servicios;
using SubastaYa.Domain;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Repositorios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Workers;

/// <summary>
/// Proceso en segundo plano que hace avanzar las subastas sin que nadie las toque:
/// activa las programadas cuando llega su hora, liquida las que vencieron con ganador y
/// marca desiertas las que vencieron sin ofertas. Es el unico que ejecuta transiciones
/// desde Activa: ningun endpoint cierra una subasta.
/// </summary>
public class AdjudicacionWorker : BackgroundService
{
    // El worker es Singleton y el DbContext es Scoped: no se puede inyectar el contexto
    // aca (la aplicacion ni siquiera arrancaria). Ademas un contexto vivo durante horas
    // acumularia entidades en su seguimiento y terminaria leyendo datos viejos. Por eso
    // se pide una fabrica de scopes y cada ciclo abre el suyo, como una peticion.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpcionesWorker _opciones;

    // A diferencia del DbContext, el IHubContext se puede inyectar en este worker
    // singleton: tambien es singleton y no guarda el estado de ninguna conexion. El Hub
    // en cambio se crea y se descarta con cada llamada, asi que nunca hay una instancia
    // a la que pedirle que avise algo.
    private readonly IHubContext<SubastaHub> _hub;
    private readonly ILogger<AdjudicacionWorker> _logger;

    public AdjudicacionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OpcionesWorker> opciones,
        IHubContext<SubastaHub> hub,
        ILogger<AdjudicacionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        // Un intervalo en cero dejaria el ciclo girando sin pausa contra la base: el
        // minimo de un segundo evita que una configuracion mal escrita tire la API.
        var segundos = Math.Max(1, _opciones.IntervaloSegundos);
        var intervalo = TimeSpan.FromSeconds(segundos);
        _logger.LogInformation("Worker de adjudicacion iniciado, revisando cada {Segundos} segundos.",
            segundos);

        while (!cancelacion.IsCancellationRequested)
        {
            // El try va adentro del while y no afuera. Si una excepcion escapara de
            // ExecuteAsync, el worker moriria en silencio: la aplicacion sigue
            // respondiendo y las subastas dejan de cerrarse sin que nadie se entere.
            try
            {
                await ProcesarCicloAsync(cancelacion);
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo un ciclo del worker de adjudicacion.");
            }

            try
            {
                await Task.Delay(intervalo, cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Worker de adjudicacion detenido.");
    }

    private async Task ProcesarCicloAsync(CancellationToken cancelacion)
    {
        using var scope = _scopeFactory.CreateScope();

        var contexto  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ledger    = scope.ServiceProvider.GetRequiredService<IAsientoLedgerRepository>();
        var auditoria = scope.ServiceProvider.GetRequiredService<IServicioDeAuditoria>();

        // UTC en todo el recorrido: las fechas se guardan en columnas "timestamp with
        // time zone" y Npgsql rechaza un DateTime con Kind Local.
        var ahora = DateTime.UtcNow;

        // Las consultas del ciclo traen solo identificadores, sin seguimiento. Cada
        // subasta se vuelve a leer, ya con seguimiento, dentro de su propio paso: asi un
        // choque de versiones que obliga a limpiar el contexto no deja a las subastas
        // que faltan procesar desprendidas del seguimiento, que es como un cambio se
        // guarda "sin error" y no llega nunca a la base.
        var programadas = await contexto.Subastas
            .AsNoTracking()
            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= ahora)
            .Select(s => s.Id)
            .ToListAsync(cancelacion);

        foreach (var subastaId in programadas)
        {
            await ActivarAsync(contexto, subastaId, ahora, cancelacion);
        }

        // Una sola consulta para los dos cierres: se diferencian por tener lider o no, y
        // eso se decide al releer cada subasta.
        var vencidas = await contexto.Subastas
            .AsNoTracking()
            .Where(s => s.Estado == EstadoSubasta.Activa && s.FechaFin <= ahora)
            .Select(s => s.Id)
            .ToListAsync(cancelacion);

        foreach (var subastaId in vencidas)
        {
            await CerrarAsync(contexto, ledger, auditoria, subastaId, ahora, cancelacion);
        }
    }

    /// <summary>
    /// Programada -> Activa. No mueve dinero ni es reversible, asi que no abre
    /// transaccion: un unico SaveChanges ya es atomico.
    /// </summary>
    private async Task ActivarAsync(AppDbContext contexto, int subastaId, DateTime ahora, CancellationToken cancelacion)
    {
        // Se relee con la condicion completa, igual que el cierre: entre la lista del
        // ciclo y este momento la subasta pudo dejar de cumplirla.
        var subasta = await contexto.Subastas
            .FirstOrDefaultAsync(
                s => s.Id == subastaId && s.Estado == EstadoSubasta.Programada && s.FechaInicio <= ahora,
                cancelacion);
        if (subasta is null)
        {
            return;
        }

        subasta.Estado = EstadoSubasta.Activa;
        subasta.Version++;

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            _logger.LogInformation("Subasta {SubastaId} activada.", subasta.Id);
            await AvisarAsync(subasta.Id, "SubastaActivada", cancelacion);
        }
        catch (DbUpdateConcurrencyException)
        {
            Descartar(contexto, subastaId, "activar");
        }
    }

    private async Task CerrarAsync(
        AppDbContext contexto, IAsientoLedgerRepository ledger, IServicioDeAuditoria auditoria,
        int subastaId, DateTime ahora, CancellationToken cancelacion)
    {
        // Se relee con la condicion completa: si mientras tanto entro una oferta que
        // extendio el cierre por anti-sniping, la subasta ya no vencio y no hay nada que
        // hacer hasta que llegue su nueva fecha.
        var subasta = await contexto.Subastas
            .FirstOrDefaultAsync(
                s => s.Id == subastaId && s.Estado == EstadoSubasta.Activa && s.FechaFin <= ahora,
                cancelacion);
        if (subasta is null)
        {
            return;
        }

        if (subasta.LiderId is null)
        {
            await CerrarDesiertaAsync(contexto, auditoria, subasta, cancelacion);
        }
        else
        {
            await CerrarConGanadorAsync(contexto, ledger, auditoria, subasta, ahora, cancelacion);
        }
    }

    /// <summary>
    /// Vencida con ofertas: el saldo retenido del comprador pasa al vendedor y la
    /// subasta queda Finalizada. Todo adentro de una transaccion, incluido el cambio de
    /// estado: es lo unico que impide pagar dos veces si el proceso se corta en la mitad
    /// y el ciclo siguiente vuelve a encontrar la subasta.
    /// </summary>
    private async Task CerrarConGanadorAsync(
        AppDbContext contexto, IAsientoLedgerRepository ledger, IServicioDeAuditoria auditoria,
        Subasta subasta, DateTime ahora, CancellationToken cancelacion)
    {
        var compradorId = subasta.LiderId!.Value;
        var vendedorId = subasta.VendedorId;
        var monto = subasta.PujaActual;

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            var billeteraComprador = await contexto.Billeteras
                .FirstAsync(b => b.UsuarioId == compradorId, cancelacion);
            var billeteraVendedor = await contexto.Billeteras
                .FirstAsync(b => b.UsuarioId == vendedorId, cancelacion);

            // 1. El comprador paga: el dinero que tenia retenido deja de ser suyo, asi
            //    que baja del retenido y del total. Su saldo disponible no cambia.
            billeteraComprador.SaldoRetenido -= monto;
            billeteraComprador.SaldoTotal -= monto;
            billeteraComprador.Version++;

            await ledger.AgregarAsync(new AsientoLedger
            {
                BilleteraId = billeteraComprador.Id,
                Tipo = TipoAsiento.Pago,
                Monto = monto,
                SubastaId = subasta.Id,
                Descripcion = "Pago de la subasta ganada",
                Fecha = ahora
            });

            // 2. El vendedor cobra. Entra como disponible: no hay nada que retener.
            billeteraVendedor.SaldoTotal += monto;
            billeteraVendedor.Version++;

            await ledger.AgregarAsync(new AsientoLedger
            {
                BilleteraId = billeteraVendedor.Id,
                Tipo = TipoAsiento.Cobro,
                Monto = monto,
                SubastaId = subasta.Id,
                Descripcion = "Cobro por subasta vendida",
                Fecha = ahora
            });

            // 3. El cierre, en la misma escritura que el pago.
            subasta.Estado = EstadoSubasta.Finalizada;

            // EF no incrementa solo un token de concurrencia de tipo int. Este
            // incremento es lo que hace chocar a una oferta que llega en el mismo
            // instante: sin el, la puja se guardaria sobre una subasta ya liquidada.
            subasta.Version++;

            await contexto.SaveChangesAsync(cancelacion);

            // La auditoria confirma con su propio SaveChanges sobre este mismo contexto,
            // por eso va despues del guardado principal. Sigue adentro de la transaccion:
            // si el commit no llega, tampoco queda registrado un cierre que no ocurrio.
            // Sin usuario: la accion es del sistema, no de una persona.
            await auditoria.RegistrarAsync(
                EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.CierreWorker,
                null, new { compradorId, vendedorId, monto });

            await transaccion.CommitAsync(cancelacion);

            _logger.LogInformation(
                "Subasta {SubastaId} finalizada: {Monto} del usuario {CompradorId} al usuario {VendedorId}.",
                subasta.Id, monto, compradorId, vendedorId);

            await AvisarAsync(subasta.Id, "SubastaFinalizada", cancelacion);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Una oferta entro entre la lectura y el guardado. La transaccion se deshace
            // entera y la subasta queda intacta: el ciclo siguiente la lee de nuevo, ya
            // con su lider y su fecha actualizados.
            await transaccion.RollbackAsync(cancelacion);
            Descartar(contexto, subasta.Id, "cerrar");
        }
    }

    /// <summary>
    /// Vencida sin ofertas: pasa a Desierta y no se mueve un peso. La transaccion es por
    /// la auditoria, para que el estado y su registro entren o no entren juntos.
    /// </summary>
    private async Task CerrarDesiertaAsync(
        AppDbContext contexto, IServicioDeAuditoria auditoria,
        Subasta subasta, CancellationToken cancelacion)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            subasta.Estado = EstadoSubasta.Desierta;
            subasta.Version++;

            await contexto.SaveChangesAsync(cancelacion);

            await auditoria.RegistrarAsync(
                EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.CierreDesierta,
                null, new { precioBase = subasta.PrecioBase, fechaFin = subasta.FechaFin });

            await transaccion.CommitAsync(cancelacion);

            _logger.LogInformation("Subasta {SubastaId} declarada desierta.", subasta.Id);

            // Mismo evento que el cierre con ganador, aunque el estado sea otro: el
            // cliente no decide nada con el nombre, vuelve a pedir el detalle y redibuja
            // con lo que reciba. Un segundo evento no le aportaria nada.
            await AvisarAsync(subasta.Id, "SubastaFinalizada", cancelacion);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaccion.RollbackAsync(cancelacion);
            Descartar(contexto, subasta.Id, "declarar desierta");
        }
    }

    /// <summary>
    /// Avisa a quienes esten mirando la subasta que su estado cambio. Va siempre despues
    /// del commit: antes, un aviso podria anunciar un cierre que la transaccion termina
    /// deshaciendo. Y con su propio try/catch, porque la liquidacion ya ocurrio y no se
    /// puede deshacer por un problema de notificacion: el aviso se pierde, la pantalla se
    /// corrige sola en la proxima recarga y el ciclo sigue.
    /// </summary>
    private async Task AvisarAsync(int subastaId, string evento, CancellationToken cancelacion)
    {
        try
        {
            await _hub.Clients.Group(SubastaHub.NombreDeGrupo(subastaId))
                .SendAsync(evento, cancelacion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo avisar {Evento} de la subasta {SubastaId}.",
                evento, subastaId);
        }
    }

    /// <summary>
    /// Deja el contexto limpio despues de un choque de versiones. Sin esto, los cambios
    /// que fallaron siguen en el seguimiento y el proximo SaveChanges del ciclo —el de
    /// otra subasta o el de la auditoria— intentaria escribirlos de nuevo y volveria a
    /// chocar. La subasta descartada se vuelve a procesar en el ciclo siguiente.
    /// </summary>
    private void Descartar(AppDbContext contexto, int subastaId, string operacion)
    {
        contexto.ChangeTracker.Clear();
        _logger.LogWarning(
            "No se pudo {Operacion} la subasta {SubastaId}: cambio mientras se procesaba. Se reintenta en el proximo ciclo.",
            operacion, subastaId);
    }
}
