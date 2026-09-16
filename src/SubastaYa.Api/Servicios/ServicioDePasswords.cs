namespace SubastaYa.Api.Servicios;

public class ServicioDePasswords : IServicioDePasswords
{
    // BCrypt y no SHA-256: SHA-256 esta pensado para ser rapido, que es justo lo que no
    // conviene en una contrasena (una GPU calcula miles de millones por segundo). BCrypt
    // es lento a proposito, con un costo configurable, y guarda el salt dentro del propio
    // hash, lo que inutiliza las tablas precalculadas.
    // El costo se deja explicito porque es el valor que hay que subir con los anos.
    private const int FactorDeCosto = 11;

    public string Hashear(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, FactorDeCosto);

    public bool Verificar(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
