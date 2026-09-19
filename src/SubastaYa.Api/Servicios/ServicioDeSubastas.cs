using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeSubastas : IServicioDeSubastas
{
    private readonly AppDbContext _contexto;

    public ServicioDeSubastas(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<PaginaResponse<SubastaTarjetaResponse>> ObtenerCatalogoAsync(
        FiltroSubastasRequest filtro)
    {
        var consulta = ConstruirConsulta(filtro);

        var totalElementos = await consulta.CountAsync();

        var items = await consulta
            .Skip((filtro.Pagina - 1) * filtro.Tamano)
            .Take(filtro.Tamano)
            .Select(s => new SubastaTarjetaResponse
            {
                Id = s.Id,
                Titulo = s.Titulo,
                UrlImagen = s.UrlImagen,
                NombreCategoria = s.Categoria.Nombre,
                PujaActual = s.PujaActual,
                CantidadOfertas = s.Pujas.Count,
                FechaFin = s.FechaFin,
                Estado = s.Estado.ToString()
            })
            .ToListAsync();

        return new PaginaResponse<SubastaTarjetaResponse>
        {
            Items = items,
            PaginaActual = filtro.Pagina,
            Tamano = filtro.Tamano,
            TotalElementos = totalElementos,
            TotalPaginas = (int)Math.Ceiling(totalElementos / (double)filtro.Tamano)
        };
    }

    // Sin Include: la consulta termina en una proyeccion a DTO mas arriba, y ante una
    // proyeccion EF Core ignora el Include. El JOIN con Categoria lo genera el propio
    // Select, que es lo que evita el problema N+1.
    private IQueryable<Subasta> ConstruirConsulta(FiltroSubastasRequest filtro)
    {
        var query = _contexto.Subastas
            .AsNoTracking()
            .AsQueryable();

        if (filtro.Estado.HasValue)
        {
            query = query.Where(s => s.Estado == filtro.Estado.Value);
        }

        if (filtro.CategoriaId.HasValue)
        {
            query = query.Where(s => s.CategoriaId == filtro.CategoriaId.Value);
        }

        if (filtro.PrecioMin.HasValue)
        {
            query = query.Where(s => s.PujaActual >= filtro.PrecioMin.Value);
        }

        if (filtro.PrecioMax.HasValue)
        {
            query = query.Where(s => s.PujaActual <= filtro.PrecioMax.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            query = query.Where(s => EF.Functions.ILike(s.Titulo, $"%{filtro.Busqueda.Trim()}%"));
        }
        var ahora = DateTime.UtcNow;

        query = filtro.Orden switch
        {
            "puja" => query.OrderByDescending(s => s.PujaActual),
            "reciente" => query.OrderByDescending(s => s.FechaCreacion),
            // Primero las vigentes, de la que cierra antes a la que cierra despues; las
            // vencidas van al final, porque no les queda tiempo restante.
            _ => query.OrderBy(s => s.FechaFin < ahora).ThenBy(s => s.FechaFin)
        };

        return query;
    }

    public async Task<SubastaDetalleResponse> PublicarAsync(PublicarSubastaRequest request, int vendedorId)
    {
        var categoriaExiste = await _contexto.Categorias
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoriaId);

        if (!categoriaExiste)
        {
            throw new RecursoNoEncontradoException(
                $"No existe una categoría con id {request.CategoriaId}.");
        }

        // Se normaliza a UTC antes de cualquier comparacion: comparar un DateTime sin
        // Kind definido contra DateTime.UtcNow da un resultado que depende de la zona
        // horaria del servidor, no del valor real que mando el cliente.
        var fechaInicio = AUtc(request.FechaInicio);
        var fechaFin = AUtc(request.FechaFin);

        if (fechaFin <= fechaInicio)
        {
            throw new ReglaDeNegocioException(
                "La fecha de fin debe ser posterior a la de inicio.");
        }

        if (fechaFin <= DateTime.UtcNow)
        {
            throw new ReglaDeNegocioException(
                "La fecha de fin debe ser posterior al momento actual.");
        }

        if (request.IncrementoMinimo > request.PrecioBase)
        {
            throw new ReglaDeNegocioException(
                "El incremento mínimo no puede superar al precio base.");
        }

        var subasta = new Subasta
        {
            VendedorId = vendedorId,
            CategoriaId = request.CategoriaId,
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            UrlImagen = request.UrlImagen,
            PrecioBase = request.PrecioBase,
            IncrementoMinimo = request.IncrementoMinimo,
            PujaActual = request.PrecioBase,
            LiderId = null,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Estado = fechaInicio <= DateTime.UtcNow
                ? Domain.Enums.EstadoSubasta.Activa
                : Domain.Enums.EstadoSubasta.Programada,
            Version = 0,
            FechaCreacion = DateTime.UtcNow
       };

       _contexto.Subastas.Add(subasta);
       await _contexto.SaveChangesAsync();

       return await ObtenerDetalleAsync(subasta.Id);
    }

    public async Task<SubastaDetalleResponse> ObtenerDetalleAsync(int id)
    {
        return await _contexto.Subastas
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubastaDetalleResponse
            {
                Id = s.Id,
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                UrlImagen = s.UrlImagen,
                NombreCategoria = s.Categoria.Nombre,
                PrecioBase = s.PrecioBase,
                IncrementoMinimo = s.IncrementoMinimo,
                PujaActual = s.PujaActual,
                CantidadOfertas = s.Pujas.Count,
                FechaFin = s.FechaFin,
                Estado = s.Estado.ToString(),
                SeudonimoVendedor = "Vendedor #" + s.VendedorId,
                SeudonimoLider = s.LiderId == null ? null : "Postor #" + s.LiderId
            })
            .FirstOrDefaultAsync()
            ?? throw new RecursoNoEncontradoException("La subasta no existe.");
    }
    // El [Required] del DTO ya garantiza que value no sea null en este punto; el ! es
    // seguro. SpecifyKind marca el valor como UTC sin correr el reloj: la fecha que
    // llega en el JSON ya representa UTC, solo le falta la etiqueta para que Npgsql
    // la acepte en una columna "timestamp with time zone".
    private static DateTime AUtc(DateTime? value)
        => DateTime.SpecifyKind(value!.Value, DateTimeKind.Utc);
}