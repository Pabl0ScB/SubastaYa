// Helpers de interfaz compartidos por las pantallas: alertas y estado de carga.
// Estan aca y no repetidos en cada pantalla porque son parte de los cuatro estados
// de interfaz que tienen que resolver todas por igual.

function mostrarAlerta(idAlerta, texto, tipo = 'danger') {
    const alerta = document.getElementById(idAlerta);
    if (!alerta) return;
    // textContent y no innerHTML: el texto puede venir del servidor y no se
    // interpreta como HTML.
    alerta.textContent = texto;
    alerta.className = `alert alert-${tipo}`;
    alerta.hidden = false;
}

function ocultarAlerta(idAlerta) {
    const alerta = document.getElementById(idAlerta);
    if (alerta) alerta.hidden = true;
}

// Deshabilita el boton y muestra su spinner mientras hay una peticion en vuelo, para
// que dos clics seguidos no disparen la misma operacion dos veces.
function alternarCargando(idBoton, idSpinner, activo) {
    const boton = document.getElementById(idBoton);
    const spinner = document.getElementById(idSpinner);
    if (boton) boton.disabled = activo;
    if (spinner) spinner.hidden = !activo;
}
