namespace SubastaYa.Domain;

/// <summary>
/// Acciones que el sistema registra en la auditoria. Son constantes y no literales
/// sueltos: con literales, un "PUJA_RECHAZADA_SALDO " con un espacio de mas compila
/// igual y genera una fila que ninguna consulta encuentra. Asi el error es de
/// compilacion.
/// </summary>
public static class AccionesAuditoria
{
    public const string PujaRechazadaSaldo        = "PUJA_RECHAZADA_SALDO";
    public const string PujaRechazadaValidacion   = "PUJA_RECHAZADA_VALIDACION";
    public const string PujaRechazadaConcurrencia = "PUJA_RECHAZADA_CONCURRENCIA";
    public const string ExtensionTiempo           = "EXTENSION_TIEMPO";
    public const string CierreWorker              = "CIERRE_WORKER";
    public const string CierreDesierta            = "CIERRE_DESIERTA";
    public const string AcreditacionManual        = "ACREDITACION_MANUAL";
}