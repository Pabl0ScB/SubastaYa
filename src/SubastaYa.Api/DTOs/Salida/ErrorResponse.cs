namespace SubastaYa.Api.DTOs.Salida;

/// <summary>
/// Forma de los errores de negocio (403, 404, 409, 422) y del 500 generico.
/// El 400 por formato invalido queda aparte: lo genera ASP.NET como ProblemDetails, con
/// el detalle campo por campo, que es lo que el frontend necesita para senalar el campo.
/// </summary>
public class ErrorResponse
{
    public string Mensaje { get; set; } = null!;

    public ErrorResponse() { }

    public ErrorResponse(string mensaje) => Mensaje = mensaje;
}
