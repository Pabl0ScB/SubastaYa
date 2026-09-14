namespace SubastaYa.Api.DTOs.Entrada;

public class FiltroSubastasRequest
{
    public string? Estado { get; set; }
    public int? CategoriaId { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public string? Orden { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 10;
}