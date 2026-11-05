import { $ } from './utils.js';

// BARRA LATERAL: CAMBIAR DE VISTA

const vistas = ['resumen', 'comprobantes', 'solicitudes', 'config'];

function mostrar(v) {

    if (!vistas.includes(v)) v = 'resumen';

    vistas.forEach(x => {
        $('vista-' + x).hidden = x !== v;
    });

    document.querySelectorAll('.nav-item').forEach(b => {
        b.classList.toggle('activo', b.dataset.vista === v);
        b.setAttribute('aria-current', b.dataset.vista === v ? 'page' : 'false');
    });

    if (location.hash !== '#' + v) history.replaceState(null, '', '#' + v);
    window.scrollTo(0, 0);
}

document.querySelectorAll('[data-vista]').forEach(b =>
    b.addEventListener('click', () => mostrar(b.dataset.vista)));

document.querySelectorAll('[data-ir]').forEach(b =>
    b.addEventListener('click', () => mostrar(b.dataset.ir)));

mostrar(location.hash.slice(1));