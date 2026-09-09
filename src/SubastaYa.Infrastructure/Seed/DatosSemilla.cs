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
        var hogar = new Categoria { Nombre = "Hogar" };
        var vehiculos = new Categoria { Nombre = "Vehículos" };
        var coleccionables = new Categoria { Nombre = "Coleccionables" };

        contexto.Categorias.AddRange(tecnologia, hogar, vehiculos, coleccionables);
        contexto.SaveChanges(); // necesitamos los Id generados para las subastas

        // ---------- 2. Usuarios ----------
        var vendedor1 = new Usuario
        {
            Email = "vendedor1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Vendedor Uno",
            Seudonimo = "vendedor1",
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

        var comprador3 = new Usuario
        {
            Email = "comprador3@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordDePrueba),
            Nombre = "Comprador Tres",
            Seudonimo = "comprador3",
            FechaRegistro = ahora
        };

        contexto.Usuarios.AddRange(vendedor1, comprador1, comprador2, comprador3);
        contexto.SaveChanges(); // ya tenemos los Id de usuario

        // ---------- 3. Billeteras ----------
        // SaldoRetenido de comprador2 = 5.500 (no 0): ver nota en el chat sobre por qué
        // se ajustó para que la subasta "vencida con ganador" tenga su retención real.
        var billeteraVendedor1 = new Billetera
        {
            UsuarioId = vendedor1.Id,
            SaldoTotal = 0m,
            SaldoRetenido = 0m,
            Version = 1
        };

        var billeteraComprador1 = new Billetera
        {
            UsuarioId = comprador1.Id,
            SaldoTotal = 100000m,
            SaldoRetenido = 45000m,
            Version = 1
        };

        var billeteraComprador2 = new Billetera
        {
            UsuarioId = comprador2.Id,
            SaldoTotal = 100000m,
            SaldoRetenido = 5500m,
            Version = 1
        };

        var billeteraComprador3 = new Billetera
        {
            UsuarioId = comprador3.Id,
            SaldoTotal = 1000m,
            SaldoRetenido = 0m,
            Version = 1
        };

        contexto.Billeteras.AddRange(
            billeteraVendedor1, billeteraComprador1, billeteraComprador2, billeteraComprador3);
        contexto.SaveChanges();

        // ---------- 4. Subastas ----------

        // Caso A: Activa estándar -> catálogo, sala en vivo, puja normal
        var subastaActivaEstandar = new Subasta
        {
            VendedorId = vendedor1.Id,
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
        var subastaActivaCritica = new Subasta
        {
            VendedorId = vendedor1.Id,
            CategoriaId = hogar.Id,
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
            VendedorId = vendedor1.Id,
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
            VendedorId = vendedor1.Id,
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
            VendedorId = vendedor1.Id,
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
        contexto.AsientosLedger.AddRange(
            new AsientoLedger
            {
                BilleteraId = billeteraComprador1.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 100000m,
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
                Monto = 100000m,
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
                BilleteraId = billeteraComprador3.Id,
                Tipo = TipoAsiento.Deposito,
                Monto = 1000m,
                SubastaId = null,
                Descripcion = "Carga inicial de saldo (seed)",
                Fecha = ahora.AddDays(-1)
            }
        );

        contexto.SaveChanges();
    }
}