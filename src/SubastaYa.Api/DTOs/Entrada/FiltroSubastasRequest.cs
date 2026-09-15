using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

public class FiltroSubastasRequest
{
    public string? Estado { get; set; }
    public int? CategoriaId { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public string? Orden { get; set; }

    // Sin estos rangos, "pagina=0" produce un Skip(-10) que PostgreSQL rechaza con un
    // error no controlado, y "tamano=0" hace que el calculo de TotalPaginas divida por
    // cero. Al ser anotaciones del DTO, [ApiController] responde 400 antes de que el
    // controlador se ejecute.
    [Range(1, int.MaxValue, ErrorMessage = "La pagina debe ser mayor o igual a 1.")]
    public int Pagina { get; set; } = 1;

    // El tope evita ademas que una sola peticion se traiga la tabla entera.
    [Range(1, 100, ErrorMessage = "El tamano de pagina debe estar entre 1 y 100.")]
    public int Tamano { get; set; } = 10;
}
