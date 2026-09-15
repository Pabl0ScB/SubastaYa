namespace SubastaYa.Domain.Excepciones;

/// <summary>
/// El pedido esta bien formado pero viola una regla del negocio: saldo insuficiente,
/// monto menor al incremento minimo, subasta no activa. Se traduce a 422, que es
/// "te entiendo pero no puedo hacerlo", frente al 400 de "no te entiendo".
/// </summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}
