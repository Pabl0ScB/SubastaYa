// Catalogo de subastas: grilla, filtros y paginacion.

const filtros = {
    estado: '', categoriaId: '', precioMin: '', precioMax: '',
    orden: '', pagina: 1, tamano: 9
};

document.addEventListener('DOMContentLoaded', () => {
    cargarCatalogo();
});

function mostrarEstado(estado) {
    document.getElementById('cargando-catalogo').hidden   = estado !== 'cargando';
    document.getElementById('grilla-catalogo').hidden     = estado !== 'exito';
    document.getElementById('vacio-catalogo').hidden      = estado !== 'vacio';
    document.getElementById('paginacion-catalogo').hidden = estado !== 'exito';
    if (estado !== 'error') ocultarAlerta('alerta-catalogo');
}

async function cargarCatalogo() {
    mostrarEstado('cargando');

    try {
        const datos = await api.get(`/auctions?${armarQuery()}`);

        if (datos.items.length === 0) {
            mostrarEstado('vacio');
            return;
        }

        dibujarGrilla(datos.items);
        dibujarPaginacion(datos);
        mostrarEstado('exito');
    } catch (error) {
        mostrarEstado('error');
        mostrarAlerta('alerta-catalogo', error.message);
    }
}

// Los parametros vacios no se mandan: enviar "precioMin=" sin valor puede interpretarse
// distinto a no mandarlo.
function armarQuery() {
    const parametros = new URLSearchParams();
    Object.entries(filtros).forEach(([clave, valor]) => {
        if (valor !== '' && valor !== null && valor !== undefined) {
            parametros.append(clave, valor);
        }
    });
    return parametros.toString();
}

function dibujarGrilla(items) {
    const grilla = document.getElementById('grilla-catalogo');
    grilla.replaceChildren();

    items.forEach(subasta => grilla.appendChild(crearTarjeta(subasta)));
}

function crearTarjeta(subasta) {
    const columna = document.createElement('div');
    columna.className = 'col';

    columna.innerHTML = `
        <div class="card h-100 tarjeta-subasta">
            <img src="" class="card-img-top" alt="">
            <div class="card-body d-flex flex-column">
                <span class="badge text-bg-secondary align-self-start mb-2 js-categoria"></span>
                <h2 class="h6 card-title js-titulo"></h2>
                <p class="mb-1 small text-muted">
                    <span class="js-ofertas"></span> ofertas
                </p>
                <p class="fs-5 fw-semibold mb-2">$ <span class="js-puja"></span></p>
                <p class="mb-3"><span class="contador js-contador" data-fecha-fin=""></span></p>
                <a class="btn btn-primary mt-auto js-detalle">Ver subasta</a>
            </div>
        </div>`;

    columna.querySelector('.js-titulo').textContent    = subasta.titulo;
    columna.querySelector('.js-categoria').textContent = subasta.nombreCategoria;
    columna.querySelector('.js-ofertas').textContent   = subasta.cantidadOfertas;
    columna.querySelector('.js-puja').textContent      = subasta.pujaActual.toLocaleString('es-AR');

    const imagen = columna.querySelector('img');
    imagen.src = subasta.urlImagen;
    imagen.alt = subasta.titulo;
    imagen.addEventListener('error', () => {
        imagen.src = 'data:image/svg+xml,' + encodeURIComponent(
            '<svg xmlns="http://www.w3.org/2000/svg" width="400" height="180">' +
            '<rect width="100%" height="100%" fill="#dee2e6"/></svg>');
    });

    columna.querySelector('.js-contador').dataset.fechaFin = subasta.fechaFin;
    columna.querySelector('.js-detalle').href = `subasta.html?id=${subasta.id}`;

    return columna;
}

function dibujarPaginacion({ paginaActual, totalPaginas }) {
    const barra = document.getElementById('paginacion-catalogo');

    if (totalPaginas <= 1) { barra.hidden = true; return; }

    document.getElementById('pagina-actual').textContent = `${paginaActual} de ${totalPaginas}`;
    document.getElementById('pagina-anterior').parentElement
        .classList.toggle('disabled', paginaActual <= 1);
    document.getElementById('pagina-siguiente').parentElement
        .classList.toggle('disabled', paginaActual >= totalPaginas);
}

document.getElementById('pagina-anterior').addEventListener('click', () => {
    if (filtros.pagina > 1) { filtros.pagina--; cargarCatalogo(); }
});
document.getElementById('pagina-siguiente').addEventListener('click', () => {
    filtros.pagina++; cargarCatalogo();
});