namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Una oferta tal como la ve cualquiera: identifica al postor por su seudonimo, nunca por
/// su email ni por su id.
/// </summary>
public class PujaResponse
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaPuja { get; set; }
    public string Seudonimo { get; set; } = null!;
}
