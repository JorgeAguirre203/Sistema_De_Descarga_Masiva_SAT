(function () {
    const URL_FIJAR = '/Control/FijarTiempoWorker';
    const msg = document.getElementById('msgTiempos');

    function mostrar(texto, tipo) {
        if (!msg) return;
        msg.textContent = texto;
        msg.dataset.tipo = tipo;
    }

    document.querySelectorAll('.btn-tiempo').forEach(btn => {
        btn.addEventListener('click', async () => {
            const input = document.getElementById(btn.dataset.input);
            const tipo = btn.dataset.tipo;
            const tiempo = parseInt(input.value, 10);

            if (!Number.isInteger(tiempo) || tiempo < 1 || tiempo > 1440) {
                mostrar('Escribe un tiempo entre 1 y 1440 Horas.', 'err');
                return;
            }

            btn.disabled = true;
            try {
                
                const resp = await fetch(URL_FIJAR, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ tipo, tiempo })
                });
                const data = await resp.json();

                if (data.ok) {
                    mostrar(`${tipo}: tiempo actualizado a ${data.tiempo} horas.`, 'ok');
                } else {
                    mostrar(data.error || 'No se pudo actualizar.', 'err');
                }
            } catch (e) {
                mostrar('Error de conexión: ' + e.message, 'err');
            } finally {
                btn.disabled = false;
            }
        });
    });
})();


const tabs = document.querySelectorAll('#vista-config .tab');
const paneles = document.querySelectorAll('#vista-config .tab-panel');

function activar(tab) {
    tabs.forEach(t => {
        const on = t === tab;
        t.classList.toggle('activa', on);
        t.setAttribute('aria-selected', on);
        t.tabIndex = on ? 0 : -1;
    });

    paneles.forEach(p => {
        p.hidden = p.id !== tab.dataset.panel;
    });
}

tabs.forEach(t => t.addEventListener('click', () => activar(t)));