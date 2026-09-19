using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

public class PublicarSubastaRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El título no puede superar los 120 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Descripcion { get; set; } = string.Empty;

    [Required, Url(ErrorMessage = "La imagen debe ser una URL válida.")]
    public string UrlImagen { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "El precio base debe ser mayor a cero.")]
    public decimal PrecioBase { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "El incremento mínimo debe ser mayor a cero.")]
    public decimal IncrementoMinimo { get; set; }

    [Required] public DateTime FechaInicio { get; set; }
    [Required] public DateTime FechaFin { get; set; }
}