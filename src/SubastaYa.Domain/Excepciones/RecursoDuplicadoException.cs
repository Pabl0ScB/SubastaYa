namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// El pedido choca con un recurso que ya existe: un email o un seudonimo ya registrados.
/// Se traduce a 409.
/// </summary>
public class RecursoDuplicadoException : Exception
{
    public RecursoDuplicadoException(string mensaje) : base(mensaje) { }
}
