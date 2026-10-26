using sw.descargamasiva.InsertarBD;
using System;
using System.Security.Cryptography.X509Certificates;

namespace sw.descargamasiva.Services
{
    internal class GenerarSolicEmit
    {
        static string urlSolicitud =
            "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/SolicitaDescargaService.svc";

        static string urlSolicitudEmitidosAction =
            "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaEmitidos";

        // Genera la solicitud de CFDI EMITIDOS para un RFC concreto.
        public static string GenerarSolicitudEmitidos(
            string rfcActual,
            X509Certificate2 certificate,
            string authorization,
            out DateTime fechaInicial,
            out DateTime fechaFinal)
        {
            // Última consulta de ESTE rfc y tipo EMITIDO
            fechaInicial = Guardarenbased.ObtenerUltimaConsultaPorRfc(rfcActual, "EMITIDO");
            fechaFinal = fechaInicial.AddHours(24);

            Console.WriteLine();
            Console.WriteLine("Periodo que se solicitará al SAT (EMITIDOS):");
            Console.WriteLine($"Desde: {fechaInicial:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Hasta: {fechaFinal:yyyy-MM-dd HH:mm:ss}");

            // SolicitudEmitidos arma el nodo SolicitaDescargaEmitidos
            // (la clase Solicitud está fija a SolicitaDescargaRecibidos).
            SolicitudEmitidos solicitud =
                new SolicitudEmitidos(urlSolicitud, urlSolicitudEmitidosAction);

            string xmlSolicitud = solicitud.Generate(
                certificate: certificate,
                rfcSolicitante: rfcActual,
                rfcEmisor: rfcActual,
                fechaInicial: fechaInicial,
                fechaFinal: fechaFinal);

            Console.WriteLine();
            Console.WriteLine("Solicitud XML a enviar al SAT:");
            Console.WriteLine(xmlSolicitud);

            string respuesta = solicitud.Send(authorization);

            // Solo llegamos aquí si el SAT aceptó (GetResult lanza excepción si no).
            // Antes se actualizaba "RECIBIDO" por error; ahora EMITIDO de este RFC.
            Guardarenbased.ActualizarUltimaConsultaPorRfc(
                rfcActual,
                "EMITIDO",
                fechaFinal);

            Console.WriteLine();
            Console.WriteLine("Última consulta (EMITIDO) actualizada en BD.");
            Console.WriteLine($"Nueva fecha: {fechaFinal:yyyy-MM-dd HH:mm:ss}");

            return respuesta;
        }
    }
}