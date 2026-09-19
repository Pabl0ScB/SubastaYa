using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

public class DepositoRequest : IValidatableObject
{
    // Minimo: un deposito de cero o negativo no es un monto, es un error de formato, y por
    // eso lo rechaza la anotacion con un 400 antes de llegar al servicio.
    // Maximo: la columna del saldo es numeric(18,2). Sin tope, un monto absurdo desborda
    // la columna y la base responde con un error que terminaria en un 500.
    //
    // ParseLimitsInInvariantCulture: los limites estan escritos como texto con punto
    // decimal. Sin esta opcion se interpretan con la cultura del servidor, y en una
    // maquina configurada en castellano "0.01" no es un numero valido: la validacion
    // misma lanza una excepcion y cualquier deposito termina en 500.
    [Range(typeof(decimal), "0.01", "1000000000",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El monto debe ser mayor a cero y no superar $1.000.000.000.")]
    public decimal Monto { get; set; }

    // La columna guarda dos decimales. Un monto como 10.005 se redondearia al guardarlo,
    // pero la respuesta y la auditoria mostrarian el valor sin redondear: la base diria
    // una cosa y la API otra. Se rechaza en vez de redondear en silencio.
    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (decimal.Round(Monto, 2) != Monto)
        {
            yield return new ValidationResult(
                "El monto no puede tener mas de dos decimales.",
                new[] { nameof(Monto) });
        }
    }
}
