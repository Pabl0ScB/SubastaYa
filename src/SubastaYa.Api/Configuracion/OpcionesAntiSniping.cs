namespace SubastaYa.Api.Configuracion;

/// <summary>
/// Parametros de la regla anti-sniping, leidos de la seccion "AntiSniping" de
/// appsettings.json. Estan en configuracion y no en el codigo para poder ajustarlos sin
/// recompilar, por ejemplo agrandar la ventana durante una demostracion.
/// </summary>
public class OpcionesAntiSniping
{
    public const string Seccion = "AntiSniping";

    /// <summary>Una oferta que llega con este margen o menos antes del cierre lo extiende.</summary>
    public int UmbralSegundos { get; set; } = 60;

    /// <summary>Cuanto se corre la fecha de fin cuando se dispara la regla.</summary>
    public int ExtensionMinutos { get; set; } = 2;
}
