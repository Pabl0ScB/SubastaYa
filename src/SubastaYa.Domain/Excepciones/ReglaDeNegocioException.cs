namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// El pedido esta bien formado pero viola una regla del negocio: saldo insuficiente,
/// monto menor al incremento minimo, fecha de fin anterior a la de inicio, subasta que
/// no esta activa. El middleware la traduce a 422 Unprocessable Entity.
///
/// La diferencia con el 400 es la que se pregunta en la defensa: 400 es "no te
/// entiendo" (formato invalido, lo detecta [ApiController] desde las anotaciones del
/// DTO), 422 es "te entiendo pero no puedo hacerlo".
/// </summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}
