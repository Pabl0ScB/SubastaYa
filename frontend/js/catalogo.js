// Catalogo de subastas: grilla, filtros y paginacion.

const filtros = {
    busqueda: '', estado: '', categoriaId: '', precioMin: '', precioMax: '',
    orden: '', pagina: 1, tamano: 9
};

document.addEventListener('DOMContentLoaded', () => {
    conectarFiltros();
    cargarCategorias();
    cargarCatalogo();
});

function mostrarEstado(estado) {
    document.getElementById('cargando-catalogo').hidden   = estado !== 'cargando';
    document.getElementById('grilla-catalogo').hidden     = estado !== 'exito';
    document.getElementById('vacio-catalogo').hidden      = estado !== 'vacio';
    document.getElementById('paginacion-catalogo').hidden = estado !== 'exito';
    if (estado !== 'error') ocultarAlerta('alerta-catalogo');

    if (estado === 'vacio') {
        const hayFiltros = ['estado','categoriaId','precioMin','precioMax']
            .some(clave => filtros[clave] !== '');

        document.querySelector('#vacio-catalogo p').textContent = hayFiltros
            ? 'No hay subastas que coincidan con el filtro.'
            : 'No hay subastas publicadas todavía.';
    }
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
        mostrarEstado('exito');
        dibujarPaginacion(datos);
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
                <p class="mb-1 small text-muted js-ofertas"></p>
                <p class="fs-5 fw-semibold mb-2">$ <span class="js-puja"></span></p>
                <p class="mb-3"><span class="contador js-contador" data-fecha-fin=""></span></p>
                <a class="btn btn-primary mt-auto js-detalle">Ver subasta</a>
            </div>
        </div>`;

    columna.querySelector('.js-titulo').textContent    = subasta.titulo;
    columna.querySelector('.js-categoria').textContent = subasta.nombreCategoria;
    const cantidadOfertas = subasta.cantidadOfertas;
    columna.querySelector('.js-ofertas').textContent =
        cantidadOfertas === 1 ? '1 oferta' : cantidadOfertas === 0 ? 'Sin ofertas' : `${cantidadOfertas} ofertas`;
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
function conectarFiltros() {
    const controles = {
        'filtro-busqueda': 'busqueda',
        'filtro-estado': 'estado',
        'filtro-categoria': 'categoriaId',
        'filtro-precio-min': 'precioMin',
        'filtro-precio-max': 'precioMax',
        'filtro-orden': 'orden'
    };

    Object.entries(controles).forEach(([id, clave]) => {
        document.getElementById(id).addEventListener('change', evento => {
            filtros[clave] = evento.target.value;
            aplicarFiltros();
        });
    });

    document.getElementById('limpiar-filtros').addEventListener('click', () => {
        document.getElementById('filtros-catalogo').reset();
        Object.keys(controles).forEach(id => { filtros[controles[id]] = ''; });
        aplicarFiltros();
    });
}

function aplicarFiltros() {
    // Cualquier cambio de filtro vuelve a la pagina 1. Sin esto, alguien parado en la
    // pagina 3 que filtra por una categoria con dos resultados ve el estado vacio sobre
    // un filtro que si tiene resultados. Es el bug mas comun de esta pantalla.
    filtros.pagina = 1;
    cargarCatalogo();
}

async function cargarCategorias() {
    try {
        const categorias = await api.get('/categories');
        const select = document.getElementById('filtro-categoria');

        categorias.forEach(categoria => {
            const opcion = document.createElement('option');
            opcion.value = categoria.id;
            opcion.textContent = categoria.nombre;
            select.appendChild(opcion);
        });
    } catch {
        // Que falle el desplegable no tiene que impedir ver el catalogo: se deja solo
        // la opcion "Todas" y la pantalla sigue siendo usable.
    }
}