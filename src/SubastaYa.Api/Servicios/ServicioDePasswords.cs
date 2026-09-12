namespace SubastaYa.Api.Servicios;

/// <summary>
/// Implementacion con BCrypt.
///
/// Por que BCrypt y no SHA-256, si los dos son hashes:
///
/// 1. SHA-256 esta disenado para ser RAPIDO, que es exactamente lo que no se quiere
///    en una contrasena: una placa de video moderna calcula miles de millones por
///    segundo, asi que probar un diccionario entero por fuerza bruta es cuestion de
///    minutos. BCrypt es deliberadamente lento y su costo es configurable: cada
///    unidad que se le sube duplica el trabajo necesario para calcularlo.
///
/// 2. BCrypt incorpora el salt DENTRO del propio hash. El salt es un valor aleatorio
///    distinto por contrasena, asi que dos usuarios con la misma clave terminan con
///    hashes distintos. Eso inutiliza las tablas precalculadas (rainbow tables) y
///    evita que, mirando la base, se pueda deducir que usuarios comparten contrasena.
///    Con SHA-256 el salt habria que generarlo y guardarlo por separado.
/// </summary>
public class ServicioDePasswords : IServicioDePasswords
{
    /// <summary>
    /// Factor de costo: el hash hace 2^FactorDeCosto iteraciones internas.
    /// Se declara explicito (aunque coincida con el valor por defecto de la libreria)
    /// para dejar a la vista que es el parametro que se sube cuando el hardware mejora.
    /// </summary>
    private const int FactorDeCosto = 11;

    public string Hashear(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, FactorDeCosto);

    public bool Verificar(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
