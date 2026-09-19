using Microsoft.AspNetCore.Mvc;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriasController : ControllerBase
{
    private readonly IServicioDeCategorias _servicio;

    public CategoriasController(IServicioDeCategorias servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> ObtenerCategorias()
        => Ok(await _servicio.ObtenerTodasAsync());
}