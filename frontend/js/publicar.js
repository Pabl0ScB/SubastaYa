document.addEventListener('DOMContentLoaded', () => {
    cargarCategorias();
    configurarVistaPrevia();
    limitarADosDecimales(document.getElementById('precio-base'));
    limitarADosDecimales(document.getElementById('incremento'));
    configurarEnvio();
});

async function cargarCategorias() {
    const select = document.getElementById('categoria');
    try {
        const categorias = await api.get('/categories');
        categorias.forEach(categoria => {
            const opcion = document.createElement('option');
            opcion.value = categoria.id;
            opcion.textContent = categoria.nombre;
            select.appendChild(opcion);
        });
    } catch (error) {
        mostrarAlerta('alerta-publicar', 'No se pudieron cargar las categorías. Recargá la página.', 'danger');
    }
}

function configurarVistaPrevia() {
    const campoUrl = document.getElementById('url-imagen');
    const vistaPrevia = document.getElementById('vista-previa');

    campoUrl.addEventListener('input', () => {
        const url = campoUrl.value.trim();
        if (!url) {
            vistaPrevia.hidden = true;
            return;
        }
        vistaPrevia.src = url;
        vistaPrevia.hidden = false;
    });

    vistaPrevia.addEventListener('error', () => {
        vistaPrevia.hidden = true;
    });
}

function valor(id) {
    return document.getElementById(id).value;
}

function marcar(id, mensaje) {
    const campo = document.getElementById(id);
    campo.setCustomValidity(mensaje);
    if (mensaje) {
        document.getElementById(`error-${id}`).textContent = mensaje;
    }
}

function validarCoherencia() {
    const precio = Number(valor('precio-base'));
    const incremento = Number(valor('incremento'));
    const inicio = new Date(valor('fecha-inicio'));
    const fin = new Date(valor('fecha-fin'));

    marcar('incremento', incremento > precio
        ? 'El incremento no puede superar al precio base.' : '');

    marcar('fecha-fin', fin <= inicio
        ? 'La fecha de fin tiene que ser posterior a la de inicio.'
        : fin <= new Date() ? 'La fecha de fin tiene que ser futura.' : '');
}

// Las claves del 400 son los nombres de las propiedades del DTO de C#.
const CAMPOS = {
    Titulo: 'titulo', Descripcion: 'descripcion', UrlImagen: 'url-imagen',
    CategoriaId: 'categoria', PrecioBase: 'precio-base',
    IncrementoMinimo: 'incremento', FechaInicio: 'fecha-inicio', FechaFin: 'fecha-fin'
};

function limpiarErrores(formulario) {
    formulario.querySelectorAll('.form-control, .form-select').forEach(campo => {
        campo.setCustomValidity('');
        campo.classList.remove('is-invalid');
    });
}

// setCustomValidity es necesario ademas de la clase: el formulario ya tiene
// was-validated, y sin invalidar la validez nativa Bootstrap lo sigue pintando de
// verde (el campo cumple sus reglas de HTML5, aunque el servidor lo haya rechazado).
function marcarCampoConError(idCampo, mensaje) {
    const campo = document.getElementById(idCampo);
    campo.setCustomValidity(mensaje);
    campo.classList.add('is-invalid');
    document.getElementById(`error-${idCampo}`).textContent = mensaje;
}

function mostrarErroresDeCampo(erroresPorCampo) {
    Object.entries(erroresPorCampo).forEach(([clave, mensajes]) => {
        const idCampo = CAMPOS[clave];
        const mensaje = Array.isArray(mensajes) ? mensajes[0] : mensajes;
        if (idCampo) {
            marcarCampoConError(idCampo, mensaje);
        } else {
            mostrarAlerta('alerta-publicar', mensaje, 'danger');
        }
    });
}

// El 422 es una regla de negocio, no un error de formato: el servidor solo manda un
// mensaje de texto, sin nombre de campo. Como ServicioDeSubastas.PublicarAsync solo
// puede rechazar por estos cuatro motivos fijos, alcanza con reconocer el mensaje para
// mostrarlo bajo el campo correcto en vez de un alert() generico (asi lo pide la
// consigna del Bloque 5, Tarea 5.6).
const MENSAJES_DE_NEGOCIO_A_CAMPO = [
    [/no existe una categoría/i, 'categoria'],
    [/incremento mínimo no puede superar/i, 'incremento'],
    [/fecha de fin debe ser posterior/i, 'fecha-fin']
];

function mostrarErrorDeNegocio(mensaje) {
    const coincidencia = MENSAJES_DE_NEGOCIO_A_CAMPO.find(([patron]) => patron.test(mensaje));
    if (coincidencia) {
        marcarCampoConError(coincidencia[1], mensaje);
    } else {
        mostrarAlerta('alerta-publicar', mensaje, 'danger');
    }
}

function configurarEnvio() {
    const formulario = document.getElementById('formulario-publicar');

    formulario.addEventListener('submit', async (evento) => {
        evento.preventDefault();
        ocultarAlerta('alerta-publicar');
        limpiarErrores(formulario);
        validarCoherencia();

        formulario.classList.add('was-validated');
        if (!formulario.checkValidity()) {
            return;
        }

        const datos = {
            titulo: valor('titulo').trim(),
            descripcion: valor('descripcion').trim(),
            urlImagen: valor('url-imagen').trim(),
            categoriaId: Number(valor('categoria')),
            precioBase: Number(valor('precio-base')),
            incrementoMinimo: Number(valor('incremento')),
            fechaInicio: new Date(valor('fecha-inicio')).toISOString(),
            fechaFin: new Date(valor('fecha-fin')).toISOString()
        };

        alternarCargando('boton-publicar', 'spinner-publicar', true);

        try {
            const creada = await api.post('/auctions', datos);
            window.location.href = `subasta.html?id=${creada.id}`;
        } catch (error) {
            if (error.status === 400 && error.erroresPorCampo) {
                mostrarErroresDeCampo(error.erroresPorCampo);
            } else if (error.status === 422) {
                mostrarErrorDeNegocio(error.message);
            } else {
                mostrarAlerta('alerta-publicar', error.message, 'danger');
            }
        } finally {
            alternarCargando('boton-publicar', 'spinner-publicar', false);
        }
    });
}
