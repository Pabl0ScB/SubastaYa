// Centraliza todas las llamadas HTTP a la API: el token y los headers viven
// unicamente en este archivo.

const CLAVE_TOKEN = 'subastaya.token';
const CLAVE_USUARIO = 'subastaya.usuario';

// La sesion vive en sessionStorage y no en localStorage: se borra al cerrar la
// pestana, que es lo razonable para un token de acceso sin renovacion.
function obtenerToken() {
    return sessionStorage.getItem(CLAVE_TOKEN);
}

function obtenerUsuario() {
    const crudo = sessionStorage.getItem(CLAVE_USUARIO);
    return crudo ? JSON.parse(crudo) : null;
}

function guardarSesion(sesion) {
    sessionStorage.setItem(CLAVE_TOKEN, sesion.token);
    sessionStorage.setItem(CLAVE_USUARIO, JSON.stringify(sesion.usuario));
}

function borrarSesion() {
    sessionStorage.removeItem(CLAVE_TOKEN);
    sessionStorage.removeItem(CLAVE_USUARIO);
}

// Error con el codigo HTTP y el cuerpo, para que cada pantalla decida que hacer
// segun el status en vez de leer el texto del mensaje.
class ErrorApi extends Error {
    constructor(status, mensaje, cuerpo) {
        super(mensaje);
        this.name = 'ErrorApi';
        this.status = status;
        this.cuerpo = cuerpo;
    }

    // Errores de formato: los devuelve ASP.NET como ProblemDetails, con un arreglo
    // de mensajes por cada campo invalido. Permite senalar el campo en el formulario.
    get erroresPorCampo() {
        return this.cuerpo && this.cuerpo.errors ? this.cuerpo.errors : null;
    }
}

async function llamarApi(ruta, opciones = {}) {
    const headers = { ...(opciones.headers || {}) };

    // Sin cuerpo no hay nada que describir, y un Content-Type en un GET hace que
    // algunos navegadores disparen una peticion preflight innecesaria.
    if (opciones.body !== undefined) {
        headers['Content-Type'] = 'application/json';
    }

    const token = obtenerToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    let respuesta;
    try {
        respuesta = await fetch(`${API_BASE}${ruta}`, { ...opciones, headers });
    } catch {
        // fetch solo rechaza cuando la peticion no llega a destino: API apagada,
        // sin red o bloqueo de CORS. No hay status porque no hubo respuesta.
        throw new ErrorApi(0, 'No se pudo conectar con el servidor.', null);
    }

    // fetch NO lanza ante un 4xx ni un 5xx: hay que mirar el status a mano. Es el
    // error mas comun al usar fetch y aca importa, porque el manejo del 409 depende
    // de que estas respuestas lleguen al catch de la pantalla.
    if (!respuesta.ok) {
        const cuerpo = await leerCuerpo(respuesta);
        throw new ErrorApi(respuesta.status, mensajeDeError(respuesta.status, cuerpo), cuerpo);
    }

    // 201 Created y 204 No Content pueden venir sin cuerpo: pedirles .json()
    // lanzaria un error de parseo sobre una respuesta exitosa.
    return leerCuerpo(respuesta);
}

async function leerCuerpo(respuesta) {
    if (respuesta.status === 204) return null;
    const texto = await respuesta.text();
    if (!texto) return null;
    try {
        return JSON.parse(texto);
    } catch {
        return null;
    }
}

function mensajeDeError(status, cuerpo) {
    // Los errores de negocio traen { mensaje }. Se prefiere ese texto: ya viene
    // redactado para el usuario desde el servicio que lo lanzo.
    if (cuerpo && cuerpo.mensaje) return cuerpo.mensaje;

    if (cuerpo && cuerpo.errors) {
        const primero = Object.values(cuerpo.errors)[0];
        if (Array.isArray(primero) && primero.length) return primero[0];
    }

    if (status === 401) return 'Tenes que iniciar sesion para hacer esto.';
    if (status === 403) return 'No tenes permiso para hacer esto.';
    if (status === 404) return 'No encontramos lo que buscabas.';
    if (status >= 500) return 'El servidor tuvo un problema. Intentalo de nuevo en un momento.';
    return 'Ocurrio un error inesperado.';
}

const api = {
    get: (ruta) => llamarApi(ruta),
    post: (ruta, cuerpo) => llamarApi(ruta, { method: 'POST', body: JSON.stringify(cuerpo) }),
    put: (ruta, cuerpo) => llamarApi(ruta, { method: 'PUT', body: JSON.stringify(cuerpo) }),
    delete: (ruta) => llamarApi(ruta, { method: 'DELETE' })
};
