using SubastaYa.Api.DTOs.Salida;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeCategorias
{
    Task<IEnumerable<CategoriaResponse>> ObtenerTodasAsync();
}