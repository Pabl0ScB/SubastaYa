using SubastaYa.Domain.Entidades;

namespace SubastaYa.Domain.Repositorios;

public interface IAsientoLedgerRepository
{
    Task AgregarAsync(AsientoLedger asiento);
    Task<IReadOnlyList<AsientoLedger>> ObtenerPorBilleteraAsync(int billeteraId, int pagina, int tamanoPagina);
    Task<int> ContarPorBilleteraAsync(int billeteraId);
    Task<decimal> SumarPorBilleteraAsync(int billeteraId);
}
