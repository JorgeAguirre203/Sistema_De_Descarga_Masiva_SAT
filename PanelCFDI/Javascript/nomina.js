// VISTA "EXPORTAR PDF": convierte el XML de un CFDI (nómina o comprobante normal) a PDF.
// Todo se procesa en el navegador. Requiere jsPDF, jsPDF-AutoTable y qrcode-generator
// cargados con <script> en index.html ANTES de main.js (exponen window.jspdf y window.qrcode).

/* ---------- Catálogos del SAT (los más comunes) ---------- */
const CAT = {
    regimen: { "601": "General de Ley Personas Morales", "603": "Personas Morales con Fines no Lucrativos", "605": "Sueldos y Salarios", "606": "Arrendamiento", "612": "Actividades Empresariales y Profesionales", "616": "Sin obligaciones fiscales", "621": "Incorporación Fiscal", "626": "Régimen Simplificado de Confianza" },
    tipoNomina: { O: "Ordinaria", E: "Extraordinaria" },
    periodicidad: { "01": "Diario", "02": "Semanal", "03": "Catorcenal", "04": "Quincenal", "05": "Mensual", "06": "Bimestral", "07": "Unidad obra", "08": "Comisión", "09": "Precio alzado", "10": "Decenal", "99": "Otra" },
    contrato: { "01": "Tiempo indeterminado", "02": "Obra determinada", "03": "Tiempo determinado", "04": "Por temporada", "05": "Sujeto a prueba", "06": "Capacitación inicial", "07": "Pago por hora", "08": "Comisión laboral", "09": "Sin relación de trabajo", "10": "Jubilación o pensión", "99": "Otro" },
    jornada: { "01": "Diurna", "02": "Nocturna", "03": "Mixta", "04": "Por hora", "05": "Reducida", "06": "Continuada", "07": "Partida", "08": "Por turnos", "99": "Otra" },
    tipoRegimen: { "02": "Sueldos", "03": "Jubilados", "04": "Pensionados", "09": "Asimilados honorarios", "11": "Asimilados otros", "12": "Jubilados o pensionados", "13": "Indemnización o separación", "99": "Otro" },
    formaPago: { "01": "Efectivo", "02": "Cheque", "03": "Transferencia", "04": "Tarjeta de crédito", "28": "Tarjeta de débito", "99": "Por definir" },
    usoCfdi: { "CN01": "Nómina", "S01": "Sin efectos fiscales", "G03": "Gastos en general" }
};
const cat = (tabla, v) => v ? (CAT[tabla][v] ? `${v} · ${CAT[tabla][v]}` : v) : "";


/* ---------- Lectura del XML ---------- */
function leerCfdi(texto) {
    const xml = new DOMParser().parseFromString(texto, "application/xml");
    if (xml.getElementsByTagName("parsererror").length) throw new Error("El archivo no es un XML válido.");
    const uno = (nombre, dentro) => (dentro || xml).getElementsByTagNameNS("*", nombre)[0] || null;
    const todos = (nombre, dentro) => Array.from((dentro || xml).getElementsByTagNameNS("*", nombre));
    const at = (el, a) => (el && el.getAttribute(a)) || "";

    const comp = xml.documentElement;
    if (!comp || comp.localName !== "Comprobante") throw new Error("No parece un CFDI (falta el nodo Comprobante).");
    const emisor = uno("Emisor"), receptor = uno("Receptor"), tfd = uno("TimbreFiscalDigital");
    const nom = uno("Nomina");

    const d = {
        version: at(comp, "Version"), serie: at(comp, "Serie"), folio: at(comp, "Folio"),
        fecha: at(comp, "Fecha"), subtotal: at(comp, "SubTotal"), descuento: at(comp, "Descuento"),
        total: at(comp, "Total"), moneda: at(comp, "Moneda"), tipo: at(comp, "TipoDeComprobante"),
        formaPago: at(comp, "FormaPago"), metodoPago: at(comp, "MetodoPago"), lugar: at(comp, "LugarExpedicion"),
        noCert: at(comp, "NoCertificado"),
        emisor: { rfc: at(emisor, "Rfc"), nombre: at(emisor, "Nombre"), regimen: at(emisor, "RegimenFiscal") },
        receptor: {
            rfc: at(receptor, "Rfc"), nombre: at(receptor, "Nombre"), cp: at(receptor, "DomicilioFiscalReceptor"),
            regimen: at(receptor, "RegimenFiscalReceptor"), uso: at(receptor, "UsoCFDI")
        },
        timbre: tfd ? {
            uuid: at(tfd, "UUID"), fecha: at(tfd, "FechaTimbrado"), noCertSat: at(tfd, "NoCertificadoSAT"),
            selloCfd: at(tfd, "SelloCFD"), selloSat: at(tfd, "SelloSAT"), rfcProv: at(tfd, "RfcProvCertif")
        } : null,
        conceptos: [], nomina: null
    };

    if (nom) {
        const nEm = uno("Emisor", nom), nRe = uno("Receptor", nom);
        const per = uno("Percepciones", nom), ded = uno("Deducciones", nom);
        d.nomina = {
            tipo: at(nom, "TipoNomina"), fechaPago: at(nom, "FechaPago"), inicio: at(nom, "FechaInicialPago"),
            fin: at(nom, "FechaFinalPago"), dias: at(nom, "NumDiasPagados"),
            totPer: at(nom, "TotalPercepciones"), totDed: at(nom, "TotalDeducciones"), totOtros: at(nom, "TotalOtrosPagos"),
            registroPatronal: at(nEm, "RegistroPatronal"),
            curp: at(nRe, "Curp"), nss: at(nRe, "NumSeguridadSocial"), ingreso: at(nRe, "FechaInicioRelLaboral"),
            antiguedad: antig(at(nRe, "Antigüedad") || at(nRe, "Antiguedad")), contrato: at(nRe, "TipoContrato"),
            jornada: at(nRe, "TipoJornada"), tipoRegimen: at(nRe, "TipoRegimen"), numEmpleado: at(nRe, "NumEmpleado"),
            departamento: at(nRe, "Departamento"), puesto: at(nRe, "Puesto"), periodicidad: at(nRe, "PeriodicidadPago"),
            banco: at(nRe, "Banco"), cuenta: at(nRe, "CuentaBancaria"), sbc: at(nRe, "SalarioBaseCotApor"),
            sdi: at(nRe, "SalarioDiarioIntegrado"), entidad: at(nRe, "ClaveEntFed"),
            percepciones: per ? todos("Percepcion", per).map(p => ({
                clave: at(p, "Clave") || at(p, "TipoPercepcion"),
                concepto: at(p, "Concepto"), gravado: +at(p, "ImporteGravado") || 0, exento: +at(p, "ImporteExento") || 0
            })) : [],
            deducciones: ded ? todos("Deduccion", ded).map(x => ({
                clave: at(x, "Clave") || at(x, "TipoDeduccion"),
                concepto: at(x, "Concepto"), importe: +at(x, "Importe") || 0
            })) : [],
            otros: todos("OtroPago", nom).map(x => ({
                clave: at(x, "Clave") || at(x, "TipoOtroPago"),
                concepto: at(x, "Concepto"), importe: +at(x, "Importe") || 0
            }))
        };
    } else {
        d.conceptos = todos("Concepto").map(c => ({
            clave: at(c, "ClaveProdServ"), cantidad: at(c, "Cantidad"),
            descripcion: at(c, "Descripcion"), unitario: +at(c, "ValorUnitario") || 0, importe: +at(c, "Importe") || 0
        }));
    }
    return d;
}


/* ---------- Formatos ---------- */
function antig(v) {
    const m = /^P(?:(\d+)Y)?(?:(\d+)M)?(?:(\d+)W)?(?:(\d+)D)?$/.exec(v || "");
    if (!m) return v;
    const u = [[m[1], "año", "años"], [m[2], "mes", "meses"], [m[3], "semana", "semanas"], [m[4], "día", "días"]];
    return u.filter(x => +x[0]).map(x => `${+x[0]} ${+x[0] === 1 ? x[1] : x[2]}`).join(", ") || v;
}
const MXN = new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" });
const dinero = v => MXN.format(+v || 0);
const MESES = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];
function fecha(s, conHora) {
    if (!s) return "";
    const [f, h] = s.split("T"); const [a, m, dd] = f.split("-");
    if (!m) return s;
    return `${+dd} ${MESES[+m - 1]} ${a}` + (conHora && h ? ` ${h.slice(0, 5)}` : "");
}
const ocultarDato = (v, visibles = 4) => !v ? "" : (v.length <= visibles ? v : "•".repeat(Math.min(6, v.length - visibles)) + v.slice(-visibles));


/* ---------- PDF ---------- */
function crearPdf(d, opciones) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "pt", format: "letter" });
    const W = doc.internal.pageSize.getWidth(), H = doc.internal.pageSize.getHeight();
    const M = 40, ancho = W - 2 * M;
    const VERDE = [31, 92, 69], TINTA = [24, 35, 31], GRIS = [93, 107, 101], LINEA = [213, 221, 217], SUAVE = [235, 242, 238];
    const n = d.nomina;
    const sens = v => opciones.ocultar ? ocultarDato(v) : v;

    // Encabezado
    doc.setFillColor(...VERDE); doc.rect(0, 0, W, 84, "F");
    doc.setTextColor(255, 255, 255);
    doc.setFont("helvetica", "bold"); doc.setFontSize(9);
    doc.text(n ? "RECIBO DE NÓMINA" : "COMPROBANTE FISCAL DIGITAL", M, 30, { charSpace: 1 });
    doc.setFontSize(15);
    if (doc.getTextWidth(d.emisor.nombre || "") > ancho - 190) doc.setFontSize(10);
    doc.text(doc.splitTextToSize(d.emisor.nombre || "Emisor", ancho - 190).slice(0, 2), M, 47);
    doc.setFont("helvetica", "normal"); doc.setFontSize(9);
    const lineaEm = [`RFC ${d.emisor.rfc}`, n && n.registroPatronal ? `Reg. patronal ${n.registroPatronal}` : "", CAT.regimen[d.emisor.regimen] || ""].filter(Boolean).join("   ·   ");
    doc.text(lineaEm, M, 71);
    doc.setFont("helvetica", "bold"); doc.setFontSize(9);
    doc.text(`Folio ${[d.serie, d.folio].filter(Boolean).join("-") || "—"}`, W - M, 30, { align: "right" });
    doc.setFont("helvetica", "normal");
    doc.text(`Emitido ${fecha(d.fecha, true)}`, W - M, 46, { align: "right" });
    if (n) doc.text(`Pago ${fecha(n.fechaPago)}`, W - M, 60, { align: "right" });

    let y = 112;
    doc.setTextColor(...TINTA);

    // Bloque de datos en dos columnas
    function bloque(titulo, filas) {
        filas = filas.filter(f => f[1]);
        doc.setFont("helvetica", "bold"); doc.setFontSize(8); doc.setTextColor(...VERDE);
        doc.text(titulo.toUpperCase(), M, y, { charSpace: 0.8 });
        y += 8; doc.setDrawColor(...LINEA); doc.setLineWidth(0.6); doc.line(M, y, W - M, y); y += 15;
        const col = ancho / 2;
        for (let i = 0; i < filas.length; i += 2) {
            let alto = 0;
            [filas[i], filas[i + 1]].forEach((f, k) => {
                if (!f) return;
                const x = M + k * col;
                doc.setFont("helvetica", "normal"); doc.setFontSize(7.5); doc.setTextColor(...GRIS);
                doc.text(f[0], x, y);
                doc.setFontSize(9.5); doc.setTextColor(...TINTA);
                const t = doc.splitTextToSize(String(f[1]), col - 14);
                doc.text(t, x, y + 12);
                alto = Math.max(alto, t.length);
            });
            y += 14 + alto * 11 + 6;
        }
        y += 6;
    }

    if (n) {
        bloque("Empleado", [
            ["Nombre", d.receptor.nombre], ["RFC", d.receptor.rfc],
            ["CURP", sens(n.curp)], ["Núm. de seguridad social", sens(n.nss)],
            ["Núm. de empleado", n.numEmpleado], ["Puesto", n.puesto],
            ["Departamento", n.departamento], ["Fecha de ingreso", fecha(n.ingreso)],
            ["Tipo de contrato", cat("contrato", n.contrato)], ["Jornada", cat("jornada", n.jornada)],
            ["Salario base de cotización", n.sbc ? dinero(n.sbc) : ""], ["Salario diario integrado", n.sdi ? dinero(n.sdi) : ""],
            ["Cuenta de depósito", n.cuenta ? sens(n.cuenta) : ""], ["Antigüedad", n.antiguedad]
        ]);
        bloque("Periodo", [
            ["Periodo pagado", n.inicio ? `${fecha(n.inicio)} al ${fecha(n.fin)}` : ""], ["Días pagados", n.dias ? (+n.dias).toString() : ""],
            ["Tipo de nómina", cat("tipoNomina", n.tipo)], ["Periodicidad", cat("periodicidad", n.periodicidad)]
        ]);

        // Resumen
        const tot = [
            ["Percepciones", n.totPer || n.percepciones.reduce((s, p) => s + p.gravado + p.exento, 0)],
            ["Deducciones", n.totDed || n.deducciones.reduce((s, p) => s + p.importe, 0)],
            ["Otros pagos", n.totOtros || n.otros.reduce((s, p) => s + p.importe, 0)]
        ];
        const altoR = 54;
        doc.setFillColor(...SUAVE); doc.roundedRect(M, y, ancho, altoR, 6, 6, "F");
        const anchoNeto = 170, celda = (ancho - anchoNeto) / 3;
        tot.forEach((t, i) => {
            const x = M + 16 + i * celda;
            doc.setFont("helvetica", "normal"); doc.setFontSize(7.5); doc.setTextColor(...GRIS);
            doc.text(t[0].toUpperCase(), x, y + 21, { charSpace: 0.6 });
            doc.setFont("helvetica", "bold"); doc.setFontSize(11); doc.setTextColor(...TINTA);
            doc.text(dinero(t[1]), x, y + 38);
        });
        doc.setFillColor(...VERDE); doc.roundedRect(W - M - anchoNeto, y, anchoNeto, altoR, 6, 6, "F");
        doc.setTextColor(255, 255, 255); doc.setFont("helvetica", "normal"); doc.setFontSize(7.5);
        doc.text("NETO A PAGAR", W - M - anchoNeto + 16, y + 21, { charSpace: 0.6 });
        doc.setFont("helvetica", "bold"); doc.setFontSize(16);
        doc.text(dinero(d.total), W - M - anchoNeto + 16, y + 41);
        y += altoR + 24;
    } else {
        bloque("Receptor", [
            ["Nombre", d.receptor.nombre], ["RFC", d.receptor.rfc],
            ["Régimen fiscal", cat("regimen", d.receptor.regimen)], ["Uso del CFDI", cat("usoCfdi", d.receptor.uso)],
            ["Forma de pago", cat("formaPago", d.formaPago)], ["Método de pago", d.metodoPago]
        ]);
    }

    // Tablas
    const tabla = (titulo, head, body, foot, alinear) => {
        if (!body.length) return;
        doc.autoTable({
            startY: y, margin: { left: M, right: M, bottom: 60 },
            head: [[{ content: titulo.toUpperCase(), colSpan: head.length, styles: { textColor: VERDE, fontSize: 8, cellPadding: { top: 0, bottom: 6, left: 0, right: 0 } } }], head],
            body, foot: foot ? [foot] : undefined, theme: "plain",
            styles: { font: "helvetica", fontSize: 9, textColor: TINTA, cellPadding: { top: 5, bottom: 5, left: 6, right: 6 }, lineColor: LINEA },
            headStyles: { fontStyle: "bold", fontSize: 7.5, textColor: GRIS },
            footStyles: { fontStyle: "bold", textColor: TINTA },
            columnStyles: alinear,
            didParseCell: c => {
                if (c.section === "head" && c.row.index === 1) { c.cell.styles.lineWidth = { bottom: 0.8 }; c.cell.styles.lineColor = LINEA; }
                if (c.section === "body") { c.cell.styles.lineWidth = { bottom: 0.4 }; c.cell.styles.lineColor = LINEA; }
                if (c.section === "foot") { c.cell.styles.lineWidth = { top: 0.8 }; c.cell.styles.lineColor = TINTA; }
                if (c.section !== "body" && alinear[c.column.index] && alinear[c.column.index].halign) c.cell.styles.halign = alinear[c.column.index].halign;
            }
        });
        y = doc.lastAutoTable.finalY + 22;
    };
    const der = { halign: "right" };
    if (n) {
        const p = n.percepciones;
        tabla("Percepciones", ["Clave", "Concepto", "Gravado", "Exento", "Importe"],
            p.map(x => [x.clave, x.concepto, dinero(x.gravado), dinero(x.exento), dinero(x.gravado + x.exento)]),
            ["", "Total", dinero(p.reduce((s, x) => s + x.gravado, 0)), dinero(p.reduce((s, x) => s + x.exento, 0)), dinero(p.reduce((s, x) => s + x.gravado + x.exento, 0))],
            { 0: { cellWidth: 52 }, 2: der, 3: der, 4: der });
        tabla("Deducciones", ["Clave", "Concepto", "Importe"],
            n.deducciones.map(x => [x.clave, x.concepto, dinero(x.importe)]),
            ["", "Total", dinero(n.deducciones.reduce((s, x) => s + x.importe, 0))], { 0: { cellWidth: 52 }, 2: der });
        tabla("Otros pagos", ["Clave", "Concepto", "Importe"],
            n.otros.map(x => [x.clave, x.concepto, dinero(x.importe)]),
            ["", "Total", dinero(n.otros.reduce((s, x) => s + x.importe, 0))], { 0: { cellWidth: 52 }, 2: der });
    } else {
        tabla("Conceptos", ["Clave", "Descripción", "Cant.", "Valor unitario", "Importe"],
            d.conceptos.map(c => [c.clave, c.descripcion, c.cantidad, dinero(c.unitario), dinero(c.importe)]),
            ["", "Total", "", "", dinero(d.total)], { 0: { cellWidth: 62 }, 2: der, 3: der, 4: der });
    }

    // Timbre fiscal con QR
    if (d.timbre) {
        const altoT = 150;
        if (y + altoT > H - 50) { doc.addPage(); y = 50; }
        doc.setFont("helvetica", "bold"); doc.setFontSize(8); doc.setTextColor(...VERDE);
        doc.text("TIMBRE FISCAL DIGITAL", M, y, { charSpace: 0.8 });
        y += 8; doc.setDrawColor(...LINEA); doc.line(M, y, W - M, y); y += 12;

        const lado = 92;
        if (window.qrcode) {
            const fe = (d.timbre.selloCfd || "").slice(-8);
            const url = `https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id=${d.timbre.uuid}&re=${d.emisor.rfc}&rr=${d.receptor.rfc}&tt=${d.total}&fe=${encodeURIComponent(fe)}`;
            const qr = window.qrcode(0, "M"); qr.addData(url); qr.make();
            const k = qr.getModuleCount(), t = lado / k;
            doc.setFillColor(...TINTA);
            for (let r = 0; r < k; r++) for (let c = 0; c < k; c++) if (qr.isDark(r, c)) doc.rect(M + c * t, y + r * t, t + 0.05, t + 0.05, "F");
        }
        const x = M + lado + 18, w = W - M - x;
        const datos = [["Folio fiscal (UUID)", d.timbre.uuid], ["Fecha de timbrado", fecha(d.timbre.fecha, true)],
        ["Certificado del emisor", d.noCert], ["Certificado del SAT", d.timbre.noCertSat], ["RFC del proveedor de certificación", d.timbre.rfcProv]];
        let yy = y + 6;
        datos.forEach(([e, v], i) => {
            const cx = x + (i === 0 ? 0 : ((i - 1) % 2) * (w / 2));
            if (i === 0 || i % 2 === 1) { if (i > 0) yy += 26; }
            doc.setFont("helvetica", "normal"); doc.setFontSize(7.5); doc.setTextColor(...GRIS); doc.text(e, cx, yy);
            doc.setFont(i === 0 ? "courier" : "helvetica", i === 0 ? "bold" : "normal"); doc.setFontSize(9); doc.setTextColor(...TINTA);
            doc.text(v || "—", cx, yy + 11);
        });
        y = Math.max(y + lado, yy + 20) + 12;

        doc.setFont("courier", "normal"); doc.setFontSize(5.5); doc.setTextColor(...GRIS);
        [["Sello digital del CFDI", d.timbre.selloCfd], ["Sello del SAT", d.timbre.selloSat]].forEach(([e, v]) => {
            if (!v) return;
            const lineas = doc.splitTextToSize(v, ancho);
            if (y + 10 + lineas.length * 6.5 > H - 40) { doc.addPage(); y = 50; }
            doc.setFont("helvetica", "bold"); doc.setFontSize(6.5); doc.text(e, M, y); y += 8;
            doc.setFont("courier", "normal"); doc.setFontSize(5.5); doc.text(lineas, M, y); y += lineas.length * 6.5 + 6;
        });
    }

    // Pie en cada página
    const paginas = doc.getNumberOfPages();
    for (let i = 1; i <= paginas; i++) {
        doc.setPage(i);
        doc.setFont("helvetica", "normal"); doc.setFontSize(7); doc.setTextColor(...GRIS);
        doc.text(`Representación impresa de un CFDI ${d.version}${n ? " con complemento de Nómina 1.2" : ""}`, M, H - 24);
        doc.text(`Página ${i} de ${paginas}`, W - M, H - 24, { align: "right" });
    }

    // Nombre del archivo
    const limpio = s => (s || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^\w]+/g, "_").replace(/^_|_$/g, "");
    const base = n ? `Nomina_${limpio(d.receptor.nombre)}_${(n.fechaPago || d.fecha).slice(0, 10)}`
        : `CFDI_${limpio(d.emisor.nombre).slice(0, 30)}_${d.fecha.slice(0, 10)}`;
    return { doc, nombre: base + ".pdf" };
}


/* ---------- Interfaz ---------- */
const lista = document.getElementById('npLista');
const contador = document.getElementById('npCuantos');
const casillaOcultar = document.getElementById('npOcultar');

function avisar(nombre, texto, error) {
    const li = document.createElement('li');
    li.innerHTML = `<span class="np-nombre"></span><span class="${error ? 'np-err' : 'np-monto'}"></span>`;
    li.children[0].textContent = nombre;
    li.children[1].textContent = texto;
    lista.prepend(li);
}

let generados = 0;

async function procesar(archivos) {
    if (!window.jspdf || !window.jspdf.jsPDF || !window.jspdf.jsPDF.API.autoTable) {
        avisar('Sin conexión', 'No se cargaron las herramientas de PDF. Revisa tu internet y recarga la página.', true);
        return;
    }
    for (const f of archivos) {
        try {
            const d = leerCfdi(await f.text());
            const { doc, nombre } = crearPdf(d, { ocultar: casillaOcultar.checked });
            doc.save(nombre);
            avisar(nombre, dinero(d.total), false);
            contador.textContent = ++generados;
        } catch (e) {
            avisar(f.name, e.message || 'No se pudo leer el archivo.', true);
        }
    }
}


/* ---------- Abrir en el celular ---------- */
document.getElementById('npBtnCel').addEventListener('click', () => {
    const panel = document.getElementById('npPanelQr'), btn = document.getElementById('npBtnCel');
    panel.hidden = !panel.hidden;
    btn.setAttribute('aria-expanded', String(!panel.hidden));
    if (panel.hidden) return;

    const caja = document.getElementById('npCodigoQr'), texto = document.getElementById('npTextoQr');
    const url = location.href.split('#')[0];
    const enLinea = location.protocol === 'http:' || location.protocol === 'https:';
    const soloEstaPc = /^(localhost|127\.|\[::1\])/.test(location.hostname);

    if (!enLinea || soloEstaPc) {
        caja.hidden = true;
        texto.innerHTML = '<strong>Primero publica la página</strong><p>Ahora la estás abriendo desde un archivo o desde <code>localhost</code>, y el celular no puede entrar a esa dirección.</p><p>Ábrela con una dirección que el celular alcance (por ejemplo, la IP de tu PC en tu red Wi-Fi) y aquí aparecerá el código QR.</p>';
        return;
    }
    if (window.qrcode) {
        const qr = window.qrcode(0, 'M'); qr.addData(url); qr.make();
        caja.innerHTML = qr.createSvgTag({ cellSize: 4, margin: 0, scalable: true });
        caja.hidden = false;
    }
    texto.innerHTML = '<strong>Escanéalo con la cámara del iPhone</strong><p>Se abre esta misma página en el teléfono. Elige el XML desde Archivos y el PDF se guarda ahí.</p><p>Dirección: <code></code></p>';
    texto.querySelector('code').textContent = url;
});


/* En iPhone el filtro por tipo a veces deja los XML en gris en la app Archivos */
if (/iP(hone|ad|od)/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) {
    document.getElementById('npArchivo').removeAttribute('accept');
}


/* ---------- Zona para soltar archivos ---------- */
const zona = document.getElementById('npZona'), input = document.getElementById('npArchivo');

input.addEventListener('change', () => { procesar(Array.from(input.files)); input.value = ''; });

['dragenter', 'dragover'].forEach(ev => zona.addEventListener(ev, e => { e.preventDefault(); zona.classList.add('over'); }));
['dragleave', 'drop'].forEach(ev => zona.addEventListener(ev, e => { e.preventDefault(); zona.classList.remove('over'); }));

zona.addEventListener('drop', e =>
    procesar(Array.from(e.dataTransfer.files).filter(f => /\.xml$/i.test(f.name) || f.type.includes('xml'))));