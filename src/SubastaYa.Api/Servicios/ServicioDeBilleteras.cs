using Microsoft.EntityFrameworkCore;
using SubastaYa.Api.DTOs.Salida;
using SubastaYa.Domain.Excepciones;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Api.Servicios;

public class ServicioDeBilleteras : IServicioDeBilleteras
{
    private readonly AppDbContext _contexto;

    public ServicioDeBilleteras(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<BilleteraResponse> ObtenerPorUsuarioAsync(int usuarioId)
    {
        var billetera = await _contexto.Billeteras
            .AsNoTracking()
            .Where(b => b.UsuarioId == usuarioId)
            .Select(b => new BilleteraResponse
            {
                SaldoTotal = b.SaldoTotal,
                SaldoRetenido = b.SaldoRetenido,

                // La resta se escribe aca y no se usa Billetera.SaldoDisponible porque esa
                // propiedad es [NotMapped]: no existe como columna y EF no puede
                // traducirla a SQL. El disponible se calcula siempre, nunca se guarda: una
                // columna mas seria un tercer numero que puede quedar desincronizado de
                // los otros dos.
                SaldoDisponible = b.SaldoTotal - b.SaldoRetenido
            })
            .FirstOrDefaultAsync();

        // Todo usuario recibe su billetera al registrarse, en la misma transaccion. Que
        // falte significa que esa transaccion se rompio, no que el usuario no cargo saldo.
        return billetera
            ?? throw new RecursoNoEncontradoException("El usuario no tiene una billetera asociada.");
    }
}
