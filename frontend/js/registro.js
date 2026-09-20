// Pantalla de alta de usuario.

document.addEventListener('DOMContentLoaded', () => {
    const formulario = document.getElementById('formulario-registro');

    if (haySesion()) {
        window.location.replace('index.html');
        return;
    }

    formulario.addEventListener('submit', async (evento) => {
        evento.preventDefault();

        if (!formulario.checkValidity()) {
            formulario.classList.add('was-validated');
            return;
        }

        ocultarAlerta('alerta-registro');
        limpiarErroresDeCampo();
        alternarCargando('boton-registrar', 'spinner-registrar', true);

        const datos = {
            email: document.getElementById('email').value.trim(),
            password: document.getElementById('password').value,
            nombre: document.getElementById('nombre').value.trim(),
            seudonimo: document.getElementById('seudonimo').value.trim()
        };

        try {
            await api.post('/users', datos);

            // El alta no devuelve token: son dos operaciones distintas. Se inicia sesion
            // con las mismas credenciales para no obligarlo a escribirlas otra vez.
            const sesion = await api.post('/auth/sessions', {
                email: datos.email,
                password: datos.password
            });

            guardarSesion(sesion);
            window.location.href = 'index.html';
        } catch (error) {
            // El 400 trae el detalle campo por campo y se muestra debajo de cada campo.
            // El 409 (email o seudonimo ya usados) no es un problema de formato sino una
            // regla de negocio, y por eso llega como un mensaje suelto.
            const marcados = error.status === 400 && error.erroresPorCampo
                ? mostrarErroresDeCampo(error.erroresPorCampo)
                : 0;

            mostrarAlerta('alerta-registro',
                marcados > 0 ? 'Revisá los datos marcados.' : error.message);
        } finally {
            alternarCargando('boton-registrar', 'spinner-registrar', false);
        }
    });

    // Devuelve cuantos campos pudo marcar. Si no reconoce ninguna clave, el mensaje no
    // se pierde: la pantalla lo muestra en la alerta general.
    function mostrarErroresDeCampo(errores) {
        let marcados = 0;
        Object.entries(errores).forEach(([campo, mensajes]) => {
            // Las claves llegan con la capitalizacion del DTO de C# (Email, Password).
            const nombre = campo.toLowerCase();
            const entrada = document.getElementById(nombre);
            if (!entrada) return;

            entrada.classList.add('is-invalid');
            const destino = document.getElementById(`error-${nombre}`);
            if (destino) destino.textContent = mensajes[0];
            marcados++;
        });
        return marcados;
    }

    // El texto original de cada campo es el mensaje de la validacion del navegador.
    // Se guarda al cargar para poder restaurarlo: si se borrara, despues del primer
    // error del servidor el formulario se quedaria sin sus mensajes propios.
    formulario.querySelectorAll('.invalid-feedback').forEach(e => {
        e.dataset.mensajeOriginal = e.textContent;
    });

    function limpiarErroresDeCampo() {
        formulario.querySelectorAll('.is-invalid')
            .forEach(e => e.classList.remove('is-invalid'));
        formulario.querySelectorAll('.invalid-feedback')
            .forEach(e => { e.textContent = e.dataset.mensajeOriginal || ''; });
    }
});
