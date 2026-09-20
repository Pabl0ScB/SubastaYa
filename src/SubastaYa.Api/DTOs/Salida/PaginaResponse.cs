namespace SubastaYa.Api.DTOs.Salida;

public class PaginaResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PaginaActual { get; set; }
    public int Tamano { get; set; }
    public int TotalElementos { get; set; }
    public int TotalPaginas { get; set; }
}

/// <summary>
/// Arma la respuesta paginada. Existe para que el calculo de TotalPaginas viva en un solo
/// lugar y no se repita en cada servicio que devuelve un listado.
/// </summary>
public static class PaginaResponse
{
    public static PaginaResponse<T> Crear<T>(
        IReadOnlyList<T> items, int pagina, int tamano, int totalElementos) => new()
    {
        Items = items,
        PaginaActual = pagina,
        Tamano = tamano,
        TotalElementos = totalElementos,
        TotalPaginas = (int)Math.Ceiling(totalElementos / (double)tamano)
    };
}
