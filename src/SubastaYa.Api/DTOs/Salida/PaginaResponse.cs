namespace SubastaYa.Api.DTOs.Salida;

public class PaginaResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PaginaActual { get; set; }
    public int Tamano { get; set; }
    public int TotalElementos { get; set; }
    public int TotalPaginas { get; set; }
}