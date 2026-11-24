import { $, obtener } from './utils.js';
import { esc } from './formato.js';

// PESTAÑAS DE CONFIGURACIÓN (Fecha de búsqueda | Intervalos | FIEL)
const tabs = [...document.querySelectorAll('.tabs .tab')];

function abrirTab(tab) {
    tabs.forEach(t => {
        const activa = t === tab;
        t.classList.toggle('activa', activa);
        t.setAttribute('aria-selected', activa ? 'true' : 'false');
        t.tabIndex = activa ? 0 : -1;
        $(t.dataset.panel).hidden = !activa;
    });

    if (tab.dataset.panel === 'panel-fiel') cargarFiel();
}

tabs.forEach(t => t.addEventListener('click', () => abrirTab(t)));

// FIEL
function aviso(tipo, texto) {
    $('msgFiel').dataset.tipo = tipo;
    $('msgFiel').textContent = texto;
}

async function cargarFiel() {
    try {
        const r = await obtener('/Control/EstadoFiel');
        if (!r.ok) throw new Error(r.error);

        $('fielCarpeta').textContent = r.carpeta;

        const actual = $('fielRfc').value;
        $('fielRfc').innerHTML = '<option value="">Elige el RFC…</option>' +
            r.filas.map(f => `<option value="${esc(f.rfc)}">${esc(f.rfc)}</option>`).join('');
        $('fielRfc').value = actual;

        const tabla = $('tablaFiel');
        if (tabla) tabla.innerHTML = r.filas.map(f => {
            const ok = f.tieneClave && f.archivosOk;
            return `
                <div class="fiel-fila">
                    <span class="fecha-rfc">${esc(f.rfc)}</span>
                    <small>${f.cer ? esc(f.cer) + ' · ' + esc(f.key) : 'Sin archivos registrados'}</small>
                    <span class="fiel-estado" data-ok="${ok}">${ok ? 'FIEL lista' : 'Falta FIEL'}</span>
                </div>`;
        }).join('');
    }
    catch (e) {
        aviso('err', e.message);
    }
}

$('btnSubirFiel').onclick = async () => {

    const rfc = $('fielRfc').value;
    const cer = $('fielCer').files[0];
    const key = $('fielKey').files[0];
    const clave = $('fielClave').value;

    if (!rfc) return aviso('err', 'Elige el RFC.');
    if (!cer) return aviso('err', 'Selecciona el archivo .cer.');
    if (!key) return aviso('err', 'Selecciona el archivo .key.');
    if (!clave) return aviso('err', 'Escribe la contraseña de la clave privada.');

    const datos = new FormData();
    datos.append('rfc', rfc);
    datos.append('clave', clave);
    datos.append('cer', cer);
    datos.append('key', key);

    $('btnSubirFiel').disabled = true;

    try {
        const resp = await fetch('/Control/SubirFiel', { method: 'POST', body: datos });
        if (!resp.ok) throw new Error('/Control/SubirFiel respondió ' + resp.status);

        const r = await resp.json();

        if (!r.ok) return aviso('err', r.error);

        aviso('ok',
            rfc + ': FIEL guardada en ' + r.carpeta + (r.carpetaCreada ? ' (la carpeta se creó)' : '') +
            '. Vence el ' + r.vence + '.' + (r.aviso ? ' ' + r.aviso : ''));

        $('fielCer').value = '';
        $('fielKey').value = '';
        $('fielClave').value = '';

        cargarFiel();
    }
    catch (e) {
        aviso('err', e.message);
    }
    finally {
        $('btnSubirFiel').disabled = false;
    }
};

// Si se agrega o elimina un RFC en la otra pestaña, la lista de RFC se refresca al abrir esta.
export { cargarFiel };