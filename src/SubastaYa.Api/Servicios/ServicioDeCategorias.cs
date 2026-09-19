using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeCategorias : IServicioDeCategorias
{
    private readonly AppDbContext _contexto;

    public ServicioDeCategorias(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IEnumerable<CategoriaResponse>> ObtenerTodasAsync()
    {
        return await _contexto.Categorias
            .AsNoTracking()
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nombre = c.Nombre,
                UrlIcono = c.UrlIcono
            })
            .ToListAsync();
    }
}