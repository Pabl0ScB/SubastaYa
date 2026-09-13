namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// Dos operaciones compitieron por la misma fila y una perdio: el UPDATE llevaba
/// "AND Version = @valorLeido" en el WHERE y afecto cero filas, asi que EF Core lanzo
/// DbUpdateConcurrencyException y el servicio la traduce a esta. El middleware
/// responde 409 Conflict.
///
/// Detalle es opcional y existe para el motor de pujas: ahi el frontend necesita
/// recibir la puja actual actualizada y el monto minimo sugerido para poder ofrecer el
/// reintento, en vez de un simple mensaje de error. Si viene en null, se responde solo
/// con el mensaje. De este modo el motor de pujas no necesita modificar el middleware:
/// le alcanza con poblar esta propiedad al lanzar la excepcion.
/// </summary>
public class ConflictoDeConcurrenciaException : Exception
{
    public object? Detalle { get; }

    public ConflictoDeConcurrenciaException(string mensaje, object? detalle = null)
        : base(mensaje)
    {
        Detalle = detalle;
    }
}
