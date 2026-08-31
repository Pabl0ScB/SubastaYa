# SubastaYa

Esta es una plataforma web de subastas en tiempo real con billetera virtual y saldo en
garantía (escrow).

Trabajo Práctico de la cátedra **Proyecto de Software** — Carrera de Ingeniería
en Informática, Instituto de Ingeniería y Agronomía. **Universidad Nacional Arturo Jauretche**

## ⏭ Integrantes del proyecto ⏭

- Pablo Daniel Scardiglia Billordo — [@Pabl0ScB](https://github.com/Pabl0ScB)
- Alexis Lionel Monte - 

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

## Puesta en marcha

_(Todavía pendiente: los requisitos, levantar la base de datos, aplicar migraciones,
cargar los data seeds y ejecutar la aplicación.)_

## Prueba de concurrencia

_(Todavía pendiente: script que demuestra el manejo de concurrencia optimista mediante
dos peticiones simultáneas.)_
