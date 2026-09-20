// Sala en vivo: historial de ofertas y actualizacion de la pantalla sin recargar.

let subastaId = null;
let conexion = null;
let cerradaPorFin = false;
let cantidadOfertas = 0;
let montoMostrado = 0;
const idsEnHistorial = new Set();

document.addEventListener('subasta:cargada', async evento => {
    const detalle = evento.detail;
    const primeraVez = subastaId === null;
    subastaId = detalle.id;
    cantidadOfertas = detalle.cantidadOfertas;
    montoMostrado = detalle.pujaActual;

    await cargarHistorial();

    // Una subasta cerrada ya no recibe ofertas: no hace falta conexion en vivo.
    const abierta = detalle.estado === 'Activa' || detalle.estado === 'Programada';
    if (primeraVez && abierta) conectar();
    if (!abierta && conexion) {
        // Se corta a proposito: la subasta termino. No es una falla de conexion.
        cerradaPorFin = true;
        conexion.stop();
    }
});

async function conectar() {
    conexion = new signalR.HubConnectionBuilder()
        .withUrl(HUB_SUBASTAS)
        .withAutomaticReconnect()
        .build();

    conexion.on('NuevaPuja', puja => registrarPujaEnSala(puja));
    conexion.on('SubastaFinalizada', () => recargarEstado());

    conexion.onreconnecting(() => mostrarConexion('reconectando'));
    conexion.onreconnected(async () => {
        // El grupo esta atado a la conexion, y una reconexion es una conexion nueva:
        // sin volver a unirse, la sala queda conectada pero no recibe nada mas.
        await conexion.invoke('UnirseASubasta', subastaId);
        // SignalR no reenvia lo que se perdio mientras estuvo caida.
        await recargarEstado();
        mostrarConexion('conectado');
    });
    conexion.onclose(() => mostrarConexion(cerradaPorFin ? 'finalizada' : 'desconectado'));

    try {
        await conexion.start();
        await conexion.invoke('UnirseASubasta', subastaId);
        mostrarConexion('conectado');
    } catch {
        mostrarConexion('desconectado');
    }
}

function mostrarConexion(estado) {
    const textos = {
        conectado: 'En vivo: las ofertas se actualizan solas.',
        reconectando: 'Reconectando…',
        desconectado: 'Sin conexión en vivo. Recargá la página para ver las últimas ofertas.',
        finalizada: ''
    };
    document.getElementById('estado-conexion').textContent = textos[estado];
}

async function cargarHistorial() {
    const lista = document.getElementById('historial-pujas');
    idsEnHistorial.clear();
    try {
        const pujas = await api.get(`/auctions/${subastaId}/bids`);
        lista.replaceChildren(...pujas.map(crearFilaHistorial));
        pujas.forEach(p => idsEnHistorial.add(p.id));
        if (pujas.length === 0) lista.replaceChildren(crearFilaVacia());
    } catch (error) {
        lista.replaceChildren(crearFilaVacia(error.message));
    }
    actualizarEstadoPostor();
}

function crearFilaHistorial(puja) {
    const fila = document.createElement('li');
    fila.className = 'list-group-item d-flex justify-content-between align-items-center';
    fila.dataset.seudonimo = puja.seudonimo;

    const quien = document.createElement('span');
    quien.textContent = puja.seudonimo;

    const hora = document.createElement('span');
    hora.className = 'small text-muted';
    hora.textContent = formatearHoraExacta(puja.fechaPuja);

    const monto = document.createElement('span');
    monto.className = 'fw-semibold';
    monto.textContent = formatearPesos(puja.monto);

    fila.append(quien, hora, monto);
    return fila;
}

function crearFilaVacia(texto = 'Todavía no hay ofertas.') {
    const fila = document.createElement('li');
    fila.className = 'list-group-item text-muted js-sin-ofertas';
    fila.textContent = texto;
    return fila;
}

// Punto unico por donde entra una oferta nueva, venga del canal en vivo o de la
// respuesta al propio POST. Quien oferta recibe las dos: el id evita contarla dos veces.
function registrarPujaEnSala(puja) {
    if (idsEnHistorial.has(puja.id)) return;

    // Una oferta que llega despues de otra mas alta (por ejemplo, la respuesta al propio
    // POST cuando el aviso de otra oferta ya llego) no puede pisar lo que se ve: se
    // vuelve a pedir el estado completo, que ya la incluye en su lugar.
    if (puja.monto <= montoMostrado) {
        recargarEstado();
        return;
    }

    idsEnHistorial.add(puja.id);
    cantidadOfertas++;
    montoMostrado = puja.monto;

    const lista = document.getElementById('historial-pujas');
    lista.querySelector('.js-sin-ofertas')?.remove();
    lista.prepend(crearFilaHistorial(puja));

    document.getElementById('puja-actual').textContent = formatearPesos(puja.monto);
    document.getElementById('lider').textContent = puja.seudonimo;
    document.getElementById('oferta-minima').textContent = formatearPesos(puja.montoMinimo);
    document.getElementById('ofertas').textContent =
        cantidadOfertas === 1 ? '1 oferta' : `${cantidadOfertas} ofertas`;

    // contador.js lee data-fecha-fin en cada segundo: alcanza con cambiar el dato.
    const contador = document.querySelector('#tiempo [data-fecha-fin]');
    if (contador) contador.dataset.fechaFin = puja.fechaFin;

    if (puja.tiempoExtendido) mostrarExtension(puja.fechaFin);
    actualizarEstadoPostor();

    document.dispatchEvent(new CustomEvent('sala:actualizada', {
        detail: {
            pujaActual: puja.monto,
            montoMinimo: puja.montoMinimo,
            seudonimoLider: puja.seudonimo,
            fechaFin: puja.fechaFin
        }
    }));
}

// Solo para quien ya oferto en esta subasta: alguien que solo mira no va ganando ni
// perdiendo. Se compara por seudonimo, que es unico y es lo que muestra el historial.
function actualizarEstadoPostor() {
    const badge = document.getElementById('estado-postor');
    const usuario = obtenerUsuario();
    const lista = document.getElementById('historial-pujas');
    const participa = usuario && lista.querySelector(
        `[data-seudonimo="${CSS.escape(usuario.seudonimo)}"]`);

    if (!participa) { badge.hidden = true; return; }

    const lidera = document.getElementById('lider').textContent === usuario.seudonimo;
    badge.textContent = lidera ? 'Liderando' : 'Superado';
    badge.className = `badge ms-1 ${lidera ? 'text-bg-success' : 'text-bg-warning'}`;
    badge.hidden = false;
}

function mostrarExtension(fechaFin) {
    const aviso = document.getElementById('aviso-extension');
    aviso.textContent =
        `Tiempo extendido: una oferta llegó sobre el cierre. Ahora termina a las ${formatearHoraExacta(fechaFin)}.`;
    aviso.hidden = false;
    clearTimeout(mostrarExtension.temporizador);
    mostrarExtension.temporizador = setTimeout(() => { aviso.hidden = true; }, 8000);
}

// Al reconectar o cuando el servidor avisa un cierre: se vuelve a pedir el detalle y
// se redibuja. dibujarDetalle vuelve a disparar subasta:cargada, que recarga el
// historial y le avisa a puja.js si la subasta ya no acepta ofertas.
async function recargarEstado() {
    try {
        const detalle = await api.get(`/auctions/${subastaId}`);
        dibujarDetalle(detalle);
    } catch {
        mostrarConexion('desconectado');
    }
}
