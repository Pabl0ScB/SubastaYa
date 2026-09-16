// Pantalla de alta de usuario.

document.addEventListener('DOMContentLoaded', () => {
    const formulario = document.getElementById('formulario-registro');
    const boton = document.getElementById('boton-registrar');
    const spinner = document.getElementById('spinner-registrar');
    const alerta = document.getElementById('alerta-registro');

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

        ocultarAlerta();
        limpiarErroresDeCampo();
        cargando(true);

        const datos = {
            email: document.getElementById('email').value.trim(),
            password: document.getElementById('password').value,
            nombre: document.getElementById('nombre').value.trim(),
            seudonimo: document.getElementById('seudonimo').value.trim()
        };

        try {
            await api.post('/users', datos);

            // El alta no devuelve token: son dos operaciones distintas. Se inicia
            // sesion con las mismas credenciales para no obligarlo a escribirlas otra vez.
            const sesion = await api.post('/auth/sessions', {
                email: datos.email,
                password: datos.password
            });

            guardarSesion(sesion);
            window.location.href = 'index.html';
        } catch (error) {
            if (error.status === 400 && error.erroresPorCampo) {
                // 400: el formato no paso las anotaciones del DTO. El cuerpo trae el
                // detalle campo por campo, asi que el mensaje va debajo del campo.
                mostrarErroresDeCampo(error.erroresPorCampo);
                mostrarAlerta('Revisa los datos marcados.', 'danger');
            } else if (error.status === 409) {
                // 409: el formato estaba bien, pero el email o el seudonimo ya existen.
                // Es una regla de negocio, no un error de formato, y por eso no es un 400.
                mostrarAlerta(error.message, 'danger');
            } else {
                mostrarAlerta(error.message, 'danger');
            }
        } finally {
            cargando(false);
        }
    });

    function mostrarErroresDeCampo(errores) {
        // Las claves llegan con la capitalizacion del DTO de C# (Email, Password).
        Object.entries(errores).forEach(([campo, mensajes]) => {
            const entrada = document.getElementById(campo.toLowerCase());
            if (!entrada) return;
            entrada.classList.add('is-invalid');
            const destino = document.getElementById(`error-${campo.toLowerCase()}`);
            if (destino) destino.textContent = mensajes[0];
        });
    }

    function limpiarErroresDeCampo() {
        formulario.querySelectorAll('.is-invalid').forEach(e => e.classList.remove('is-invalid'));
        formulario.querySelectorAll('.invalid-feedback').forEach(e => { e.textContent = ''; });
    }

    function cargando(activo) {
        boton.disabled = activo;
        spinner.hidden = !activo;
    }

    function mostrarAlerta(texto, tipo) {
        alerta.textContent = texto;
        alerta.className = `alert alert-${tipo}`;
        alerta.hidden = false;
    }

    function ocultarAlerta() {
        alerta.hidden = true;
    }
});
