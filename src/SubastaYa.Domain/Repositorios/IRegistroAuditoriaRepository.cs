using SubastaYa.Domain.Entidades;

namespace SubastaYa.Domain.Repositorios;

public interface IRegistroAuditoriaRepository
{
    Task AgregarAsync(RegistroAuditoria registro);
    Task<IReadOnlyList<RegistroAuditoria>> ObtenerPorEntidadAsync(string entidad, int entidadId);
}