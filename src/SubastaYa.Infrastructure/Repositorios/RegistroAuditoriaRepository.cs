using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entidades;
using SubastaYa.Domain.Repositorios;
using SubastaYa.Infrastructure.Persistencia;

namespace SubastaYa.Infrastructure.Repositorios;

/// <summary>
/// Acceso al registro de auditoria. Solo agrega y consulta: la interfaz no declara
/// actualizacion ni borrado, y esta implementacion tampoco los ofrece.
/// </summary>
public class RegistroAuditoriaRepository : IRegistroAuditoriaRepository
{
    private readonly AppDbContext _contexto;

    public RegistroAuditoriaRepository(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <summary>
    /// Confirma el registro en su propia escritura. Es deliberado y distinto del
    /// repositorio del libro mayor, que no confirma: una auditoria de rechazo tiene que
    /// sobrevivir al rollback de la transaccion que la provoco. Si se guardara junto con
    /// esa transaccion, el rollback la borraria y el evento se perderia.
    /// </summary>
    public async Task AgregarAsync(RegistroAuditoria registro)
    {
        _contexto.RegistrosAuditoria.Add(registro);
        await _contexto.SaveChangesAsync();
    }

    /// <summary>Historial de una entidad, del evento mas reciente al mas viejo.</summary>
    public async Task<IReadOnlyList<RegistroAuditoria>> ObtenerPorEntidadAsync(
        string entidad, int entidadId)
    {
        return await _contexto.RegistrosAuditoria
            .AsNoTracking()
            .Where(r => r.Entidad == entidad && r.EntidadId == entidadId)
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }
}