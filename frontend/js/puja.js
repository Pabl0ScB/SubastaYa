// Formulario de oferta del detalle de la subasta.
//
// Todo vive dentro de esta funcion: los scripts de la pantalla comparten el ambito
// global y sala.js ya usa nombres como subastaId, conexion o recargarEstado. Declarar
// cualquiera de ellos otra vez rompe la pagina entera, no solo el formulario.
(() => {
    'use strict';

    let detalle = null;
    let montoMinimo = 0;
    let situacionDibujada = null;
    // El usuario escribio en el campo: desde ese momento su monto no se pisa solo.
    let montoTocado = false;
    // Su oferta acaba de entrar: lo unico que cambia es el texto de la confirmacion.
    let recienOferto = false;
    // Estado con el que se dibujo la vez anterior, para detectar el arranque en vivo.
    let estadoAnterior = null;

    // El detalle se redibuja cada vez que llega una oferta, se reconecta la sala o el
    // sistema cierra la subasta. Por eso aca no se dibuja de nuevo si la situacion no
    // cambio: redibujar borraria lo que el usuario esta escribiendo y le sacaria el foco.
    document.addEventListener('subasta:cargada', evento => {
        const arranco = estadoAnterior === 'Programada' && evento.detail.estado === 'Activa';
        estadoAnterior = evento.detail.estado;

        detalle = evento.detail;
        montoMinimo = detalle.pujaActual + detalle.incrementoMinimo;
        dibujar();

        // La subasta arranco mientras la pantalla estaba abierta: el estado ya se
        // redibujo solo, pero el cambio es facil de no ver si el usuario esta mirando
        // otra parte de la pagina.
        if (arranco) avisarInicio(situacionDibujada);
    });

    // Aviso de sala.js cuando entra una oferta nueva sin recargar el detalle: alcanza
    // con corregir el minimo y, si cambio quien lidera, la situacion.
    document.addEventListener('sala:actualizada', evento => {
        if (!detalle) return;
        detalle = { ...detalle, ...evento.detail };
        montoMinimo = evento.detail.montoMinimo;
        dibujar();
    });

    // Entre que vence la fecha de fin y el proceso que cierra las subastas la procesa, la
    // API la sigue informando como Activa. Ofertar ahi devuelve 422: mejor no ofrecerlo.
    // Mismo criterio que subasta.js y catalogo.js.
    function puedeRecibirOfertas() {
        return detalle.estado === 'Activa' && new Date(detalle.fechaFin) > new Date();
    }

    function situacionActual() {
        if (!puedeRecibirOfertas()) return 'cerrada';

        const usuario = obtenerUsuario();
        if (!usuario) return 'anonimo';
        if (usuario.seudonimo === detalle.seudonimoVendedor) return 'vendedor';
        if (usuario.seudonimo === detalle.seudonimoLider) return 'lider';
        return 'puede';
    }

    function dibujar() {
        const situacion = situacionActual();

        if (situacion === situacionDibujada) {
            if (situacion === 'puede') actualizarMinimo();
            return;
        }

        situacionDibujada = situacion;
        montoTocado = false;

        const contenedor = document.getElementById('formulario-puja');
        contenedor.replaceChildren(construir(situacion));
    }

    function construir(situacion) {
        if (situacion === 'cerrada') return document.createDocumentFragment();
        if (situacion === 'anonimo') return enlaceDeIngreso();
        if (situacion === 'vendedor') {
            return aviso('Es tu subasta: no podés ofertar en lo que publicaste.');
        }
        if (situacion === 'lider') {
            const texto = recienOferto
                ? 'Oferta registrada: vas ganando. Esperá a que alguien te supere para volver a ofertar.'
                : 'Vas ganando. Esperá a que alguien te supere para volver a ofertar.';
            recienOferto = false;
            return aviso(texto, 'success');
        }
        return formulario();
    }

    function aviso(texto, tipo = 'secondary') {
        const bloque = document.createElement('p');
        bloque.className = `alert alert-${tipo} py-2 mb-2`;
        bloque.textContent = texto;
        return bloque;
    }

    function enlaceDeIngreso() {
        const bloque = document.createElement('div');
        bloque.className = 'mb-2';

        const texto = document.createElement('p');
        texto.className = 'mb-2';
        texto.textContent = 'Para ofertar necesitás iniciar sesión.';

        const enlace = document.createElement('a');
        enlace.className = 'btn btn-primary';
        // Vuelve a esta subasta despues de ingresar, en vez de dejarlo en el catalogo.
        // El nombre del parametro y el formato sin ruta son los que espera login.js:
        // valida el destino contra "archivo.html" para no redirigir a cualquier lado.
        enlace.href = `login.html?volverA=${encodeURIComponent('subasta.html' + location.search)}`;
        enlace.textContent = 'Ingresar para ofertar';

        bloque.append(texto, enlace);
        return bloque;
    }

    function formulario() {
        const bloque = document.createElement('div');

        const etiqueta = document.createElement('label');
        etiqueta.className = 'form-label';
        etiqueta.htmlFor = 'monto-puja';
        etiqueta.textContent = 'Tu oferta';

        const grupo = document.createElement('div');
        grupo.className = 'input-group';

        const simbolo = document.createElement('span');
        simbolo.className = 'input-group-text';
        simbolo.textContent = '$';

        const campo = document.createElement('input');
        campo.type = 'number';
        campo.className = 'form-control';
        campo.id = 'monto-puja';
        campo.step = '0.01';
        campo.min = String(montoMinimo);
        // Arranca con el minimo ya escrito: es un clic en vez de averiguar el numero, y
        // hace que reintentar despues de un conflicto sea inmediato.
        campo.value = String(montoMinimo);
        campo.addEventListener('input', () => { montoTocado = true; });
        limitarADosDecimales(campo);

        const boton = document.createElement('button');
        boton.type = 'submit';
        boton.className = 'btn btn-primary';
        boton.id = 'boton-ofertar';

        const spinner = document.createElement('span');
        spinner.className = 'spinner-border spinner-border-sm me-1';
        spinner.id = 'spinner-ofertar';
        spinner.hidden = true;

        boton.append(spinner, document.createTextNode('Ofertar'));
        grupo.append(simbolo, campo, boton);

        const ayuda = document.createElement('p');
        ayuda.className = 'form-text mb-2';
        ayuda.id = 'ayuda-puja';
        ayuda.textContent = `Mínimo: ${formatearPesos(montoMinimo)}`;

        const error = document.createElement('p');
        error.className = 'text-danger small mb-2';
        error.id = 'error-puja';
        error.hidden = true;

        // Contenedor del dialogo de conflicto: vacio hasta que la API devuelve un 409.
        const conflicto = document.createElement('div');
        conflicto.id = 'conflicto-puja';

        const formulario = document.createElement('form');
        formulario.noValidate = true;
        formulario.addEventListener('submit', evento => {
            evento.preventDefault();
            ofertar(campo.value);
        });
        formulario.append(etiqueta, grupo, ayuda, error, conflicto);

        bloque.append(formulario);
        return bloque;
    }

    // El minimo sube con cada oferta ajena. El monto del usuario no se pisa si ya lo
    // toco: se le avisa que el minimo subio y decide el. Pisarlo mientras escribe seria
    // cambiarle el numero por atras justo antes de que apriete el boton.
    function actualizarMinimo() {
        const campo = document.getElementById('monto-puja');
        const ayuda = document.getElementById('ayuda-puja');
        if (!campo || !ayuda) return;

        campo.min = String(montoMinimo);
        ayuda.textContent = `Mínimo: ${formatearPesos(montoMinimo)}`;

        if (!montoTocado) {
            campo.value = String(montoMinimo);
            return;
        }

        if (Number(campo.value) < montoMinimo) {
            mostrarError(`Otra oferta subió el mínimo a ${formatearPesos(montoMinimo)}.`);
        }
    }

    // mostrarAvisoFlotante vive en ui.js: la comparte con el aviso de extension de
    // tiempo de sala.js, asi los dos se ven como parte del mismo sistema.
    function avisarInicio(situacion) {
        const textos = {
            puede: 'La subasta comenzó: ya podés ofertar.',
            vendedor: 'Tu subasta comenzó: ya puede recibir ofertas.',
            anonimo: 'La subasta comenzó. Ingresá para ofertar.'
        };

        mostrarAvisoFlotante(textos[situacion] ?? 'La subasta comenzó.', {
            tipo: 'success',
            icono: '🔔'
        });
    }

    function mostrarError(texto) {
        const error = document.getElementById('error-puja');
        if (!error) return;
        error.textContent = texto;
        error.hidden = false;
    }

    function limpiarMensajes() {
        const error = document.getElementById('error-puja');
        if (error) error.hidden = true;
        document.getElementById('conflicto-puja')?.replaceChildren();
    }

    async function ofertar(valor) {
        limpiarMensajes();

        const monto = Number(valor);
        if (!valor || Number.isNaN(monto)) {
            mostrarError('Escribí cuánto querés ofertar.');
            return;
        }
        // Comodidad, no defensa: quien decide de verdad es el servidor, que valida el
        // monto contra la puja actual en el mismo instante en que la registra.
        if (monto < montoMinimo) {
            mostrarError(`La oferta debe ser de al menos ${formatearPesos(montoMinimo)}.`);
            return;
        }

        alternarCargando('boton-ofertar', 'spinner-ofertar', true);

        // Se marca antes de mandar, no despues: la oferta propia puede volver por el
        // canal en vivo antes que la respuesta del POST, y en ese caso la pantalla ya se
        // redibujo sin la confirmacion.
        recienOferto = true;

        try {
            const puja = await api.post(`/auctions/${detalle.id}/bids`, { monto });

            // La sala es el unico punto por donde entra una oferta nueva, venga del canal
            // en vivo o de esta respuesta. Asi la pantalla se actualiza sin esperar el
            // aviso, y sin contar dos veces la misma oferta.
            registrarPujaEnSala(puja);
            montoTocado = false;
        } catch (error) {
            // La oferta no entro: si mas adelante aparece como lider por otra via, no
            // corresponde mostrarle la confirmacion de esta.
            recienOferto = false;
            manejarError(error);
        } finally {
            // En el finally y no al final del try: si el boton queda deshabilitado
            // despues de un error, el usuario no puede volver a ofertar en toda la
            // subasta y la unica salida es recargar.
            alternarCargando('boton-ofertar', 'spinner-ofertar', false);
        }
    }

    function manejarError(error) {
        // 409: no es un error del usuario. Otra oferta entro mientras preparaba la suya,
        // asi que en vez de un cartel rojo se le ofrece el reintento con el monto nuevo.
        if (error.status === 409 && error.cuerpo && error.cuerpo.montoMinimo) {
            mostrarConflicto(error.cuerpo);
            return;
        }

        // 400: el cuerpo trae los errores por campo del DTO (monto con mas de dos
        // decimales, fuera de rango). El unico campo del formulario es el monto.
        if (error.status === 400 && error.erroresPorCampo) {
            const mensajes = Object.values(error.erroresPorCampo)[0];
            mostrarError(Array.isArray(mensajes) ? mensajes[0] : String(mensajes));
            return;
        }

        // 403 (vendedor), 422 (saldo, monto, subasta vencida) y 404: el servidor ya manda
        // el mensaje que corresponde mostrar, no hace falta reescribirlo.
        mostrarError(error.message);
    }

    function mostrarConflicto(conflicto) {
        const contenedor = document.getElementById('conflicto-puja');
        if (!contenedor) return;

        montoMinimo = conflicto.montoMinimo;

        const bloque = document.createElement('div');
        bloque.className = 'alert alert-warning py-2';

        const texto = document.createElement('p');
        texto.className = 'mb-2';
        texto.textContent =
            `Alguien ofertó antes que vos. La oferta actual es de ${formatearPesos(conflicto.pujaActual)}.`;

        const reintentar = document.createElement('button');
        reintentar.type = 'button';
        reintentar.className = 'btn btn-primary btn-sm me-2';
        reintentar.textContent = `Ofertar ${formatearPesos(conflicto.montoMinimo)}`;
        reintentar.addEventListener('click', () => ofertar(String(conflicto.montoMinimo)));

        const cancelar = document.createElement('button');
        cancelar.type = 'button';
        cancelar.className = 'btn btn-outline-secondary btn-sm';
        cancelar.textContent = 'Cancelar';
        cancelar.addEventListener('click', () => limpiarMensajes());

        bloque.append(texto, reintentar, cancelar);
        contenedor.replaceChildren(bloque);

        // El campo queda con el monto nuevo para quien prefiera escribir otro numero.
        const campo = document.getElementById('monto-puja');
        if (campo) {
            campo.min = String(conflicto.montoMinimo);
            campo.value = String(conflicto.montoMinimo);
            montoTocado = false;
        }
        const ayuda = document.getElementById('ayuda-puja');
        if (ayuda) ayuda.textContent = `Mínimo: ${formatearPesos(conflicto.montoMinimo)}`;
    }
})();
