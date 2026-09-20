// Mis actividades: las subastas en las que el usuario oferto y las que publico.

// Nombre visible de cada estado de subasta. "Programada" es el valor del enum; el
// enunciado a esas subastas las llama "Proximas".
const NOMBRES_ESTADO = {
    Programada: 'Próxima',
    Activa: 'Activa',
    Finalizada: 'Finalizada',
    Desierta: 'Desierta'
};

// Color de cada resultado. Los textos los arma el servidor; aca solo se elige como se ven.
const COLORES_RESULTADO = {
    'Liderando': 'text-bg-success',
    'Superado':  'text-bg-warning',
    'Ganada':    'text-bg-primary',
    'No ganada': 'text-bg-secondary'
};

const COLORES_ADJUDICACION = {
    'Adjudicada': 'text-bg-success',
    'En curso':   'text-bg-info',
    'Programada': 'text-bg-light border',
    'Desierta':   'text-bg-secondary'
};

// Las dos pestanas comparten la forma de cargarse y solo cambian de donde leen y como
// arman cada fila.
const PESTANAS = {
    compras:       { ruta: '/users/me/bids',     crearFila: crearFilaCompra,      cargada: false },
    publicaciones: { ruta: '/users/me/auctions', crearFila: crearFilaPublicacion, cargada: false }
};

document.addEventListener('DOMContentLoaded', () => {
    // requiereSesion ya redirigio al login: sin este corte, la pantalla llegaria a pedir
    // datos sin token y mostraria un error antes de irse.
    if (!haySesion()) return;

    cargarPestana('compras');

    // La segunda pestana se carga recien la primera vez que se abre: si el usuario nunca
    // la mira, no se hace esa consulta.
    document.getElementById('pestana-publicaciones').addEventListener('shown.bs.tab', () => {
        if (!PESTANAS.publicaciones.cargada) cargarPestana('publicaciones');
    });
});

function mostrarEstado(prefijo, estado) {
    document.getElementById(`cargando-${prefijo}`).hidden = estado !== 'cargando';
    document.getElementById(`vacio-${prefijo}`).hidden    = estado !== 'vacio';
    document.getElementById(`tabla-${prefijo}`).hidden    = estado !== 'exito';
    if (estado !== 'error') ocultarAlerta(`alerta-${prefijo}`);
}

async function cargarPestana(prefijo) {
    const pestana = PESTANAS[prefijo];
    mostrarEstado(prefijo, 'cargando');

    try {
        const filas = await api.get(pestana.ruta);
        pestana.cargada = true;

        if (filas.length === 0) {
            mostrarEstado(prefijo, 'vacio');
            return;
        }

        const cuerpo = document.getElementById(`cuerpo-${prefijo}`);
        cuerpo.replaceChildren(...filas.map(pestana.crearFila));
        mostrarEstado(prefijo, 'exito');
    } catch (error) {
        mostrarEstado(prefijo, 'error');
        mostrarAlerta(`alerta-${prefijo}`, error.message);
    }
}

function crearFilaCompra(participacion) {
    return crearFila([
        celdaSubasta(participacion.subastaId, participacion.titulo),
        celdaTexto(formatearPesos(participacion.miOfertaMaxima), 'text-end text-nowrap'),
        celdaTexto(formatearPesos(participacion.pujaActual), 'text-end text-nowrap'),
        celdaTexto(formatearFecha(participacion.fechaFin), 'text-nowrap'),
        celdaEtiqueta(participacion.resultado, COLORES_RESULTADO[participacion.resultado])
    ]);
}

function crearFilaPublicacion(publicacion) {
    // El servidor usa "Programada" tambien como estado de adjudicacion; se muestra con el
    // mismo nombre que en la columna Estado para que la fila no diga dos cosas distintas.
    const valorAdjudicacion = publicacion.estadoAdjudicacion;
    const adjudicacion = celdaEtiqueta(
        NOMBRES_ESTADO[valorAdjudicacion] ?? valorAdjudicacion,
        COLORES_ADJUDICACION[valorAdjudicacion]);

    // El ganador va aparte y no dentro de la etiqueta: el servidor manda los dos datos
    // separados justamente para que la pantalla decida como mostrarlos.
    if (publicacion.seudonimoGanador) {
        const ganador = document.createElement('div');
        ganador.className = 'small text-muted mt-1';
        ganador.textContent = publicacion.seudonimoGanador;
        adjudicacion.appendChild(ganador);
    }

    return crearFila([
        celdaSubasta(publicacion.id, publicacion.titulo),
        celdaTexto(NOMBRES_ESTADO[publicacion.estado] ?? publicacion.estado),
        celdaTexto(publicacion.cantidadOfertas, 'text-end'),
        celdaTexto(formatearPesos(publicacion.pujaActual), 'text-end text-nowrap'),
        celdaTexto(formatearPesos(publicacion.recaudado), 'text-end text-nowrap'),
        adjudicacion
    ]);
}

// Constructores de celdas. Todo el contenido entra con textContent: los titulos los
// escribieron otros usuarios y no se interpretan como HTML.
function crearFila(celdas) {
    const fila = document.createElement('tr');
    fila.append(...celdas);
    return fila;
}

function celdaTexto(texto, clases = '') {
    const celda = document.createElement('td');
    celda.className = clases;
    celda.textContent = texto;
    return celda;
}

function celdaSubasta(id, titulo) {
    const celda = document.createElement('td');
    const enlace = document.createElement('a');
    enlace.href = `subasta.html?id=${id}`;
    enlace.textContent = titulo;
    celda.appendChild(enlace);
    return celda;
}

function celdaEtiqueta(texto, clases) {
    const celda = document.createElement('td');
    const etiqueta = document.createElement('span');
    etiqueta.className = `badge ${clases ?? 'text-bg-secondary'}`;
    etiqueta.textContent = texto;
    celda.appendChild(etiqueta);
    return celda;
}
