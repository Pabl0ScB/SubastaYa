namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// El usuario esta autenticado pero la accion le esta vedada. Se traduce a 403, que es
/// "se quien sos y por eso no podes", frente al 401 de "no se quien sos".
/// </summary>
public class AccionProhibidaException : Exception
{
    public AccionProhibidaException(string mensaje) : base(mensaje) { }
}
