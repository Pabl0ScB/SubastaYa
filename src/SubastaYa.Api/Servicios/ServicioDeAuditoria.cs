using System.Text.Encodings.Web;
using System.Text.Json;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Repositorios;

namespace SubastaYa.Api.Servicios;

public class ServicioDeAuditoria : IServicioDeAuditoria
{
    // El detalle se guarda legible, con sus tildes, para que el registro se pueda leer
    // directamente en la base. Por defecto el serializador las guardaria como codigos.
    private static readonly JsonSerializerOptions OpcionesDetalle =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

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
            DetalleJson = detalle is null ? "{}" : JsonSerializer.Serialize(detalle, OpcionesDetalle),

            // UTC: Npgsql rechaza un DateTime con Kind Local sobre una columna
            // "timestamp with time zone", y el error aparece recien al insertar.
            Fecha       = DateTime.UtcNow
        });
    }
}