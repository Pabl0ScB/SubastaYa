using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Api.DTOs.Entrada;

// Solo el monto: el id de la subasta viene en la ruta y el del postor sale del token.
public class CrearPujaRequest : IValidatableObject
{
    // ParseLimitsInInvariantCulture es obligatorio: sin el, en una maquina configurada
    // en castellano "0.01" no se puede leer como numero y toda puja termina en 500.
    [Range(typeof(decimal), "0.01", "1000000000",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El monto debe ser mayor a cero y no superar $1.000.000.000.")]
    public decimal Monto { get; set; }

    // Mismo criterio que DepositoRequest: se rechaza en vez de redondear en silencio,
    // para que la respuesta y la auditoria no muestren un valor distinto al enviado.
    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (decimal.Round(Monto, 2) != Monto)
        {
            yield return new ValidationResult(
                "El monto no puede tener más de dos decimales.",
                new[] { nameof(Monto) });
        }
    }
}
