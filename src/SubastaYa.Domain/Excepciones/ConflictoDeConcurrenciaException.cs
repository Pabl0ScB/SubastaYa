namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// Dos operaciones compitieron por la misma fila y una perdio: el UPDATE llevaba
/// "AND Version = @valorLeido" y afecto cero filas. Se traduce a 409.
/// </summary>
public class ConflictoDeConcurrenciaException : Exception
{
    /// <summary>
    /// Cuerpo opcional de la respuesta. Permite devolver el estado actualizado del
    /// recurso para que el cliente pueda reintentar con datos frescos.
    /// </summary>
    public object? Detalle { get; }

    public ConflictoDeConcurrenciaException(string mensaje, object? detalle = null)
        : base(mensaje)
    {
        Detalle = detalle;
    }
}
