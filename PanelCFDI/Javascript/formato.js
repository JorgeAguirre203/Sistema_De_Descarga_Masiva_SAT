// ESCAPAR HTML

export const esc = s =>
    String(s ?? '')
        .replace(
            /[&<>"']/g,
            c => ({
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
            }[c])
        );

// FORMATO DE DINERO

export const dinero = n =>
    Number(n).toLocaleString(
        'es-MX',
        {
            style: 'currency',
            currency: 'MXN'
        }
    );

// FORMATO DE FECHA

export const fechaMx = f =>
    f
        ? f
            .slice(0, 10)
            .split('-')
            .reverse()
            .join('/')
        : '';

// CLASE DEL TIPO DE CFDI

export const claseTipo = t =>
({
    Ingreso: 'ok',
    Nómina: 'ok',
    Egreso: 'err',
    Pago: 'pend'
}[t] || 'pend');

// CLASE DEL ESTADO DE SOLICITUD

export const claseEstado = e =>
    /descargada|terminada|sin cfdi/i.test(e)
        ? 'ok'
        : /error|rechazada|vencida/i.test(e)
            ? 'err'
            : 'pend';