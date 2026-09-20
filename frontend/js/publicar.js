// Mismos limites que ServicioDeSubastas.PublicarAsync (DuracionMaxima y
// ToleranciaInicioPasado). El navegador no le puede preguntar el limite al servidor
// antes de dibujar el calendario, asi que el numero queda escrito en los dos lados: si
// se cambia uno, hay que cambiar el otro.
const TOLERANCIA_INICIO_PASADO_MS = 5 * 60 * 1000;
const DURACION_MAXIMA_MS = 365 * 24 * 60 * 60 * 1000;

document.addEventListener('DOMContentLoaded', () => {
    cargarCategorias();
    configurarVistaPrevia();
    configurarLimitesDeFecha();
    limitarADosDecimales(document.getElementById('precio-base'));
    limitarADosDecimales(document.getElementById('incremento'));
    configurarEnvio();
});

// El calendario en gris fuera de rango es una comodidad del navegador: la regla real
// la aplica el servidor. Sin esto, escribir un ano de mas de cuatro digitos deja el
// campo invalido pero no lo avisa hasta enviar.
function configurarLimitesDeFecha() {
    const inicioMinimo = new Date(Date.now() - TOLERANCIA_INICIO_PASADO_MS);
    const finMaximo = new Date(Date.now() + DURACION_MAXIMA_MS);

    document.getElementById('fecha-inicio').min = paraInputFecha(inicioMinimo);
    document.getElementById('fecha-fin').max = paraInputFecha(finMaximo);
}

// datetime-local espera "YYYY-MM-DDTHH:mm" en hora local, sin zona.
function paraInputFecha(fecha) {
    const dos = n => String(n).padStart(2, '0');
    return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}-${dos(fecha.getDate())}` +
        `T${dos(fecha.getHours())}:${dos(fecha.getMinutes())}`;
}

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

    const inicioMinimo = new Date(Date.now() - TOLERANCIA_INICIO_PASADO_MS);
    marcar('fecha-inicio', inicio < inicioMinimo
        ? 'La fecha de inicio no puede estar en el pasado.' : '');

    const finMaximo = new Date(Date.now() + DURACION_MAXIMA_MS);
    marcar('fecha-fin', fin <= inicio
        ? 'La fecha de fin tiene que ser posterior a la de inicio.'
        : fin <= new Date() ? 'La fecha de fin tiene que ser futura.'
        : fin > finMaximo
            ? `La fecha de fin no puede superar el ${finMaximo.toLocaleDateString('es-AR')}.`
            : '');
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
// mostrarlo bajo el campo correcto en vez de un alert() generico.
const MENSAJES_DE_NEGOCIO_A_CAMPO = [
    [/no existe una categoría/i, 'categoria'],
    [/incremento mínimo no puede superar/i, 'incremento'],
    [/fecha de inicio no puede estar en el pasado/i, 'fecha-inicio'],
    [/fecha de fin debe ser posterior/i, 'fecha-fin'],
    [/subasta no puede terminar más allá/i, 'fecha-fin']
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
            // Sin esto el envio se corta en silencio: el aviso rojo queda arriba de
            // todo y el boton al final de un formulario largo, como si la pagina se
            // hubiera colgado.
            formulario.querySelector(':invalid')?.focus();
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
