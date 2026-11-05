import { $, obtener } from './utils.js';
import { esc, dinero, fechaMx, claseTipo, claseEstado } from './formato.js';

// DATOS DEL PANEL

let cfdis = [];

/*
    Ejemplo:

    {
        fecha: '2026-10-01T10:00:00',
        emisor: '',
        rfc: '',
        uuid: '',
        tipo: 'Ingreso',
        total: 0,
        xml: '',
        pdf: ''
    }
*/


let solicitudes = [];

/*
    Ejemplo:

    {
        id: '',
        desde: '2026-09-01 00:00',
        hasta: '2026-09-30 00:00',
        estado: 'En proceso'
    }
*/


let info = {};

/*
    Ejemplo:

    {
        rfc: '',
        estado: '',
        ultima: '',
        total: 0
    }
*/

// CARGAR DATOS

export async function cargarDatos() {

    try {

        const [
            c,
            s,
            i
        ] = await Promise.all([

            obtener('/Cfdi/Lista'),

            obtener('/Cfdi/Solicitudes'),

            obtener('/Cfdi/Info')

        ]);


        cfdis = c;

        solicitudes = s;

        info = i;


        pintar();

    }
    catch (e) {

        $('estado').textContent =
            'Error cargando datos: ' +
            e.message;

    }
}

// PINTAR INFORMACIÓN

export function pintar() {

    // INFORMACIÓN GENERAL

    const receptores = [...new Set(cfdis.map(c => c.receptor).filter(Boolean))].sort();

    if ($('rfc')) {
        $('rfc').textContent =
            receptores.length > 1
                ? receptores.length + ' RFC'
                : (receptores[0] || info.rfc || '');
    }

    $('estado').textContent =
        info.estado ||
        'Sin información del proceso';


    $('r-total').textContent =
        (
            info.total ??
            cfdis.length
        ).toLocaleString('es-MX');


    $('r-proceso').textContent =
        solicitudes.filter(
            s =>
                claseEstado(s.estado) === 'pend'
        ).length;


    $('r-ultima').textContent =
        info.ultima || '—';

    $('nav-cfdi').textContent =
        cfdis.length || '';

    $('nav-sol').textContent =
        $('r-proceso').textContent === '0'
            ? ''
            : $('r-proceso').textContent;


    // MESES DISPONIBLES

    const meses = [
        ...new Set(
            cfdis
                .map(c => c.fecha.slice(0, 7))
        )
    ]
        .sort()
        .reverse();


    const actual =
        $('mes').value;


    $('mes').innerHTML =
        '<option value="">Todos los meses</option>' +

        meses
            .map(
                m =>
                    `<option value="${m}">${m}</option>`
            )
            .join('');


    $('mes').value =
        actual;


    const rfcActual = $('rfcFiltro').value;

    $('rfcFiltro').innerHTML =
        '<option value="">Todos los RFC</option>' +
        receptores.map(r => `<option value="${esc(r)}">${esc(r)}</option>`).join('');

    $('rfcFiltro').value = receptores.includes(rfcActual) ? rfcActual : '';
    $('rfcFiltro').hidden = receptores.length < 2;

    // TABLA DE CFDI

    $('filas').innerHTML =
        cfdis
            .map(
                c => `

                <tr
                    data-mes="${esc(c.fecha.slice(0, 7))}"
                    data-tipo="${esc(c.tipo)}"
                    data-receptor="${esc(c.receptor)}">

                    <td>
                        ${fechaMx(c.fecha)}
                    </td>
                    <td class="uuid">
                        ${esc(c.receptor)}
                    </td>


                    <td class="emisor">
                        <div class="emisor-celda">
                            <span class="avatar" aria-hidden="true">${esc((c.emisor || '?').trim().charAt(0).toUpperCase())}</span>
                            <span>
                                ${esc(c.emisor)}
                                <small>${esc(c.rfc)}</small>
                            </span>
                        </div>
                    </td>


                    <td class="uuid">
                        ${esc(c.uuid)}
                    </td>


                    <td>

                        <span
                            class="tag ${claseTipo(c.tipo)}">

                            ${esc(c.tipo)}

                        </span>

                    </td>


                    <td class="num">
                        ${dinero(c.total)}
                    </td>


                    <td class="acc">

                        ${c.xml
                        ? `<a
                                    href="${esc(c.xml)}"
                                    target="_blank"
                                    rel="noopener">
                                    XML
                                   </a>`
                        : ''
                    }


                        ${c.pdf
                        ? `<a
                                    href="${esc(c.pdf)}"
                                    target="_blank"
                                    rel="noopener">
                                    PDF
                                   </a>`
                        : ''
                    }

                    </td>

                </tr>

            `
            )
            .join('');

    // SOLICITUDES SAT

    $('solicitudes').innerHTML =
        solicitudes
            .map(
                s => `

                <li class="sol-${claseEstado(s.estado)}">

                    <span>

                        <strong>
                            Solicitud ${esc(s.id)}
                        </strong>

                        <small>
                            del
                            ${fechaMx(s.desde)}
                            al
                            ${fechaMx(s.hasta)}
                        </small>

                    </span>


                    <span
                        class="tag ${claseEstado(s.estado)}">

                        ${esc(s.estado)}

                    </span>

                </li>

            `
            )
            .join('');

    // MENSAJE DE SOLICITUDES VACÍAS

    $('vacio-sol').style.display =
        solicitudes.length
            ? 'none'
            : 'block';


    // APLICAR FILTROS

    filtrar();

}

// FILTRAR CFDI

function filtrar() {

    const t =
        $('buscar')
            .value
            .trim()
            .toLowerCase();


    const filas =
        [
            ...document.querySelectorAll(
                '#filas tr'
            )
        ];


    let n = 0;


    filas.forEach(f => {

        const ok =

            f.textContent
                .toLowerCase()
                .includes(t)

            &&

            (
                !$('mes').value ||
                f.dataset.mes ===
                $('mes').value
            )

            &&
            (
                !$('tipo').value ||
                f.dataset.tipo ===
                $('tipo').value
            )

            &&

            (
                !$('rfcFiltro').value ||
                f.dataset.receptor ===
                $('rfcFiltro').value
            );


        f.hidden = !ok;


        if (ok) {
            n++;
        }

    });


    $('vacio').textContent =

        cfdis.length

            ? 'No hay comprobantes con esos filtros. Cambia la búsqueda o el mes.'

            : 'Aún no hay comprobantes descargados.';


    $('vacio').style.display =
        n
            ? 'none'
            : 'block';

    $('contador').textContent =
        n === cfdis.length
            ? n + (n === 1 ? ' comprobante' : ' comprobantes')
            : n + ' de ' + cfdis.length;

}

// EVENTOS DE FILTROS

['buscar', 'mes', 'tipo', 'rfcFiltro']
    .forEach(
        id =>
            $(id).addEventListener(
                'input',
                filtrar
            )
    );