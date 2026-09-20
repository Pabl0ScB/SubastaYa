// Cuenta regresiva de las subastas. Cualquier elemento con data-fecha-fin se actualiza
// solo, en cualquier pantalla.

const UN_MINUTO = 60 * 1000;
const CINCO_MINUTOS = 5 * UN_MINUTO;
const COLORES = ['contador-normal', 'contador-alerta', 'contador-critico'];

// className reemplazaria todas las clases del elemento, incluida "js-contador" que
// otras pantallas van a necesitar para encontrarlo despues. classList.remove/add solo
// tocan las clases de color.
function pintar(elemento, clase) {
    elemento.classList.remove(...COLORES);
    elemento.classList.add(clase);
}

function actualizarContadores() {
    document.querySelectorAll('[data-fecha-fin]').forEach(elemento => {
        // La fecha viene del servidor en UTC absoluto ("2026-09-19T14:30:00Z"). new Date
        // la interpreta correctamente y la resta da el tiempo real, sin importar en que
        // zona horaria este la maquina del usuario.
        const fin = new Date(elemento.dataset.fechaFin);
        const restante = fin - new Date();

        if (restante <= 0) {
            // No se puede afirmar "Finalizada": recien lo confirma el proceso que cierra
            // las subastas vencidas, y hasta que corra el backend la sigue informando
            // como Activa. Mismo texto que usan catalogo.js y subasta.js en ese intervalo.
            elemento.textContent = 'Finalizando…';
            pintar(elemento, 'contador-critico');
            return;
        }

        elemento.textContent = formatear(restante);
        pintar(elemento, restante <= UN_MINUTO ? 'contador-critico' :
                          restante <= CINCO_MINUTOS ? 'contador-alerta' :
                          'contador-normal');
    });
}

function formatear(milisegundos) {
    const total = Math.floor(milisegundos / 1000);
    const dias = Math.floor(total / 86400);
    const horas = Math.floor((total % 86400) / 3600);
    const minutos = Math.floor((total % 3600) / 60);
    const segundos = total % 60;

    if (dias > 0) return `${dias}d ${horas}h`;
    return `${dos(horas)}:${dos(minutos)}:${dos(segundos)}`;
}

const dos = numero => String(numero).padStart(2, '0');

// Un solo intervalo para toda la pagina. Con veinte tarjetas, un setInterval por tarjeta
// serian veinte temporizadores compitiendo y veinte repintados por segundo.
document.addEventListener('DOMContentLoaded', () => {
    actualizarContadores();
    setInterval(actualizarContadores, 1000);
});