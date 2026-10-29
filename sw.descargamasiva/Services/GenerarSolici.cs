using System;
using System.Security.Cryptography.X509Certificates;
using sw.descargamasiva.InsertarBD;

namespace sw.descargamasiva.Services
{
    internal class GenerarSolici
    {
        static string urlSolicitud =
            "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/SolicitaDescargaService.svc";
        static string urlSolicitudRecibidosAction =
            "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaRecibidos";

       
        public static string GenerarSolicitudRecibidos(
            string rfcActual,
            X509Certificate2 certificate,
            string authorization,
            out DateTime fechaInicial,
            out DateTime fechaFinal,
            out string RfcReceptor,
            out string RfcEmisor,
            out string RfcSolicitante)
        {
            //  Usamos el rfcActual para buscar los datos específicos de este contribuyente en la BD
            RfcReceptor = rfcActual;
            RfcEmisor = rfcActual;
            RfcSolicitante = rfcActual; 

            // Obtenemos la última fecha de consulta vinculada estrictamente a este RFC
            fechaInicial = Guardarenbased.ObtenerUltimaConsultaPorRfc(rfcActual, "RECIBIDO");

            // No pedir el presente ni el futuro: se deja 1 hora de margen y,
            // ya al día, se espera a juntar al menos 6 horas antes de pedir.
            DateTime limite = DateTime.Now.AddHours(-1);

            if (limite - fechaInicial < TimeSpan.FromHours(6))
            {
                fechaFinal = fechaInicial;
                Console.WriteLine($"RFC {rfcActual} (RECIBIDOS) al día. Última consulta: {fechaInicial:yyyy-MM-dd HH:mm:ss}");
                return null;
            }

            fechaFinal = fechaInicial.AddHours(24);

            if (fechaFinal > limite)
                fechaFinal = limite;

            Console.WriteLine();
            Console.WriteLine("Periodo que se solicitará al SAT:");
            Console.WriteLine($"Inicio: {fechaInicial:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Final : {fechaFinal:yyyy-MM-dd HH:mm:ss}");

            Solicitud solicitud = new Solicitud(
                urlSolicitud,
                urlSolicitudRecibidosAction);

            //      Generamos el XML inyectando el rfcActual en lugar de datos quemados
            string xmlSolicitud = solicitud.Generate(
                certificate: certificate,
                rfcEmisor: rfcActual,
                rfcReceptor: rfcActual,
                rfcSolicitante: rfcActual,
                fechaInicial: fechaInicial.ToString("yyyy-MM-ddTHH:mm:ss"),
                fechaFinal: fechaFinal.ToString("yyyy-MM-ddTHH:mm:ss")
            );

            Console.WriteLine();
            Console.WriteLine("Solicitud XML a enviar al SAT:");
            Console.WriteLine(xmlSolicitud);

            // ENVIAR AL SAT
            string respuesta = solicitud.Send(authorization);

            // Actualizamos la última consulta para este RFC en específico
            Guardarenbased.ActualizarUltimaConsultaPorRfc(
                rfcActual,
                "RECIBIDO",
                fechaFinal);

            Console.WriteLine();
            Console.WriteLine("Última consulta actualizada en BD.");
            Console.WriteLine($"Nueva fecha: {fechaFinal:yyyy-MM-dd HH:mm:ss}");

            return respuesta;
        }
    }
}