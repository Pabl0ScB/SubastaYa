using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SubastaYa.Api.Servicios;
using SubastaYa.Infrastructure.Persistencia;
using SubastaYa.Infrastructure.Seed;


var builder = WebApplication.CreateBuilder(args);

// Persistencia
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Autenticacion por JWT.
// El servidor no guarda sesiones: confia en el token porque puede recalcular su firma
// con la clave secreta. Por eso la clave vive en user-secrets y nunca en appsettings:
// quien la tenga puede fabricar un token haciendose pasar por cualquier usuario.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!)),

            // Por defecto ASP.NET tolera 5 minutos de gracia sobre la expiracion.
            // Aca no hay varios servidores con relojes desincronizados, asi que la
            // expiracion es exacta.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Servicios de aplicacion.
// Scoped por convencion del proyecto: mismo ciclo de vida que el DbContext y que los
// repositorios, asi todo lo que participa de una misma peticion comparte instancia.
builder.Services.AddScoped<IServicioDePasswords, ServicioDePasswords>();

// Controllers y documentacion de la API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opciones =>
{
    // Habilita el boton "Authorize" de Swagger para pegar el token a mano.
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegar solamente el token, sin escribir 'Bearer' adelante."
    });

    // En Microsoft.OpenApi v2 las referencias se resuelven contra el documento, por eso
    // el requirement se construye dentro de una funcion que lo recibe.
    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", documento), new List<string>() }
    });
});

// CORS: el frontend corre en otro puerto (Live Server) y necesita permiso explicito
builder.Services.AddCors(opt => opt.AddPolicy("Frontend", p =>
    p.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

// El orden importa: UseAuthentication averigua QUIEN es el usuario a partir del token,
// y UseAuthorization decide si ese usuario puede acceder. Invertirlos hace que
// [Authorize] evalue sobre un usuario todavia sin identificar y rechace todo con 401.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Services.AplicarDatosSemilla();
app.Run();

// Pendiente de bloques posteriores:
// - Registrar IAsientoLedgerRepository e IRegistroAuditoriaRepository (Tareas 3.4 y 4.4)
// - UseMiddleware<ManejadorDeExcepcionesMiddleware> (Bloque 7, junto con las
//   excepciones de dominio que traduce)
