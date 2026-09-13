namespace SubastaYa.Api.Servicios;

/// <summary>
/// Hasheo y verificacion de contrasenas. Los consumidores dependen de esta interfaz y no
/// de BCrypt, para poder cambiar de algoritmo tocando una sola clase.
/// </summary>
public interface IServicioDePasswords
{
    string Hashear(string password);

    bool Verificar(string password, string hash);
}
