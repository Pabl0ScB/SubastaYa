document.addEventListener('DOMContentLoaded', () => {
    const parametros = new URLSearchParams(location.search);
    const id = parametros.get('id');

    if (!id || !/^\d+$/.test(id)) {
        mostrarEstado('no-encontrada');
        return;
    }

    cargarDetalle(id);
});

function mostrarEstado(estado) {
    document.getElementById('cargando-subasta').hidden = estado !== 'cargando';
    document.getElementById('detalle-subasta').hidden  = estado !== 'detalle';
    document.getElementById('no-encontrada').hidden     = estado !== 'no-encontrada';
    if (estado !== 'error') ocultarAlerta('alerta-subasta');
}

async function cargarDetalle(id) {
    mostrarEstado('cargando');

    try {
        const detalle = await api.get(`/auctions/${id}`);
        dibujarDetalle(detalle);
        mostrarEstado('detalle');
    } catch (error) {
        if (error.status === 404) {
            mostrarEstado('no-encontrada');
        } else {
            mostrarEstado('error');
            mostrarAlerta('alerta-subasta', error.message);
        }
    }
}

function dibujarDetalle(detalle) {
    document.title = `${detalle.titulo} — SubastaYa`;

    document.getElementById('categoria').textContent = detalle.nombreCategoria;
    document.getElementById('titulo').textContent = detalle.titulo;
    document.getElementById('vendedor').textContent = detalle.seudonimoVendedor;
    document.getElementById('descripcion').textContent = detalle.descripcion;

    document.getElementById('puja-actual').textContent = formatearPesos(detalle.pujaActual);
    document.getElementById('precio-base').textContent = formatearPesos(detalle.precioBase);
    document.getElementById('incremento').textContent = formatearPesos(detalle.incrementoMinimo);

    const ofertaMinima = detalle.pujaActual + detalle.incrementoMinimo;
    document.getElementById('oferta-minima').textContent = formatearPesos(ofertaMinima);

    const cantidadOfertas = detalle.cantidadOfertas;
    document.getElementById('ofertas').textContent =
        cantidadOfertas === 1 ? '1 oferta' : cantidadOfertas === 0 ? 'Sin ofertas' : `${cantidadOfertas} ofertas`;

    document.getElementById('lider').textContent = detalle.seudonimoLider ?? 'Todavía nadie';

    dibujarEstadoYTiempo(detalle);
    dibujarFoto(detalle);

    // Avisa a los otros scripts de la pantalla (sala en vivo y formulario de oferta)
    // que el detalle esta dibujado, con los datos con los que se dibujo.
    document.dispatchEvent(new CustomEvent('subasta:cargada', { detail: detalle }));
}

function dibujarEstadoYTiempo(detalle) {
    const badgeEstado = document.getElementById('estado');
    const tiempo = document.getElementById('tiempo');

    // Entre que vence la fecha de fin y el proceso que cierra las subastas la procesa, el
    // backend todavía la informa como Activa. En ese intervalo se muestra "Finalizando…"
    // en lugar de un badge "Activa" que contradiga al contador. Mismo criterio que catalogo.js.
    const yaVencida = detalle.estado === 'Activa' && new Date(detalle.fechaFin) <= new Date();
    const estadoVisual = yaVencida ? 'Vencida' : detalle.estado;

    const etiquetas = {
        Programada: 'Próxima',
        Activa: 'Activa',
        Finalizada: 'Finalizada',
        Desierta: 'Desierta',
        Vencida: 'Finalizando…' // aún no sabemos si hubo ganador o quedó desierta; eso lo decide el Worker
    };
    badgeEstado.textContent = etiquetas[estadoVisual] ?? estadoVisual;
    badgeEstado.className = 'badge ' + (
        estadoVisual === 'Activa' ? 'text-bg-success' :
        estadoVisual === 'Programada' ? 'text-bg-secondary' :
        estadoVisual === 'Finalizada' ? 'text-bg-primary' :
        estadoVisual === 'Vencida' ? 'text-bg-warning' :
        'text-bg-dark'
    );

    if (estadoVisual === 'Programada') {
        tiempo.textContent = `Comienza el ${formatearFecha(detalle.fechaInicio)}`;
    } else if (estadoVisual === 'Activa') {
        tiempo.innerHTML = '';
        const span = document.createElement('span');
        span.className = 'contador';
        span.dataset.fechaFin = detalle.fechaFin;
        tiempo.appendChild(span);
    } else if (estadoVisual === 'Vencida') {
        tiempo.textContent = 'Esperando cierre del sistema';
    } else if (estadoVisual === 'Finalizada') {
        tiempo.textContent = `Ganó ${detalle.seudonimoLider}`;
    } else if (estadoVisual === 'Desierta') {
        tiempo.textContent = 'Terminó sin ofertas';
    }
}

function dibujarFoto(detalle) {
    const fondo = document.getElementById('foto-fondo');
    const frente = document.getElementById('foto-frente');

    fondo.src = detalle.urlImagen;
    frente.src = detalle.urlImagen;
    frente.alt = detalle.titulo;

    frente.addEventListener('error', () => {
        document.querySelector('.foto-subasta').hidden = true;
    });
}
