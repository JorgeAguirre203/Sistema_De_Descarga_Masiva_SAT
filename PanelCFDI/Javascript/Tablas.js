(function () {
    const URL_FIJAR = '/Config/FijarTiempoWorker';
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
                mostrar('Escribe un tiempo entre 1 y 1440 minutos.', 'err');
                return;
            }

            btn.disabled = true;
            try {
                s
                const resp = await fetch(URL_FIJAR, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ tipo, tiempo })
                });
                const data = await resp.json();

                if (data.ok) {
                    mostrar(`${tipo}: tiempo actualizado a ${data.tiempo} min.`, 'ok');
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