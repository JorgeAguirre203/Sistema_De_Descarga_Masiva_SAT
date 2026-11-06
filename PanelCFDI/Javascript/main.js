import './navegacion.js';
import './barra-lateral.js';
import { cargar } from './fechas-rfc.js';
import { pintar, cargarDatos } from './cfdi.js';

// =========================================================
// ARRANQUE
// =========================================================

pintar();
cargar();
cargarDatos();

// Actualizar automáticamente cada 30 segundos
setInterval(cargarDatos, 30000);


// =========================================================
// ELIMINAR RFC
// =========================================================

const btnEliminar = document.getElementById('DropRFC');
const inputEliminar = document.getElementById('Eliminarfc');
const msg = document.getElementById('msg');

function mostrarMensaje(texto, tipo) {
    msg.textContent = texto;
    msg.dataset.tipo = tipo;   // el CSS usa data-tipo="ok" y "err"
}

btnEliminar.addEventListener('click', async () => {
    const rfc = inputEliminar.value.trim().toUpperCase();

    if (!rfc) {
        mostrarMensaje('Escribe el RFC a eliminar.', 'err');
        return;
    }

    if (!confirm('¿Eliminar el RFC ' + rfc + '? Se borrarán su fecha de búsqueda y los datos de su FIEL.')) {
        return;
    }

    try {
        const resp = await fetch('/Control/EliminarRfc', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'rfc=' + encodeURIComponent(rfc)
        });
        const datos = await resp.json();

        mostrarMensaje(datos.mensaje, datos.ok ? 'ok' : 'err');

        if (datos.ok) {
            inputEliminar.value = '';
            cargar();   // vuelve a pintar la tabla de RFC
        }
    } catch (e) {
        mostrarMensaje('No se pudo conectar con el servidor.', 'err');
    }
});