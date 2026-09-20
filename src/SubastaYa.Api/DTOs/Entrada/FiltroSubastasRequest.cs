using System.ComponentModel.DataAnnotations;
using SubastaYa.Domain.Enums;

namespace SubastaYa.Api.DTOs.Entrada;

public class FiltroSubastasRequest : PaginacionRequest
{
    // Tipado como enum y no como texto: asi el model binding rechaza un valor
    // invalido con un 400 automatico, en vez de ignorar el filtro en silencio y
    // devolver todas las subastas.
    public EstadoSubasta? Estado { get; set; }
    public int? CategoriaId { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public string? Orden { get; set; }
    [MaxLength(100)]
    public string? Busqueda { get; set; }
}
