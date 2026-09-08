using Microsoft.EntityFrameworkCore;
using SubastaYa.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

// Persistencia
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Controllers y documentacion de la API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: el frontend corre en otro puerto (Live Server) y necesita permiso explicito
builder.Services.AddCors(opt => opt.AddPolicy("Frontend", p =>
    p.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.MapControllers();

app.Run();

// ¡¡¡PENDIENTE!!!1!! todavia no corresponde:
// - AddAuthentication + AddJwtBearer, UseAuthentication/UseAuthorization -> Tarea 2.1 
// - Registrar IAsientoLedgerRepository/IRegistroAuditoriaRepository -> Tareas 3.4 y 4.4)
// - UseMiddleware<ManejadorDeExcepcionesMiddleware> -> Tarea 7.1 (jeje)