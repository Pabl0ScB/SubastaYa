# SubastaYa

Eesta es una plataforma web de subastas en tiempo real con billetera virtual y saldo en
garantía (escrow).

Trabajo Práctico de la cátedra **Proyecto de Software** — Carrera de Ingeniería
en Informática, Instituto de Ingeniería y Agronomía. **Universidad Nacional Arturo Jauretche**

## ⏭ Integrantes del proyecto ⏭

- Pablo Daniel Scardiglia Billordo — [@Pabl0ScB](https://github.com/Pabl0ScB)
- Alexis Lionel Monte - [@montealexis5-create](https://github.com/montealexis5-create)

## Descripción

SubastaYa es una plataforma de subastas en línea que está construida con estos dos criterios:

- **Solvencia garantizada:** toda puja que se haga se respalda por un saldo real que está retenido
  en garantía (*escrow*) dentro de la billetera virtual del usuario, cosa que no se pueda
  ofertar dinero que no tenga disponible.
- **Juego limpio:** A partir de un mecanismo de extensión automática de tiempo conocido como
  (*anti-sniping*) evita que haya victorias a partir de ofertas de último momento.

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | C# / ASP.NET Core Web API |
| ORM | Entity Framework Core (enfoque Code-First) |
| Base de datos | PostgreSQL 16 (contenedor Docker) |
| Tiempo real | SignalR |
| Frontend | HTML, CSS y JavaScript (Vanilla) + Bootstrap |
| Documentación de API | OpenAPI / Swagger UI |

## Estado 

En desarrollo — fase de diseño.

## Requisitos previos

| Herramienta | Versión | Para qué |
|---|---|---|
| .NET SDK | 10.0 (desarrollado con 10.0.400) | Compilar y ejecutar la API |
| Docker Desktop | Cualquiera reciente | Levantar PostgreSQL y pgAdmin |
| Git | Cualquiera reciente | Clonar el repositorio |
| `dotnet-ef` | 10.x | Aplicar las migraciones |
| Live Server | Extensión de VS Code | Servir el frontend |

La herramienta de EF Core se instala una sola vez, de forma global:

```bash
dotnet tool install --global dotnet-ef
```

Para verificar el SDK, `dotnet --version` tiene que devolver una versión **10.x**. El
proyecto apunta a `net10.0` y no compila con versiones anteriores.

## Puesta en marcha

### 1. Clonar el repositorio

```bash
git clone https://github.com/Pabl0ScB/SubastaYa.git
cd SubastaYa
```

### 2. Levantar la base de datos

```bash
docker compose up -d
```

Levanta dos contenedores: PostgreSQL 16 en el puerto `5432` y pgAdmin en el `5050`.

### 3. Configurar los secretos

La cadena de conexión y la clave de firma de los tokens **no están en el repositorio**:
se guardan con la herramienta de secretos de usuario del SDK, que los deja fuera del
proyecto. El repositorio es público, y quien tenga la clave de firma puede generar un
token válido para cualquier usuario.

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=subastaya;Username=subastaya;Password=subastaya_dev" -p src/SubastaYa.Api

dotnet user-secrets set "Jwt:Clave" "clave-de-desarrollo-de-al-menos-32-caracteres" -p src/SubastaYa.Api
```

La cadena es la que corresponde al `docker-compose.yml` de este repositorio. La clave
puede ser cualquier texto, pero **de 32 caracteres o más**: el algoritmo de firma
necesita una clave de ese tamaño, y con una más corta la aplicación arranca igual pero
falla al iniciar sesión, cuando tiene que firmar el token.

> Si salteás este paso, los comandos siguientes fallan al intentar conectarse a la base
> o al construir la clave de firma. No hace falta `dotnet user-secrets init`: el
> identificador ya está en el archivo de proyecto.

### 4. Crear las tablas

```bash
dotnet ef database update -p src/SubastaYa.Infrastructure -s src/SubastaYa.Api
```

Durante la creación puede aparecer un `fail` con un error de conexión: es esperable, EF
intenta conectarse, ve que la base no existe todavía y la crea.

### 5. Ejecutar la API

```bash
dotnet run --project src/SubastaYa.Api
```

Queda escuchando en `http://localhost:5093` y la documentación interactiva está en
`http://localhost:5093/swagger`.

**Los datos de prueba se cargan solos** la primera vez, con la base vacía. Si ya hay
usuarios cargados, el sembrado no hace nada.

Además queda corriendo el proceso en segundo plano que adjudica las subastas: cada 10
segundos activa las programadas que llegaron a su hora, cierra las vencidas liquidando
el pago del comprador al vendedor y marca desiertas las que no recibieron ofertas. Se ve
en la consola de la aplicación.

### 6. Abrir el frontend

Con la API corriendo, abrir `frontend/index.html` con **Live Server** (clic derecho →
*Open with Live Server*). Sirve cualquier servidor de archivos estáticos, siempre que
use el puerto indicado abajo.

Tiene que quedar servido en `http://127.0.0.1:5500` o `http://localhost:5500`: son los
dos únicos orígenes que la API acepta. Si Live Server usa otro puerto, hay que agregarlo
a la política de CORS en `Program.cs`.

La dirección de la API está en `frontend/js/config.js`, por si se cambia el puerto.

### 7. pgAdmin (opcional)

`http://localhost:5050`, con `admin@subastaya.com` y contraseña `admin`. Al registrar el
servidor, el host es **`db`** —el nombre del contenedor dentro de la red de Docker—, no
`localhost`; el resto de los datos son los del `docker-compose.yml`.

### Empezar de nuevo

Para volver al estado inicial, con la API detenida:

```bash
dotnet ef database drop -f -p src/SubastaYa.Infrastructure -s src/SubastaYa.Api
dotnet ef database update -p src/SubastaYa.Infrastructure -s src/SubastaYa.Api
```

Al arrancar la aplicación se vuelven a cargar los datos de prueba. Conviene hacerlo antes
de una demostración: algunas subastas del sembrado nacen vencidas a propósito y el
proceso en segundo plano las cierra en cuanto arranca.

## Prueba de concurrencia

Dos personas que ofertan el mismo monto en el mismo instante no pueden quedar las dos
como líderes: sería la misma subasta con saldo retenido en dos billeteras. El control de
concurrencia optimista lo impide — la columna `Version` de la subasta y de las
billeteras—, y `scripts/prueba-concurrencia.ps1` lo demuestra.

El script crea sus propios postores con saldo simulado, así no depende de quién esté
liderando en ese momento, y dispara todas las ofertas juntas para que lleguen al
servidor a la vez. No modifica nada de lo que ya existe.

Con la API corriendo y la base sembrada, desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/prueba-concurrencia.ps1
```

Salida de una ejecución real (los identificadores, los montos y el nombre de la subasta
cambian según el estado de la base):

```
Subasta 6 - PS5 Slim (estado: Activa)
5 postores de prueba creados, con saldo simulado cargado.

Ronda 1 - 5 ofertas simultaneas de $ 1.105.000,00
  prueba-951477-1        201  {"fechaFin":"2026-09-20T23:00:00Z","montoMinimo":1120000.00,
                               "tiempoExtendido":false,"id":11,"monto":1105000.00,
                               "fechaPuja":"2026-09-20T18:17:29.4019206Z","seudonimo":"prueba-951477-1"}
  prueba-951477-2        409  {"mensaje":"Otra oferta se registró antes que la tuya. La oferta
                               mínima ahora es de $ 1.120.000,00.","pujaActual":1105000.00,
                               "montoMinimo":1120000.00,"fechaFin":"2026-09-20T23:00:00Z"}
  prueba-951477-3        409  {"mensaje":"Otra oferta se registró antes que la tuya. ...
  prueba-951477-4        409  {"mensaje":"Otra oferta se registró antes que la tuya. ...
  prueba-951477-5        409  {"mensaje":"Otra oferta se registró antes que la tuya. ...

OK: las ofertas simultaneas se rechazaron con 409 y solo una por ronda fue aceptada.
Verificacion en la base: la tabla RegistrosAuditoria tiene una fila PUJA_RECHAZADA_CONCURRENCIA por cada 409.
```

La respuesta del conflicto no es un error seco: trae la oferta actual, el nuevo mínimo y
la fecha de cierre, que es lo que necesita la pantalla para ofrecer el reintento en un
solo clic.

**Una sola aceptada y el resto rechazadas**: eso es lo que se está probando. El script
termina con código 1 si alguna ronda acepta más de una oferta.

Si en cambio informa que ninguna ronda produjo un conflicto, las ofertas no llegaron a
superponerse: se vuelve a ejecutar, o se aumenta la cantidad de postores con
`-Postores 10`.

Los rechazos quedan registrados. En la base:

```sql
SELECT "Accion", COUNT(*)
FROM "RegistrosAuditoria"
GROUP BY "Accion";
```

Tiene que haber una fila `PUJA_RECHAZADA_CONCURRENCIA` por cada 409.

Y ningún saldo queda retenido de más — el libro mayor respalda cada peso de cada
billetera:

```sql
SELECT b."UsuarioId", b."SaldoTotal",
       SUM(CASE WHEN a."Tipo" IN ('Deposito','Cobro') THEN a."Monto"
                WHEN a."Tipo" = 'Pago' THEN -a."Monto" ELSE 0 END) AS total_segun_ledger
FROM "Billeteras" b
LEFT JOIN "AsientosLedger" a ON a."BilleteraId" = b."Id"
GROUP BY b."UsuarioId", b."SaldoTotal"
ORDER BY b."UsuarioId";
```

Las dos columnas tienen que coincidir en todas las filas.
