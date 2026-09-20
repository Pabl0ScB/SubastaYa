using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

/// <summary>
/// Parametros de paginacion comunes a todos los listados. Los que ademas filtran heredan
/// de esta clase, asi los limites se definen una sola vez.
/// </summary>
public class PaginacionRequest
{
    // Sin estos rangos, "pagina=0" produce un Skip negativo que PostgreSQL rechaza con un
    // error no controlado, y "tamano=0" hace que el calculo de TotalPaginas divida por
    // cero. Al ser anotaciones del DTO, [ApiController] responde 400 antes de que el
    // controlador se ejecute.
    // La pagina tambien tiene tope: el salto se calcula como (pagina - 1) * tamano en un
    // int, y con una pagina enorme esa cuenta desborda y vuelve a dar negativo. Con este
    // tope y el de tamano, el salto maximo es de unos diez millones de filas.
    [Range(1, 100000, ErrorMessage = "La página debe estar entre 1 y 100000.")]
    public int Pagina { get; set; } = 1;

    // El tope evita ademas que una sola peticion se traiga la tabla entera.
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int Tamano { get; set; } = 10;
}
