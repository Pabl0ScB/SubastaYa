using System.Text.Json;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Excepciones;

namespace SubastaYa.Api.Middleware;

/// <summary>
/// Unico lugar donde las excepciones de dominio se traducen a codigos HTTP.
///
/// Existe para que los servicios puedan senalizar un fallo sin saber nada de HTTP
/// -estan una capa por debajo- y para que los controladores no tengan que repetir un
/// try/catch cada uno. Asi la logica de negocio no queda acoplada al controlador, que
/// se limita a su unica responsabilidad: traducir una peticion HTTP en una respuesta.
///
/// Nunca devuelve el stack trace al cliente: lo inesperado se registra en el log del
/// servidor y al cliente le llega un 500 con un mensaje generico.
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
            // Si la respuesta ya empezo a escribirse no se pueden cambiar los headers;
            // lo unico sensato es dejar que la conexion falle y registrarlo.
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

        // Los dos casos de 409. El de concurrencia puede traer un cuerpo enriquecido
        // (la puja actual, el monto sugerido) para que el frontend ofrezca reintentar.
        ConflictoDeConcurrenciaException c =>
            (StatusCodes.Status409Conflict, c.Detalle ?? Mensaje(ex)),

        RecursoDuplicadoException =>
            (StatusCodes.Status409Conflict, Mensaje(ex)),

        _ => (StatusCodes.Status500InternalServerError,
              (object)new ErrorResponse("Ocurrio un error inesperado procesando la peticion."))
    };

    private static object Mensaje(Exception ex) => new ErrorResponse(ex.Message);
}
