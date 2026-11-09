// TEMA: claro / oscuro (se recuerda en el navegador)

(function () {

    const raiz = document.documentElement;
    const boton = document.getElementById('btnTema');
    const texto = document.getElementById('temaTexto');
    const clave = 'panelcfdi.tema';

    function aplicar(tema) {
        const oscuro = tema === 'oscuro';
        raiz.dataset.tema = oscuro ? 'oscuro' : 'claro';
        boton.setAttribute('aria-pressed', String(oscuro));
        texto.textContent = oscuro ? 'Modo claro' : 'Modo oscuro';
        boton.title = oscuro ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro';
    }

    aplicar(raiz.dataset.tema);

    boton.addEventListener('click', function () {
        const nuevo = raiz.dataset.tema === 'oscuro' ? 'claro' : 'oscuro';
        aplicar(nuevo);
        try { localStorage.setItem(clave, nuevo); } catch (e) { }
    });

})();