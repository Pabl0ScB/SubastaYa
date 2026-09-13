namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Forma unica de los errores que devuelve la API en los casos de negocio
/// (404, 403, 409, 422) y en el 500 generico.
///
/// El 400 por formato invalido es la excepcion: lo genera ASP.NET automaticamente a
/// partir de las anotaciones del DTO y usa el formato estandar ProblemDetails, con el
/// detalle campo por campo. No se unifica con este a proposito: ese detalle por campo
/// es util para el frontend y lo da el framework sin costo.
/// </summary>
public class ErrorResponse
{
    public string Mensaje { get; set; } = null!;

    public ErrorResponse() { }

    public ErrorResponse(string mensaje) => Mensaje = mensaje;
}
