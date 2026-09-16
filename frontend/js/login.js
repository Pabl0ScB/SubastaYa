// Pantalla de ingreso.

document.addEventListener('DOMContentLoaded', () => {
    const formulario = document.getElementById('formulario-login');
    const boton = document.getElementById('boton-ingresar');
    const spinner = document.getElementById('spinner-ingresar');
    const alerta = document.getElementById('alerta-login');

    const parametros = new URLSearchParams(window.location.search);

    // Si llego expulsado por un token vencido, se le explica en vez de dejarlo
    // adivinar por que volvio al login.
    if (parametros.get('vencida')) {
        mostrarAlerta('Tu sesion expiro. Volve a ingresar.', 'warning');
    }

    // Ya logueado no tiene sentido ver este formulario.
    if (haySesion()) {
        window.location.replace(destinoPosterior());
        return;
    }

    formulario.addEventListener('submit', async (evento) => {
        evento.preventDefault();

        // Validacion del navegador antes de gastar una llamada. La validacion que
        // manda sigue siendo la del backend: esta se puede saltear desde la consola.
        if (!formulario.checkValidity()) {
            formulario.classList.add('was-validated');
            return;
        }

        ocultarAlerta();
        cargando(true);

        try {
            const sesion = await api.post('/auth/sessions', {
                email: document.getElementById('email').value.trim(),
                password: document.getElementById('password').value
            });

            guardarSesion(sesion);
            window.location.href = destinoPosterior();
        } catch (error) {
            if (error.status === 401) {
                // Mismo mensaje que da el backend para email inexistente y para
                // contrasena incorrecta: distinguirlos revelaria que cuentas existen.
                mostrarAlerta('Email o contrasena incorrectos.', 'danger');
            } else {
                mostrarAlerta(error.message, 'danger');
            }
        } finally {
            // En el finally y no al final del try: si la llamada falla, el boton
            // tiene que volver a habilitarse igual o la pantalla queda trabada.
            cargando(false);
        }
    });

    function destinoPosterior() {
        const volverA = parametros.get('volverA');
        // Solo se acepta un nombre de archivo del propio sitio. Redirigir a lo que
        // venga en la URL permitiria mandar a un usuario logueado a un sitio externo.
        return volverA && /^[a-z0-9-]+\.html(\?.*)?$/i.test(volverA) ? volverA : 'index.html';
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
