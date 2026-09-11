using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Infrastructure.Seed;

/// <summary>
/// Carga datos de prueba al arrancar la aplicación (Tarea 1.11).
/// No usa HasData() porque las fechas de las subastas tienen que calcularse
/// relativas a DateTime.UtcNow en cada arranque, no quedar congeladas en la migración.
/// Es idempotente: si ya hay usuarios, no hace nada.
///
/// Ajustado según la sección 3.3 "Datos Semilla Obligatorios (Seed Data)" del enunciado.
/// </summary>
public static class DatosSemilla
{
    private const string PasswordDePrueba = "Test1234";

    public static void AplicarDatosSemilla(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Idempotencia: si ya hay usuarios cargados, no se toca nada.
        if (contexto.Usuarios.Any())
        {
            return;
        }

        var ahora = DateTime.UtcNow;

        // ---------- 1. Categorías ----------
        var tecnologia = new Categoria { Nombre = "Tecnología" };
        var indumentaria = new Categoria { Nombre = "Indumentaria" };
        var vehiculos = new Categoria { Nombre = "Vehículos" };
        var coleccionables = new Categoria { Nombre = "Coleccionables" };

        contexto.Categorias.AddRange(tecnologia, indumentaria, vehiculos, coleccionables);
        contexto.SaveChanges(); // necesitamos los Id generados para las subastas

        // ---------- 2. Usuarios ----------
        var vendedor = new Usuario
        {
            Email = "vendedor@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Vendedor",
            Seudonimo = "vendedor",
            FechaRegistro = ahora
        };

        var comprador1 = new Usuario
        {
            Email = "comprador1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Comprador Uno",
            Seudonimo = "comprador1",
            FechaRegistro = ahora
        };

        var comprador2 = new Usuario
        {
            Email = "comprador2@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Comprador Dos",
            Seudonimo = "comprador2",
            FechaRegistro = ahora
        };

        var sinFondos = new Usuario
        {
            Email = "sinfondos@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Sin Fondos",
            Seudonimo = "sinfondos",
            FechaRegistro = ahora
        };

        contexto.Usuarios.AddRange(vendedor, comprador1, comprador2, sinFondos);
        contexto.SaveChanges(); // ya tenemos los Id de usuario

        // ---------- 3. Billeteras ----------
        // comprador1: Total $150.000 / Retenido $45.000 / Disponible $105.000 (según enunciado).
        //
        // comprador2: Total $200.000, SaldoRetenido = 5.500 (no 0).
        // TODO(pendiente profesor): el enunciado lo describe como "Postor habilitado" con
        // Disponible = $200.000 (o sea, sin nada retenido), pero acá gana la subasta
        // "Vencida con ganador" y necesita su puja retenida hasta que el worker la liquide
        // (Bloque 8). Es una contradicción del propio enunciado -> ya se le consultó al
        // profesor. Mientras no responda, se deja comprador2 con esta retención y ganando
        // esa subasta. NO agregar un quinto usuario para esto todavía: eso se evalúa recién
        // en el Bloque 4 (billetera/ledger), en un commit aparte.
        var billeteraVendedor = new Billetera
        {
            UsuarioId = vendedor.Id,
            SaldoTotal = 0m,
            SaldoRetenido = 0m,
            Version = 1
        };

        var billeteraComprador1 = new Billetera
        {
            UsuarioId = comprador1.Id,
            SaldoTotal = 150000m,
            SaldoRetenido = 45000m,
            Version = 1
        };

        var billeteraComprador2 = new Billetera
        {
            UsuarioId = comprador2.Id,
            SaldoTotal = 200000m,
            SaldoRetenido = 5500m,
            Version = 1
        };

        var billeteraSinFondos = new Billetera
        {
            UsuarioId = sinFondos.Id,
            SaldoTotal = 500m,
            SaldoRetenido = 0m,
            Version = 1
        };

        contexto.Billeteras.AddRange(
            billeteraVendedor, billeteraComprador1, billeteraComprador2, billeteraSinFondos);
        contexto.SaveChanges();

        // ---------- 4. Subastas ----------

        // Caso A: Activa estándar -> catálogo, sala en vivo, puja normal
        var subastaActivaEstandar = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = tecnologia.Id,
            Titulo = "Notebook Gamer usada",
            Descripcion = "Notebook gamer, poco uso, con cargador original.",
            UrlImagen = "https://picsum.photos/seed/notebook/600/400",
            PrecioBase = 40000m,
            IncrementoMinimo = 1000m,
            PujaActual = 45000m,
            LiderId = comprador1.Id,
            FechaInicio = ahora.AddHours(-1),
            FechaFin = ahora.AddMinutes(25),
            Estado = EstadoSubasta.Activa,
            Version = 1,
            FechaCreacion = ahora.AddHours(-1)
        };

        // Caso B: Activa crítica -> alerta visual y anti-sniping
        // NOTA: sigue en la categoría "Indumentaria" (ex "Hogar") por el rename de categorías;
        // el título/descripción del ítem (sillones de living) ya no encaja semánticamente con
        // esa categoría. El enunciado solo pide el rename de la categoría, no reasignar qué
        // ítem va en cuál — si querés que el contenido tenga sentido, cambiá este ítem por
        // algo de indumentaria (ej. "Campera de cuero") o movelo a otra categoría.
        var subastaActivaCritica = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = indumentaria.Id,
            Titulo = "Set de sillones de living",
            Descripcion = "Juego de living de 3 cuerpos, buen estado.",
            UrlImagen = "https://picsum.photos/seed/sillones/600/400",
            PrecioBase = 20000m,
            IncrementoMinimo = 500m,
            PujaActual = 20000m,
            LiderId = null,
            FechaInicio = ahora.AddMinutes(-10),
            FechaFin = ahora.AddSeconds(90),
            Estado = EstadoSubasta.Activa,
            Version = 1,
            FechaCreacion = ahora.AddMinutes(-10)
        };

        // Caso C: Programada -> bloqueo de pujas antes del inicio
        var subastaProgramada = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = vehiculos.Id,
            Titulo = "Bicicleta rodado 29",
            Descripcion = "Bicicleta de montaña, rodado 29, poco uso.",
            UrlImagen = "https://picsum.photos/seed/bici/600/400",
            PrecioBase = 60000m,
            IncrementoMinimo = 2000m,
            PujaActual = 60000m,
            LiderId = null,
            FechaInicio = ahora.AddHours(24),
            FechaFin = ahora.AddHours(25),
            Estado = EstadoSubasta.Programada,
            Version = 1,
            FechaCreacion = ahora
        };

        // Caso D: Vencida con ganador -> cierre y liquidación del worker
        var subastaVencidaConGanador = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = coleccionables.Id,
            Titulo = "Álbum de figuritas completo",
            Descripcion = "Álbum antiguo completo, edición especial.",
            UrlImagen = "https://picsum.photos/seed/album/600/400",
            PrecioBase = 5000m,
            IncrementoMinimo = 200m,
            PujaActual = 5500m,
            LiderId = comprador2.Id,
            FechaInicio = ahora.AddHours(-2),
            FechaFin = ahora.AddMinutes(-5),
            Estado = EstadoSubasta.Activa, // el worker todavía no la cerró
            Version = 1,
            FechaCreacion = ahora.AddHours(-2)
        };

        // Caso E: Vencida desierta -> transición a Desierta
        var subastaVencidaDesierta = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = tecnologia.Id,
            Titulo = "Router viejo sin uso",
            Descripcion = "Router en caja, nunca usado.",
            UrlImagen = "https://picsum.photos/seed/router/600/400",
            PrecioBase = 3000m,
            IncrementoMinimo = 100m,
            PujaActual = 3000m,
            LiderId = null,
            FechaInicio = ahora.AddHours(-3),
            FechaFin = ahora.AddMinutes(-30),
            Estado = EstadoSubasta.Activa, // el worker todavía no la cerró
            Version = 1,
            FechaCreacion = ahora.AddHours(-3)
        };

        contexto.Subastas.AddRange(
            subastaActivaEstandar, subastaActivaCritica, subastaProgramada,
            subastaVencidaConGanador, subastaVencidaDesierta);
        contexto.SaveChanges(); // necesitamos los Id de subasta para las pujas

        // ---------- 5. Pujas ----------
        // Historial de las 2 ofertas previas en la subasta activa estándar (enunciado 3.3):
        // comprador2 pujó primero, comprador1 la superó y quedó como líder actual.
        var pujaPreviaComprador2 = new Puja
        {
            SubastaId = subastaActivaEstandar.Id,
            CompradorId = comprador2.Id,
            Monto = 42000m,
            FechaPuja = ahora.AddMinutes(-20)
        };

        var pujaLiderComprador1 = new Puja
        {
            SubastaId = subastaActivaEstandar.Id,
            CompradorId = comprador1.Id,
            Monto = 45000m,
            FechaPuja = ahora.AddMinutes(-10)
        };

        var pujaGanadoraVencida = new Puja
        {
            SubastaId = subastaVencidaConGanador.Id,
            CompradorId = comprador2.Id,
            Monto = 5500m,
            FechaPuja = ahora.AddHours(-1)
        };

        contexto.Pujas.AddRange(pujaPreviaComprador2, pujaLiderComprador1, pujaGanadoraVencida);

        // ---------- 6. Asientos del ledger ----------
        // Deposito = cómo entró la plata a la billetera (cuenta para el SaldoTotal).
        // Retencion = por qué una parte está bloqueada ahora mismo (NO afecta el
        // SaldoTotal, solo explica el SaldoRetenido). Ver nota en el chat.
        // Los montos de Deposito de cada usuario se corrigieron para que coincidan
        // 1:1 con el SaldoTotal de su billetera (sección 3.3 del enunciado).
        contexto.AsientosLedger.AddRange(
            new AsientoLedger
            {
                BilleteraId = billeteraComprador1.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 150000m,
                SubastaId = null,
                Descripcion = "Carga inicial de saldo (seed)",
                Fecha = ahora.AddDays(-1)
            },
            new AsientoLedger
            {
                BilleteraId = billeteraComprador1.Id,
                Tipo = TipoAsiento.Retencion,
                Monto = 45000m,
                SubastaId = subastaActivaEstandar.Id,
                Descripcion = "Retención por ser líder de la subasta",
                Fecha = ahora.AddMinutes(-10)
            },
            new AsientoLedger
            {
                BilleteraId = billeteraComprador2.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 200000m,
                SubastaId = null,
                Descripcion = "Carga inicial de saldo (seed)",
                Fecha = ahora.AddDays(-1)
            },
            new AsientoLedger
            {
                BilleteraId = billeteraComprador2.Id,
                Tipo = TipoAsiento.Retencion,
                Monto = 5500m,
                SubastaId = subastaVencidaConGanador.Id,
                Descripcion = "Retención por ser líder de la subasta",
                Fecha = ahora.AddHours(-1)
            },
            new AsientoLedger
            {
                BilleteraId = billeteraSinFondos.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 500m,
                SubastaId = null,
                Descripcion = "Carga inicial de saldo (seed)",
                Fecha = ahora.AddDays(-1)
            }
        );

        contexto.SaveChanges();
    }
}