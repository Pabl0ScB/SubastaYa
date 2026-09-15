using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Api.Servicios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Controllers;

[ApiController]
[Route("api/v1/auctions")]
public class SubastasController : ControllerBase
{
    private readonly IServicioDeSubastas _servicio;

    public SubastasController(AppDbContext contexto)
    {
        _servicio = new ServicioDeSubastas(contexto);
    }

    [HttpGet]
    public async Task<ActionResult<PaginaResponse<SubastaTarjetaResponse>>> ObtenerCatalogo(
        [FromQuery] FiltroSubastasRequest filtro)
    {
        var consulta = _servicio.ConstruirConsultaCatalogo(filtro);

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

        var respuesta = new PaginaResponse<SubastaTarjetaResponse>
        {
            Items = items,
            PaginaActual = filtro.Pagina,
            Tamano = filtro.Tamano,
            TotalElementos = totalElementos,
            TotalPaginas = (int)Math.Ceiling(totalElementos / (double)filtro.Tamano)
        };

        return Ok(respuesta);
    }
}