namespace SubastaYa.Api.DTOs.Salida;

public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? UrlIcono { get; set; }
}