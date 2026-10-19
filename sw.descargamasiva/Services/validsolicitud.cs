using sw.descargamasiva.InsertarBD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using sw.descargamasiva.Worker;


namespace sw.descargamasiva.Services
{
    internal class validsolicitud
    {
        static string urlVerificarSolicitud = "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/VerificaSolicitudDescargaService.svc";
        static string urlVerificarSolicitudAction = "http://DescargaMasivaTerceros.sat.gob.mx/IVerificaSolicitudDescargaService/VerificaSolicitudDescarga";
        public static VerificaSolicitud ValidarSolicitud(
            X509Certificate2 certificate,
            string authorization,
            string idSolicitud)
        {
            VerificaSolicitud verifica =
                new VerificaSolicitud(
                    urlVerificarSolicitud,
                    urlVerificarSolicitudAction);

            string xmlVerifica =
                verifica.Generate(
                    certificate,
                    Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE"),
                    idSolicitud);

            Console.WriteLine();
            Console.WriteLine(
                "======= XML VERIFICACIÓN =======");

            Console.WriteLine(xmlVerifica);

            Console.WriteLine(
                "================================");



            verifica.Send(authorization);

            return verifica;
        }
    }
}
