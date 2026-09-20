namespace SubastaYa.Api.Configuracion;

/// <summary>
/// Parametros del proceso en segundo plano que adjudica las subastas, leidos de la
/// seccion "Worker" de appsettings.json. En configuracion y no en el codigo para poder
/// acelerar o frenar el ciclo durante una demostracion sin recompilar.
/// </summary>
public class OpcionesWorker
{
    public const string Seccion = "Worker";

    /// <summary>Cada cuanto revisa las subastas. Mas corto, el cierre se ve casi en el momento.</summary>
    public int IntervaloSegundos { get; set; } = 10;
}
