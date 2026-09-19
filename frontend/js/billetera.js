// Billetera: saldos, carga de saldo simulada e historial de movimientos.

// Cada tipo de asiento con el nombre que ve el usuario y el sentido en que mueve su
// dinero. La retencion resta porque deja de estar disponible; la liberacion la devuelve.
const TIPOS_MOVIMIENTO = {
    Deposito:   { nombre: 'Depósito',                signo: '+' },
    Retencion:  { nombre: 'Retención por oferta',    signo: '−' },
    Liberacion: { nombre: 'Liberación',              signo: '+' },
    Pago:       { nombre: 'Pago de subasta ganada',  signo: '−' },
    Cobro:      { nombre: 'Cobro por venta',         signo: '+' }
};

const TAMANO_PAGINA = 10;
const MENSAJE_MONTO = 'Ingresá un monto mayor a cero, con hasta dos decimales.';
let paginaMovimientos = 1;

document.addEventListener('DOMContentLoaded', () => {
    // requiereSesion ya redirigio al login: sin este corte, la pantalla llegaria a pedir
    // los saldos sin token y mostraria un error antes de irse.
    if (!haySesion()) return;

    const campoMonto = document.getElementById('monto-deposito');
    limitarADosDecimales(campoMonto);
    // Un error que marco el servidor deja de valer apenas el usuario corrige el campo.
    campoMonto.addEventListener('input', () => {
        campoMonto.setCustomValidity('');
        document.getElementById('error-monto-deposito').textContent = MENSAJE_MONTO;
    });

    document.getElementById('formulario-deposito').addEventListener('submit', depositar);
    document.getElementById('boton-ver-mas').addEventListener('click', verMasMovimientos);

    cargarBilletera();
});

function mostrarEstado(estado) {
    document.getElementById('cargando-billetera').hidden  = estado !== 'cargando';
    document.getElementById('contenido-billetera').hidden = estado !== 'exito';
    if (estado !== 'error') ocultarAlerta('alerta-billetera');
}

async function cargarBilletera() {
    mostrarEstado('cargando');

    try {
        // En paralelo: son independientes, y esperar una para pedir la otra solo
        // alargaria la carga.
        const [saldos, movimientos] = await Promise.all([
            api.get('/wallets/me'),
            api.get(`/wallets/me/entries?pagina=1&tamano=${TAMANO_PAGINA}`)
        ]);

        dibujarSaldos(saldos);
        paginaMovimientos = 1;
        dibujarMovimientos(movimientos, false);
        mostrarEstado('exito');
    } catch (error) {
        mostrarEstado('error');
        mostrarAlerta('alerta-billetera', error.message);
    }
}

function dibujarSaldos({ saldoTotal, saldoRetenido, saldoDisponible }) {
    document.getElementById('saldo-total').textContent      = formatearPesos(saldoTotal);
    document.getElementById('saldo-retenido').textContent   = formatearPesos(saldoRetenido);
    document.getElementById('saldo-disponible').textContent = formatearPesos(saldoDisponible);
}

// agregar = true suma la pagina nueva debajo de las anteriores ("Ver mas"); false
// reemplaza la tabla, que es lo que se quiere al cargar o despues de un deposito.
function dibujarMovimientos(pagina, agregar) {
    const cuerpo = document.getElementById('cuerpo-movimientos');
    if (!agregar) cuerpo.replaceChildren();

    pagina.items.forEach(movimiento => cuerpo.appendChild(crearFila(movimiento)));

    const hayMovimientos = pagina.totalElementos > 0;
    document.getElementById('vacio-movimientos').hidden = hayMovimientos;
    document.getElementById('tabla-movimientos').hidden = !hayMovimientos;
    document.getElementById('boton-ver-mas').hidden = pagina.paginaActual >= pagina.totalPaginas;
}

function crearFila(movimiento) {
    const tipo = TIPOS_MOVIMIENTO[movimiento.tipo] ?? { nombre: movimiento.tipo, signo: '' };
    const fila = document.createElement('tr');

    const fecha = document.createElement('td');
    fecha.className = 'text-nowrap';
    fecha.textContent = formatearFecha(movimiento.fecha);

    const nombreTipo = document.createElement('td');
    nombreTipo.textContent = tipo.nombre;

    // Detalle: la descripcion y, si el movimiento vino de una subasta, un enlace a ella.
    // Todo con textContent: el titulo de la subasta lo escribio otro usuario.
    const detalle = document.createElement('td');
    detalle.textContent = movimiento.descripcion ?? '';
    if (movimiento.subastaId) {
        const enlace = document.createElement('a');
        enlace.href = `subasta.html?id=${movimiento.subastaId}`;
        enlace.textContent = movimiento.tituloSubasta;
        const linea = document.createElement('div');
        linea.className = 'small';
        linea.appendChild(enlace);
        detalle.appendChild(linea);
    }

    const monto = document.createElement('td');
    monto.className = `text-end text-nowrap fw-semibold ${tipo.signo === '+' ? 'text-success' : 'text-danger'}`;
    monto.textContent = `${tipo.signo} ${formatearPesos(movimiento.monto)}`;

    fila.append(fecha, nombreTipo, detalle, monto);
    return fila;
}

async function verMasMovimientos() {
    alternarCargando('boton-ver-mas', 'spinner-ver-mas', true);
    try {
        const siguiente = paginaMovimientos + 1;
        const pagina = await api.get(`/wallets/me/entries?pagina=${siguiente}&tamano=${TAMANO_PAGINA}`);
        paginaMovimientos = siguiente;
        dibujarMovimientos(pagina, true);
    } catch (error) {
        mostrarAlerta('alerta-billetera', error.message);
    } finally {
        alternarCargando('boton-ver-mas', 'spinner-ver-mas', false);
    }
}

async function depositar(evento) {
    evento.preventDefault();
    const formulario = evento.target;
    const campo = document.getElementById('monto-deposito');

    ocultarAlerta('alerta-deposito');

    // Validacion del navegador antes de gastar una llamada. La que manda sigue siendo la
    // del backend: esta se puede saltear desde la consola.
    if (!formulario.checkValidity()) {
        formulario.classList.add('was-validated');
        return;
    }

    alternarCargando('boton-depositar', 'spinner-depositar', true);

    try {
        const saldos = await api.post('/wallets/me/deposits', { monto: Number(campo.value) });

        // La respuesta ya trae los saldos nuevos: se muestran sin pedirlos otra vez.
        dibujarSaldos(saldos);
        formulario.reset();
        formulario.classList.remove('was-validated');
        mostrarAlerta('alerta-deposito', 'Saldo cargado.', 'success');
    } catch (error) {
        const errorDelCampo = error.erroresPorCampo?.Monto?.[0];
        if (errorDelCampo) {
            // Error de formato: se marca el campo con el mensaje del servidor.
            document.getElementById('error-monto-deposito').textContent = errorDelCampo;
            campo.setCustomValidity(errorDelCampo);
            formulario.classList.add('was-validated');
        } else {
            mostrarAlerta('alerta-deposito', error.message);
        }
        return;
    } finally {
        alternarCargando('boton-depositar', 'spinner-depositar', false);
    }

    await refrescarMovimientos();
}

// Aparte del deposito: si esta consulta falla, el deposito igual se hizo, y el aviso
// tiene que decir eso y no que la carga de saldo fallo.
async function refrescarMovimientos() {
    try {
        const pagina = await api.get(`/wallets/me/entries?pagina=1&tamano=${TAMANO_PAGINA}`);
        // El movimiento nuevo va primero: se vuelve a la primera pagina.
        paginaMovimientos = 1;
        dibujarMovimientos(pagina, false);
    } catch {
        mostrarAlerta('alerta-billetera',
            'El saldo se cargó, pero no se pudieron actualizar los movimientos. Recargá la página.',
            'warning');
    }
}
