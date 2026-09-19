using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Entidades;

namespace SubastaYa.Api.Servicios;

public interface IServicioDeSubastas
{
    Task<PaginaResponse<SubastaTarjetaResponse>> ObtenerCatalogoAsync(FiltroSubastasRequest filtro);
    Task<Subasta> PublicarAsync(PublicarSubastaRequest request, int vendedorId);
    Task<SubastaDetalleResponse> ObtenerDetalleAsync(int id);
}