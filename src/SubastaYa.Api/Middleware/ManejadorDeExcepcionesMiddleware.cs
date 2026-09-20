using System.Text.Json;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Excepciones;

namespace SubastaYa.Api.Middleware;

/// <summary>
/// Traduce las excepciones de dominio a codigos HTTP. Centralizarlo permite que los
/// servicios senalicen un fallo sin conocer HTTP y que los controladores no repitan un
/// try/catch cada uno. Lo inesperado se registra en el log y al cliente le llega un 500
/// generico, nunca el stack trace.
/// </summary>
public class ManejadorDeExcepcionesMiddleware
{
    private static readonly JsonSerializerOptions OpcionesJson =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorDeExcepcionesMiddleware> _logger;

    public ManejadorDeExcepcionesMiddleware(
        RequestDelegate siguiente,
        ILogger<ManejadorDeExcepcionesMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            // Si la respuesta ya empezo a escribirse no se pueden cambiar los headers.
            if (contexto.Response.HasStarted)
            {
                _logger.LogError(ex, "Excepcion despues de iniciada la respuesta.");
                throw;
            }

            var (codigo, cuerpo) = Traducir(ex);

            if (codigo == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Excepcion no controlada.");
            }

            contexto.Response.Clear();
            contexto.Response.StatusCode = codigo;
            contexto.Response.ContentType = "application/json";

            await contexto.Response.WriteAsync(
                JsonSerializer.Serialize(cuerpo, OpcionesJson));
        }
    }

    private static (int Codigo, object Cuerpo) Traducir(Exception ex) => ex switch
    {
        RecursoNoEncontradoException =>
            (StatusCodes.Status404NotFound, Mensaje(ex)),

        ReglaDeNegocioException =>
            (StatusCodes.Status422UnprocessableEntity, Mensaje(ex)),

        AccionProhibidaException =>
            (StatusCodes.Status403Forbidden, Mensaje(ex)),

        // Las dos causas de 409. La de concurrencia puede traer un cuerpo enriquecido.
        ConflictoDeConcurrenciaException c =>
            (StatusCodes.Status409Conflict, c.Detalle ?? Mensaje(ex)),

        RecursoDuplicadoException =>
            (StatusCodes.Status409Conflict, Mensaje(ex)),

        _ => (StatusCodes.Status500InternalServerError,
              (object)new ErrorResponse("Ocurrió un error inesperado procesando la petición."))
    };

    private static object Mensaje(Exception ex) => new ErrorResponse(ex.Message);
}
