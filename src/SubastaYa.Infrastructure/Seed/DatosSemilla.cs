using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Infrastructure.Seed;

/// <summary>
/// Carga datos de prueba al arrancar la aplicación.
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

        // Quinto usuario, no exigido por el enunciado: sostiene la puja ganadora de la
        // subasta "vencida con ganador" sin contradecir la descripción de comprador1
        // (que ya tiene su retenido comprometido en la subasta activa) ni la de
        // comprador2 (descripto con todo su saldo disponible).
        var comprador3 = new Usuario
        {
            Email = "comprador3@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Comprador Tres",
            Seudonimo = "comprador3",
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

        contexto.Usuarios.AddRange(vendedor, comprador1, comprador2, comprador3, sinFondos);
        contexto.SaveChanges(); // ya tenemos los Id de usuario

        // ---------- 3. Billeteras ----------
        // comprador1: Total $150.000 / Retenido $45.000 / Disponible $105.000.
        // comprador2: Total $200.000 / Retenido $0 / Disponible $200.000 — la puja que
        // hizo en la subasta activa estándar fue superada por comprador1, así que su
        // escrow ya se liberó.
        // comprador3: Total $50.000 / Retenido $5.500 — sostiene la puja ganadora de
        // la subasta "vencida con ganador".
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
            SaldoRetenido = 0m,
            Version = 1
        };

        var billeteraComprador3 = new Billetera
        {
            UsuarioId = comprador3.Id,
            SaldoTotal = 50000m,
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
            billeteraVendedor, billeteraComprador1, billeteraComprador2,
            billeteraComprador3, billeteraSinFondos);
        contexto.SaveChanges();

        // ---------- 4. Subastas ----------

        // Caso A: Activa estándar -> catálogo, sala en vivo, puja normal
        var subastaActivaEstandar = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = tecnologia.Id,
            Titulo = "Notebook Gamer usada",
            Descripcion = "Notebook gamer, poco uso, con cargador original.",
            UrlImagen = "img/notebook.jpg",
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
        var subastaActivaCritica = new Subasta
        {
            VendedorId = vendedor.Id,
            CategoriaId = indumentaria.Id,
            Titulo = "Campera de cuero",
            Descripcion = "Campera de cuero negra, talle M, poco uso.",
            UrlImagen = "img/campera.jpg",
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
            UrlImagen = "img/bicicleta.jpg",
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
            UrlImagen = "img/album.jpg",
            PrecioBase = 5000m,
            IncrementoMinimo = 200m,
            PujaActual = 5500m,
            LiderId = comprador3.Id,
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
            UrlImagen = "img/router.jpg",
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
            CompradorId = comprador3.Id,
            Monto = 5500m,
            FechaPuja = ahora.AddHours(-1)
        };

        contexto.Pujas.AddRange(pujaPreviaComprador2, pujaLiderComprador1, pujaGanadoraVencida);

        // ---------- 6. Asientos del ledger ----------
        // Deposito = cómo entró la plata a la billetera (cuenta para el SaldoTotal).
        // Retencion = por qué una parte está bloqueada ahora mismo (NO afecta el
        // SaldoTotal, solo explica el SaldoRetenido).
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
                BilleteraId = billeteraComprador3.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 50000m,
                SubastaId = null,
                Descripcion = "Carga inicial de saldo (seed)",
                Fecha = ahora.AddDays(-1)
            },
            new AsientoLedger
            {
                BilleteraId = billeteraComprador3.Id,
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