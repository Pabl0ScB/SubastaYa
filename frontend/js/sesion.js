// Estado de sesion del lado del navegador: proteger las paginas privadas y adaptar
// la barra de navegacion. Depende de api.js, que es donde vive el token.

function haySesion() {
    return obtenerToken() !== null;
}

// Marca si la pagina actual exige sesion. El listener de "storage" lo usa para saber
// si cerrar sesion en otra pestana debe redirigir a esta o solo actualizar la barra.
let paginaPrivada = false;

// Se llama al principio de las paginas privadas. Devuelve false cuando ya disparo la
// redireccion, para que la pagina corte su propia carga.
function requiereSesion() {
    if (haySesion()) {
        paginaPrivada = true;
        return true;
    }

    // Se recuerda a donde queria entrar para volver ahi despues del login, en vez de
    // dejarlo siempre en el catalogo.
    const destino = window.location.pathname.split('/').pop() + window.location.search;
    window.location.replace(`login.html?volverA=${encodeURIComponent(destino)}`);
    return false;
}

// Esta guarda es de comodidad, no de seguridad: cualquiera puede borrarla desde la
// consola del navegador. Lo que protege de verdad los datos es el [Authorize] del
// backend, que responde 401 sin un token valido.
function cerrarSesion() {
    borrarSesion();
    window.location.href = 'login.html';
}

// Muestra los enlaces que corresponden al estado actual. Los elementos se marcan en el
// HTML con data-sesion="privado" o data-sesion="anonimo".
function ajustarNavegacion() {
    const logueado = haySesion();

    document.querySelectorAll('[data-sesion="privado"]')
        .forEach(el => { el.hidden = !logueado; });

    document.querySelectorAll('[data-sesion="anonimo"]')
        .forEach(el => { el.hidden = logueado; });

    const usuario = obtenerUsuario();
    const etiqueta = document.getElementById('usuario-actual');
    if (etiqueta && usuario) etiqueta.textContent = usuario.seudonimo;

    const boton = document.getElementById('boton-cerrar-sesion');
    if (boton) boton.addEventListener('click', cerrarSesion);
}

document.addEventListener('DOMContentLoaded', ajustarNavegacion);

// La sesion vive en localStorage, compartido entre pestanas. El evento "storage" avisa
// a las otras pestanas cuando cambia el token: sin esto, cerrar (o iniciar) sesion en
// una pestana dejaria a las demas con una sesion que ya no corresponde hasta que
// alguien las recargue a mano. Solo redirige al login a las paginas privadas: el
// catalogo y el detalle de una subasta se pueden ver sin sesion.
window.addEventListener('storage', evento => {
    if (evento.key !== CLAVE_TOKEN) return;
    if (!haySesion() && paginaPrivada) {
        window.location.href = 'login.html';
    } else {
        ajustarNavegacion();
    }
});
