namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// Se lanza cuando el pedido es valido en si mismo pero choca con algo que ya existe:
/// un email o un seudonimo ya registrados. El middleware la traduce a 409 Conflict.
///
/// Es distinta de ConflictoDeConcurrenciaException aunque las dos devuelvan
/// 409: aquella se produce cuando una fila cambio mientras se la leia, y responde con
/// un cuerpo distinto porque el frontend necesita la puja actualizada para ofrecer el
/// reintento.
/// </summary>
public class RecursoDuplicadoException : Exception
{
    public RecursoDuplicadoException(string mensaje) : base(mensaje) { }
}
