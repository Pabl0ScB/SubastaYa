namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// El usuario esta autenticado pero la accion le esta vedada. El middleware la traduce
/// a 403 Forbidden.
///
/// No confundir con el 401: ese significa "no se quien sos" (falta el token o vencio).
/// El 403 significa "se quien sos, y justamente por eso no podes". En este sistema el
/// unico caso es pujar en la propia subasta.
/// </summary>
public class AccionProhibidaException : Exception
{
    public AccionProhibidaException(string mensaje) : base(mensaje) { }
}
