namespace SubastaYa.Api.Servicios;

/// <summary>
/// Abstraccion del hasheo de contrasenas.
/// Los controladores y servicios de aplicacion dependen de esta interfaz y no de
/// BCrypt: si manana hubiera que migrar a otro algoritmo (Argon2, por ejemplo),
/// cambia unicamente la implementacion y el resto del codigo queda igual.
/// </summary>
public interface IServicioDePasswords
{
    /// <summary>Devuelve el hash de una contrasena en texto plano.</summary>
    string Hashear(string password);

    /// <summary>Indica si la contrasena en texto plano corresponde al hash dado.</summary>
    bool Verificar(string password, string hash);
}
