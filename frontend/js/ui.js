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

// Formatos de dinero y fecha compartidos. Se centralizan para que un mismo monto no se
// vea "$150000" en una pantalla y "$ 150.000,00" en otra.
const formatoPesos = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
// Reloj de 24 horas y anio completo: "19/09/2026, 19:39" se lee sin ambiguedad, y en una
// subasta la hora exacta importa.
const formatoFecha = new Intl.DateTimeFormat('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
});

function formatearPesos(monto) {
    return formatoPesos.format(monto);
}

// La API manda las fechas en UTC; el navegador las muestra en la hora local del usuario.
function formatearFecha(fechaIso) {
    return formatoFecha.format(new Date(fechaIso));
}

// Corta lo que se escriba despues del segundo decimal, a la vista del usuario: el monto
// que ve en el campo es exactamente el que se va a mandar.
function limitarADosDecimales(campo) {
    campo.addEventListener('input', () => {
        const [entero, decimales] = campo.value.split('.');
        if (decimales && decimales.length > 2) {
            campo.value = `${entero}.${decimales.slice(0, 2)}`;
        }
    });
}
