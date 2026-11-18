import './navegacion.js';
import './barra-lateral.js';
import './tema.js';
import './nomina.js';
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


// =========================================================
// CÓMO FUNCIONA · demo interactiva del Resumen
// - Arranca cuando la sección entra en pantalla
// - Avanza sola; se detiene al pasar el mouse, al enfocarla,
//   al cambiar de pestaña o al pulsar "Pausar"
// - Click (o flechas del teclado) en un paso para saltar a él
// =========================================================

const raiz = document.getElementById('comoFunciona');

if (raiz) {

    const pasos = [...raiz.querySelectorAll('.cf-paso')];
    const escenas = [...raiz.querySelectorAll('.cf-escena')];
    const titulo = document.getElementById('cfTituloVentana');
    const btnPausa = document.getElementById('cfPausa');
    const btnRepetir = document.getElementById('cfRepetir');
    const contador = document.getElementById('cfContador');

    const TITULOS = ['Solicitud al SAT', 'Estado de las solicitudes', 'Descarga de paquetes', 'Comprobantes'];
    const sinMovimiento = matchMedia('(prefers-reduced-motion: reduce)').matches;

    let actual = -1;
    let viendose = false;
    let encima = false;
    let pausadoPorUsuario = false;
    let rafContador = 0;

    // ---------- Pausa / reanudar ----------

    function actualizarPausa() {
        const pausa = pausadoPorUsuario || encima || !viendose || document.hidden;
        raiz.classList.toggle('cf-pausa', pausa);
    }

    // ---------- Mostrar un paso ----------

    function mostrar(i) {

        i = (i + pasos.length) % pasos.length;

        pasos.forEach(p => p.setAttribute('aria-selected', 'false'));
        escenas.forEach(e => e.classList.remove('activa'));
        cancelAnimationFrame(rafContador);
        if (contador) contador.textContent = '0';

        void raiz.offsetWidth;   // reinicia las animaciones del paso

        pasos[i].setAttribute('aria-selected', 'true');
        escenas[i].classList.add('activa');
        if (titulo) titulo.textContent = TITULOS[i];
        actual = i;

        if (i === 2) animarContador();
    }

    // El número final sale del resumen real si ya hay CFDI descargados
    function animarContador() {

        if (!contador) return;

        const real = parseInt((document.getElementById('r-total')?.textContent || '').replace(/\D/g, ''), 10);
        const meta = real > 0 ? real : 24;

        if (sinMovimiento) {
            contador.textContent = meta.toLocaleString('es-MX');
            return;
        }

        const inicio = performance.now() + 2400;   // espera a que aparezcan los XML
        const duracion = 1400;

        const paso = ahora => {
            const t = Math.min(Math.max((ahora - inicio) / duracion, 0), 1);
            contador.textContent = Math.round(meta * (1 - Math.pow(1 - t, 3))).toLocaleString('es-MX');
            if (t < 1) rafContador = requestAnimationFrame(paso);
        };

        rafContador = requestAnimationFrame(paso);
    }

    // ---------- Eventos ----------

    // Cuando termina la barra de un paso, pasa al siguiente
    raiz.addEventListener('animationend', e => {
        if (e.animationName === 'cfBarra') mostrar(actual + 1);
    });

    pasos.forEach((p, i) => p.addEventListener('click', () => mostrar(i)));

    raiz.querySelector('.cf-pasos').addEventListener('keydown', e => {

        const mas = e.key === 'ArrowDown' || e.key === 'ArrowRight';
        const menos = e.key === 'ArrowUp' || e.key === 'ArrowLeft';
        if (!mas && !menos) return;

        e.preventDefault();
        mostrar(actual + (mas ? 1 : -1));
        pasos[actual].focus();
    });

    raiz.addEventListener('mouseenter', () => { encima = true; actualizarPausa(); });
    raiz.addEventListener('mouseleave', () => { encima = false; actualizarPausa(); });
    document.addEventListener('visibilitychange', actualizarPausa);

    btnRepetir?.addEventListener('click', () => {
        mostrar(0);
    });

    btnPausa?.addEventListener('click', () => {
        pausadoPorUsuario = !pausadoPorUsuario;
        btnPausa.setAttribute('aria-pressed', String(pausadoPorUsuario));
        btnPausa.textContent = pausadoPorUsuario ? 'Reanudar' : 'Pausar';
        actualizarPausa();
    });

    // ---------- Arranque: cuando la sección entra en pantalla ----------

    function alVer(entrada) {
        viendose = entrada;
        if (entrada && actual === -1) mostrar(0);
        actualizarPausa();
    }

    if ('IntersectionObserver' in window) {
        new IntersectionObserver(([e]) => alVer(e.isIntersecting), { threshold: 0.35 }).observe(raiz);
    } else {
        alVer(true);
    }
}