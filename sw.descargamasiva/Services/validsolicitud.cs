using System;
using System.Security.Cryptography.X509Certificates;

namespace sw.descargamasiva.Services
{
    internal class validsolicitud
    {
        static string urlVerificarSolicitud = "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/VerificaSolicitudDescargaService.svc";
        static string urlVerificarSolicitudAction = "http://DescargaMasivaTerceros.sat.gob.mx/IVerificaSolicitudDescargaService/VerificaSolicitudDescarga";

        // Agregamos 'string rfcSolicitante' como parámetro
        public static VerificaSolicitud ValidarSolicitud(
            X509Certificate2 certificate,
            string authorization,
            string idSolicitud,
            string rfcSolicitante)
        {
            VerificaSolicitud verifica =
                new VerificaSolicitud(
                    urlVerificarSolicitud,
                    urlVerificarSolicitudAction);

            // Usamos el parámetro que recibimos en lugar de la consulta a la BD
            string xmlVerifica =
                verifica.Generate(
                    certificate,
                    rfcSolicitante,
                    idSolicitud);

            Console.WriteLine();
            Console.WriteLine("======= XML VERIFICACIÓN =======");
            Console.WriteLine(xmlVerifica);
            Console.WriteLine("================================");

            verifica.Send(authorization);

            return verifica;
        }
    }
}