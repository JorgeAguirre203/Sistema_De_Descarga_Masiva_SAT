using sw.descargamasiva.InsertarBD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.Services
{
    internal class GenerarSolicEmit
    {
        static string urlSolicitud = "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/SolicitaDescargaService.svc";


        static string urlSolicitudEmitidosAction = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaEmitidos";

        //Esta funcion es la que se encarga de generar solicitudes de los CFDIS EMITIDOS
        public static string GenerarSolicitudEmitidos(
         X509Certificate2 certificate,
         string authorization,
         out DateTime fechaInicial,
         out DateTime fechaFinal,
         out string RfcEmisor)
        {
            RfcEmisor = Guardarenbased.ObtenerRFCEMISOR("EMISOR");
            fechaInicial =
        Guardarenbased.ObtenerUltimaConsulta("EMITIDO");

            fechaFinal = fechaInicial.AddHours(24);


            Console.WriteLine();
            Console.WriteLine(
                "Periodo que se solicitará al SAT:");

            Console.WriteLine(
                $"Desde: {fechaInicial:yyyy-MM-dd HH:mm:ss}");

            Console.WriteLine(
                $"Hasta: {fechaFinal:yyyy-MM-dd HH:mm:ss}");


            Solicitud solicitud =
                new Solicitud(
                    urlSolicitud,
                    urlSolicitudEmitidosAction);



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
