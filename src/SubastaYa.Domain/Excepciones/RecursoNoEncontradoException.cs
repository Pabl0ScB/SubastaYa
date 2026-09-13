namespace SubastaYa.Domain.Excepciones;

/// <summary>El recurso pedido no existe. Se traduce a 404.</summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje) { }
}
