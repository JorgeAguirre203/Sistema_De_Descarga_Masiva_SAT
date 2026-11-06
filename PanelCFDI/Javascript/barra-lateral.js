// BARRA LATERAL: fijar / soltar (se recuerda en el navegador)

(function () {

    const raiz = document.documentElement;
    const boton = document.getElementById('btnFijar');
    const clave = 'panelcfdi.lateral.fijada';

    function aplicar(fijada) {
        raiz.classList.toggle('fijada', fijada);
        boton.setAttribute('aria-pressed', String(fijada));
        const texto = fijada ? 'Soltar barra lateral' : 'Fijar barra lateral';
        boton.title = texto;
        boton.setAttribute('aria-label', texto);
    }

    aplicar(raiz.classList.contains('fijada'));

    boton.addEventListener('click', function () {
        const fijada = !raiz.classList.contains('fijada');
        aplicar(fijada);
        try { localStorage.setItem(clave, fijada ? '1' : '0'); } catch (e) { }
    });

})();