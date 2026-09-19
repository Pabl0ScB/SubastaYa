using System.Text.Json;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Repositorios;

namespace SubastaYa.Api.Servicios;

public class ServicioDeAuditoria : IServicioDeAuditoria
{
    private readonly IRegistroAuditoriaRepository _repositorio;

    public ServicioDeAuditoria(IRegistroAuditoriaRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task RegistrarAsync(string entidad, int entidadId, string accion,
                                     int? usuarioId, object? detalle = null)
    {
        await _repositorio.AgregarAsync(new RegistroAuditoria
        {
            Entidad     = entidad,
            EntidadId   = entidadId,
            Accion      = accion,
            UsuarioId   = usuarioId,

            // DetalleJson no es anulable en la entidad: sin detalle se guarda un objeto
            // JSON vacio, no null, para que la columna nunca quede sin valor.
            DetalleJson = detalle is null ? "{}" : JsonSerializer.Serialize(detalle),

            // UTC: Npgsql rechaza un DateTime con Kind Local sobre una columna
            // "timestamp with time zone", y el error aparece recien al insertar.
            Fecha       = DateTime.UtcNow
        });
    }
}