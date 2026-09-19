using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Domain.Entidades;
using SubastaYa.Infrastructure.Persistencia;
using SubastaYa.Domain.Excepciones;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeSubastas
{
    IQueryable<Subasta> ConstruirConsultaCatalogo(FiltroSubastasRequest filtro);
    Task<Subasta> PublicarAsync(PublicarSubastaRequest request, int vendedorId);
}

public class ServicioDeSubastas : IServicioDeSubastas
{
    private readonly AppDbContext _contexto;

    public ServicioDeSubastas(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public IQueryable<Subasta> ConstruirConsultaCatalogo(FiltroSubastasRequest filtro)
    {
        // Sin Include: la consulta termina en una proyeccion a DTO en el controlador, y
        // ante una proyeccion EF Core ignora los Include. El JOIN con Categoria lo genera
        // el propio Select, que es lo que evita el problema N+1.
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

        query = filtro.Orden switch
        {
            "puja" => query.OrderByDescending(s => s.PujaActual),
            "reciente" => query.OrderByDescending(s => s.FechaCreacion),
            _ => query.OrderBy(s => s.FechaFin)
        };

        return query;
    }
    public async Task<Subasta> PublicarAsync(PublicarSubastaRequest request, int vendedorId)
    {
        // 1. La categoria existe (necesita ir a la base, no es expresable como anotacion).
        var categoriaExiste = await _contexto.Categorias
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoriaId);

        if (!categoriaExiste)
        {
            throw new RecursoNoEncontradoException(
                $"No existe una categoría con id {request.CategoriaId}.");
        }

        // 2. FechaFin tiene que ser posterior a FechaInicio.
        if (request.FechaFin <= request.FechaInicio)
        {
            throw new ReglaDeNegocioException(
                "La fecha de fin debe ser posterior a la de inicio.");
        }

        // 3. No se puede publicar algo que ya vence.
        if (request.FechaFin <= DateTime.UtcNow)
        {
            throw new ReglaDeNegocioException(
                "La fecha de fin debe ser posterior al momento actual.");
        }

        // 4. El incremento minimo no puede superar el precio base, o la subasta seria
        // imposible de pujar desde la primera oferta.
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
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Estado = request.FechaInicio <= DateTime.UtcNow
                ? Domain.Enums.EstadoSubasta.Activa
                : Domain.Enums.EstadoSubasta.Programada,
            Version = 1,
            FechaCreacion = DateTime.UtcNow
        };

        _contexto.Subastas.Add(subasta);
        await _contexto.SaveChangesAsync();

        return subasta;
    }
}