using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubastaYa.Api.Configuracion;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Hubs;
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
    private readonly OpcionesAntiSniping _antiSniping;
    private readonly IHubContext<SubastaHub> _hub;
    private readonly ILogger<ServicioDePujas> _logger;

    public ServicioDePujas(
        AppDbContext contexto,
        IAsientoLedgerRepository ledger,
        IServicioDeAuditoria auditoria,
        IOptions<OpcionesAntiSniping> antiSniping,
        IHubContext<SubastaHub> hub,
        ILogger<ServicioDePujas> logger)
    {
        _contexto = contexto;
        _ledger = ledger;
        _auditoria = auditoria;
        _antiSniping = antiSniping.Value;
        _hub = hub;
        _logger = logger;
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
            throw new ReglaDeNegocioException($"La oferta debe ser de al menos {FormatearPesos(montoMinimo)}.");
        }

        var billetera = await _contexto.Billeteras
            .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId)
            ?? throw new RecursoNoEncontradoException("El usuario no tiene una billetera asociada.");

        if (billetera.SaldoDisponible < monto)
        {
            await _auditoria.RegistrarAsync(
                EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.PujaRechazadaSaldo,
                usuarioId, new { monto, disponible = billetera.SaldoDisponible });
            throw new ReglaDeNegocioException(
                $"Saldo insuficiente. Disponible: {FormatearPesos(billetera.SaldoDisponible)}.");
        }

        return (subasta, billetera);
    }

    // Montos para los mensajes que lee el usuario: "$ 46.000,50". Cultura fija y no la del
    // servidor, para que se lean igual en cualquier maquina donde corra la API; y con
    // centavos, porque un minimo redondeado haria rechazar una oferta que parece valida.
    private static string FormatearPesos(decimal monto)
        => monto.ToString("C", CultureInfo.GetCultureInfo("es-AR"));

    private Task Auditar(int subastaId, int usuarioId, decimal monto, string motivo)
        => _auditoria.RegistrarAsync(
            EntidadesAuditables.Subasta, subastaId, AccionesAuditoria.PujaRechazadaValidacion,
            usuarioId, new { monto, motivo });

    // La oferta ya paso todas las validaciones. Todo lo que mueve dinero ocurre en una
    // sola transaccion: si cualquier paso falla no queda saldo retenido sin su puja, ni
    // una puja sin su asiento, ni dos postores con la misma subasta retenida.
    private async Task<PujaRegistradaResponse> EjecutarTransaccionAsync(
        Subasta subasta, Billetera billetera, int usuarioId, decimal monto)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var ahora = DateTime.UtcNow;
        var fechaFinAnterior = subasta.FechaFin;
        Puja puja;
        bool tiempoExtendido;

        try
        {
            // 1. Liberar la retencion del lider anterior. Su dinero vuelve a estar
            //    disponible en el mismo instante en que se retiene el del nuevo postor.
            if (subasta.LiderId is int liderAnteriorId)
            {
                var billeteraAnterior = await _contexto.Billeteras
                    .FirstAsync(b => b.UsuarioId == liderAnteriorId);

                billeteraAnterior.SaldoRetenido -= subasta.PujaActual;
                billeteraAnterior.Version++;

                await _ledger.AgregarAsync(new AsientoLedger
                {
                    BilleteraId = billeteraAnterior.Id,
                    Tipo = TipoAsiento.Liberacion,
                    Monto = subasta.PujaActual,
                    SubastaId = subasta.Id,
                    Descripcion = "Liberación por oferta superada",
                    Fecha = ahora
                });
            }

            // 2. Retener el monto del nuevo postor. El saldo total no se toca: el dinero
            //    sigue siendo suyo hasta que la subasta se liquide.
            billetera.SaldoRetenido += monto;
            billetera.Version++;

            await _ledger.AgregarAsync(new AsientoLedger
            {
                BilleteraId = billetera.Id,
                Tipo = TipoAsiento.Retencion,
                Monto = monto,
                SubastaId = subasta.Id,
                Descripcion = "Retención por ser líder de la subasta",
                Fecha = ahora
            });

            // 3. La puja.
            puja = new Puja
            {
                SubastaId = subasta.Id,
                CompradorId = usuarioId,
                Monto = monto,
                FechaPuja = ahora
            };
            _contexto.Pujas.Add(puja);

            // 4. La subasta.
            subasta.PujaActual = monto;
            subasta.LiderId = usuarioId;

            // 5. Anti-sniping, en esta misma transaccion: si fuera en un guardado aparte,
            //    dos ofertas simultaneas podrian extender el cierre dos veces.
            tiempoExtendido =
                (subasta.FechaFin - ahora).TotalSeconds <= _antiSniping.UmbralSegundos;
            if (tiempoExtendido)
            {
                subasta.FechaFin = subasta.FechaFin.AddMinutes(_antiSniping.ExtensionMinutos);
            }

            // EF no incrementa solo un token de concurrencia de tipo int. Sin estos
            // incrementos el UPDATE ... WHERE Version = @leida siempre coincide y dos
            // ofertas simultaneas se pisarian sin que nadie lo detecte. Las dos billeteras
            // ya lo incrementaron mas arriba.
            subasta.Version++;

            await _contexto.SaveChangesAsync();

            // La auditoria confirma con su propio SaveChanges sobre este mismo contexto.
            // Por eso va despues del guardado principal: antes, ese SaveChanges escribiria
            // tambien la puja y los saldos. Sigue dentro de la transaccion, asi que si el
            // commit no llega, tampoco queda registrada una extension que no ocurrio.
            if (tiempoExtendido)
            {
                await _auditoria.RegistrarAsync(
                    EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.ExtensionTiempo,
                    usuarioId, new { fechaAnterior = fechaFinAnterior, fechaNueva = subasta.FechaFin });
            }

            await transaccion.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra operacion modifico la subasta o alguna de las billeteras entre la
            // lectura y este guardado: su Version ya no es la que se leyo.
            await transaccion.RollbackAsync();

            // El contexto todavia tiene la puja y los saldos que fallaron. Sin limpiarlo,
            // el SaveChanges de la auditoria intentaria guardarlos otra vez, chocaria de
            // nuevo contra la Version y el cliente recibiria un 500 en lugar del 409.
            _contexto.ChangeTracker.Clear();

            var actual = await _contexto.Subastas
                .AsNoTracking()
                .Where(s => s.Id == subasta.Id)
                .Select(s => new { s.PujaActual, s.IncrementoMinimo, s.FechaFin })
                .FirstAsync();

            // Fuera de la transaccion que se deshizo: el enunciado pide auditar los
            // rechazos por concurrencia, y adentro el rollback la habria borrado.
            await _auditoria.RegistrarAsync(
                EntidadesAuditables.Subasta, subasta.Id, AccionesAuditoria.PujaRechazadaConcurrencia,
                usuarioId, new { montoOfrecido = monto, pujaActual = actual.PujaActual });

            var montoMinimo = actual.PujaActual + actual.IncrementoMinimo;

            throw new ConflictoDeConcurrenciaException(
                "Otra oferta se registró antes que la tuya.",
                new ConflictoPujaResponse
                {
                    Mensaje = $"Otra oferta se registró antes que la tuya. La oferta mínima ahora es de {FormatearPesos(montoMinimo)}.",
                    PujaActual = actual.PujaActual,
                    MontoMinimo = montoMinimo,
                    FechaFin = actual.FechaFin
                });
        }

        var seudonimo = await _contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Seudonimo)
            .FirstAsync();

        var respuesta = new PujaRegistradaResponse
        {
            Id = puja.Id,
            Monto = puja.Monto,
            FechaPuja = puja.FechaPuja,
            Seudonimo = seudonimo,
            FechaFin = subasta.FechaFin,
            MontoMinimo = subasta.PujaActual + subasta.IncrementoMinimo,
            TiempoExtendido = tiempoExtendido
        };

        // Despues del commit y fuera del try: si se avisara antes y la transaccion se
        // deshiciera, todos verian una oferta que no existe. Y un fallo al avisar no puede
        // convertir en error una oferta que ya quedo guardada: se registra y se sigue.
        try
        {
            await _hub.Clients.Group(SubastaHub.NombreDeGrupo(subasta.Id))
                .SendAsync("NuevaPuja", respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo avisar la oferta {PujaId} a la sala en vivo.", respuesta.Id);
        }

        return respuesta;
    }
}
