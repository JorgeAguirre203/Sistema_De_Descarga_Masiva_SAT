import './navegacion.js';
import './barra-lateral.js';
import { cargar } from './fechas-rfc.js';
import { pintar, cargarDatos } from './cfdi.js';

// ARRANQUE

pintar();

cargar();

cargarDatos();


// Actualizar automáticamente
// cada 30 segundos

setInterval(
    cargarDatos,
    30000
);