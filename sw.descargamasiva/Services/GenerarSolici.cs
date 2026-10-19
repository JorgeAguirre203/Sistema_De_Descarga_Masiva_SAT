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
     X509Certificate2 certificate,
     string authorization,
     out DateTime fechaInicial,
     out DateTime fechaFinal,
           out string RfcReceptor,
            out string RfcEmisor,
            out string RfcSolicitante)
        {
            RfcReceptor = Guardarenbased.ObtenerRFC("RECIBIDO");
            RfcEmisor = Guardarenbased.ObtenerRFCEMISOR("EMISOR");
            RfcSolicitante = Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE");


            fechaInicial = Guardarenbased.ObtenerUltimaConsulta("RECIBIDO");



            fechaFinal = fechaInicial.AddHours(24);
         

            Console.WriteLine();
            Console.WriteLine(
                "Periodo que se solicitará al SAT:");

            Console.WriteLine(
        $"Inicio: {fechaInicial:yyyy-MM-dd HH:mm:ss}");

            Console.WriteLine(
                $"Final : {fechaFinal:yyyy-MM-dd HH:mm:ss}");


            Solicitud solicitud =
                new Solicitud(
                    urlSolicitud,
                    urlSolicitudRecibidosAction);


          
            // GENERAR XML
          

            string xmlSolicitud =
                solicitud.Generate(

                    certificate: certificate,

                    rfcEmisor:
                        Guardarenbased.ObtenerRFCEMISOR("EMISOR"),

                    rfcReceptor:
                       Guardarenbased.ObtenerRFC("RECIBIDO"),

                    rfcSolicitante:
                        Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE"),

                    fechaInicial:
                        fechaInicial.ToString(
                            "yyyy-MM-ddTHH:mm:ss"),

                    fechaFinal:
                        fechaFinal.ToString(
                            "yyyy-MM-ddTHH:mm:ss")
                );


            Console.WriteLine();
            Console.WriteLine(
                "Solicitud XML a enviar al SAT:");

            Console.WriteLine(xmlSolicitud);


            
            // ENVIAR AL SAT
            
            string respuesta =
                solicitud.Send(authorization);


            // SOLO LLEGAMOS AQUÍ SI SAT ACEPTÓ
          

            Guardarenbased.ActualizarUltimaConsulta(
                "RECIBIDO",
                fechaFinal);


            Console.WriteLine();
            Console.WriteLine(
                "Última consulta actualizada en BD.");

            Console.WriteLine(
                $"Nueva fecha: {fechaFinal:yyyy-MM-dd HH:mm:ss}");


            return respuesta;
        }
    }
}