// Pantalla de ingreso.

document.addEventListener('DOMContentLoaded', () => {
    const formulario = document.getElementById('formulario-login');
    const parametros = new URLSearchParams(window.location.search);

    // Si llego expulsado por un token vencido, se le explica en vez de dejarlo
    // adivinar por que volvio al login.
    if (parametros.get('vencida')) {
        mostrarAlerta('alerta-login', 'Tu sesión expiró. Volvé a ingresar.', 'warning');
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

        ocultarAlerta('alerta-login');
        alternarCargando('boton-ingresar', 'spinner-ingresar', true);

        try {
            const sesion = await api.post('/auth/sessions', {
                email: document.getElementById('email').value.trim(),
                password: document.getElementById('password').value
            });

            guardarSesion(sesion);
            window.location.href = destinoPosterior();
        } catch (error) {
            // Mismo mensaje para email inexistente y para contrasena incorrecta, igual
            // que el backend: distinguirlos permitiria averiguar que cuentas existen.
            const texto = error.status === 401
                ? 'Email o contraseña incorrectos.'
                : error.message;
            mostrarAlerta('alerta-login', texto);
        } finally {
            // En el finally y no al final del try: si la llamada falla, el boton tiene
            // que volver a habilitarse igual o la pantalla queda trabada.
            alternarCargando('boton-ingresar', 'spinner-ingresar', false);
        }
    });

    function destinoPosterior() {
        const volverA = parametros.get('volverA');
        // Solo se acepta un nombre de archivo del propio sitio. Redirigir a lo que venga
        // en la URL permitiria mandar a un usuario recien logueado a un sitio externo.
        return volverA && /^[a-z0-9-]+\.html(\?.*)?$/i.test(volverA) ? volverA : 'index.html';
    }
});
