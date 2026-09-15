using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _contexto;

    public CategoriasController(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> ObtenerCategorias()
    {
        var categorias = await _contexto.Categorias
            .AsNoTracking()
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nombre = c.Nombre,
                UrlIcono = c.UrlIcono
            })
            .ToListAsync();

        return Ok(categorias);
    }
}