import { $, obtener } from './utils.js';
import { esc } from './formato.js';

// FECHA DE BÚSQUEDA POR RFC (ControlConsultasPorRFC)
// Una fecha por RFC: se aplica a todos sus tipos
// (RECIBIDO, EMITIDO y SOLICITANTE).

const nombreTipoConsulta = { RECIBIDO: 'Recibidos', EMITIDO: 'Emitidos', SOLICITANTE: 'Solicitante' };

function avisoFecha(tipo, texto) {
    $('msg').dataset.tipo = tipo;
    $('msg').textContent = texto;
}

export async function cargar() {
    try {
        const r = await obtener('/Control/EstadoRfc');
        if (!r.ok) throw new Error(r.error);
        pintarFechas(r.filas || []);
    }
    catch (e) {
        avisoFecha('err', e.message);
    }
}

function pintarFechas(filas) {

    // Agrupar las filas por RFC
    const porRfc = {};
    filas.forEach(f => (porRfc[f.rfc] = porRfc[f.rfc] || []).push(f));

    const rfcs = Object.keys(porRfc).sort();

    $('tablaFechas').innerHTML = rfcs.map(rfc => {

        const tipos = porRfc[rfc];
        const fechas = [...new Set(tipos.map(t => t.fecha))];

        // Si todos los tipos tienen la misma fecha se muestra una sola;
        // si no, se muestra la de cada tipo.
        const detalle = fechas.length === 1
            ? `<span class="fecha-actual">${esc(fechas[0] || '—')}
                   <small>${tipos.map(t => esc(nombreTipoConsulta[t.tipo] || t.tipo)).join(', ')}</small></span>`
            : `<span class="fecha-actual">${tipos.map(t =>
                esc(t.fecha || '—') + ' <small>' + esc(nombreTipoConsulta[t.tipo] || t.tipo) + '</small>').join('<br>')}</span>`;

        return `
            <div class="fecha-fila" data-rfc="${esc(rfc)}">
                <span class="fecha-rfc">${esc(rfc)}</span>
                ${detalle}
                <input type="datetime-local" aria-label="Nueva fecha para ${esc(rfc)}">
                <button type="button" class="btn btn-principal" data-accion="fecha">Aplicar</button>
                <button type="button" class="btn btn-secundario" data-accion="hoy">Hoy</button>
            </div>`;
    }).join('');

    $('vacio-fechas').style.display = rfcs.length ? 'none' : 'block';

    // En Resumen se muestra la fecha más antigua de los recibidos
    const rec = filas.filter(f => f.tipo === 'RECIBIDO' && f.fecha).map(f => f.fecha).sort();
    $('desde').textContent = rec.length
        ? rec[0] + (rec.length > 1 ? ' (la más antigua de ' + rec.length + ' RFC)' : '')
        : '—';
}

async function fijarFecha(rfc, datos) {
    try {
        const resp = await fetch('/Control/FijarFechaRfc', {
            method: 'POST',
            body: new URLSearchParams({ rfc, ...datos })
        });

        if (!resp.ok) throw new Error('/Control/FijarFechaRfc respondió ' + resp.status);

        const r = await resp.json();

        avisoFecha(r.ok ? 'ok' : 'err',
            r.ok
                ? rfc + ': buscará desde ' + r.fecha + ' (' + r.filas + (r.filas === 1 ? ' tipo actualizado)' : ' tipos actualizados)')
                : r.error);

        if (r.ok) cargar();
    }
    catch (e) {
        avisoFecha('err', e.message);
    }
}

$('tablaFechas').addEventListener('click', e => {

    const b = e.target.closest('button[data-accion]');
    if (!b) return;

    const fila = b.closest('.fecha-fila');
    const rfc = fila.dataset.rfc;

    if (b.dataset.accion === 'hoy') {
        if (confirm(rfc + ': se buscará solo desde hoy en adelante en todos sus tipos. ¿Continuar?')) {
            fijarFecha(rfc, { modo: 'actual' });
        }
        return;
    }

    const v = fila.querySelector('input').value;
    if (!v) return avisoFecha('err', 'Elige una fecha para ' + rfc + '.');

    fijarFecha(rfc, { modo: 'fecha', fecha: v });
});

$('btnAgregarFecha').onclick = () => {

    const rfc = $('nuevoRfcFecha').value.trim().toUpperCase();
    const v = $('nuevaFecha').value;

    if (!/^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$/.test(rfc)) return avisoFecha('err', 'Escribe un RFC válido.');
    if (!v) return avisoFecha('err', 'Elige desde qué fecha buscar.');

    fijarFecha(rfc, { modo: 'fecha', fecha: v });
};

// GUARDAR RFC

async function guardarRfc(url, tipo, idInput) {

    const rfc = $(idInput).value.trim().toUpperCase();

    if (!rfc) {
        $('msgRfc').textContent = 'Escribe un RFC.';
        return;
    }

    try {

        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ tipo, rfc })
        });

        if (!resp.ok) {
            throw new Error(url + ' respondió ' + resp.status);
        }

        const data = await resp.json();

        $('msgRfc').dataset.tipo = data.ok ? 'ok' : 'err';
        $('msgRfc').textContent = data.ok
            ? 'RFC ' + tipo.toLowerCase() + ' actualizado'
            : data.error;

    }
    catch (e) {
        $('msgRfc').dataset.tipo = 'err';
        $('msgRfc').textContent = e.message;
    }
}