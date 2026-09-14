using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeSubastas
{
    IQueryable<Subasta> ConstruirConsultaCatalogo(FiltroSubastasRequest filtro);
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
        var query = _contexto.Subastas
            .AsNoTracking()
            .Include(s => s.Categoria)
            .Include(s => s.Vendedor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Estado) &&
            Enum.TryParse<EstadoSubasta>(filtro.Estado, ignoreCase: true, out var estado))
        {
            query = query.Where(s => s.Estado == estado);
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
}