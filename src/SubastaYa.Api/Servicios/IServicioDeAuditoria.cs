namespace SubastaYa.Api.Servicios;

public interface IServicioDeAuditoria
{
    Task RegistrarAsync(string entidad, int entidadId, string accion,
                        int? usuarioId, object? detalle = null);
}