using SubastaYa.Api.DTOs.Entrada;
using SubastaYa.Api.DTOs.Salida;


namespace SubastaYa.Api.Servicios;

public interface IServicioDeSubastas
{
    Task<PaginaResponse<SubastaTarjetaResponse>> ObtenerCatalogoAsync(FiltroSubastasRequest filtro);
    Task<SubastaDetalleResponse> PublicarAsync(PublicarSubastaRequest request, int vendedorId);
    Task<SubastaDetalleResponse> ObtenerDetalleAsync(int id);
    Task<IReadOnlyList<MiPublicacionResponse>> ObtenerMisPublicacionesAsync(int vendedorId);
}