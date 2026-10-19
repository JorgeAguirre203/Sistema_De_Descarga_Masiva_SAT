using sw.descargamasiva.InsertarBD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.Services
{
    internal class SolicDescarga
    {
        static string urlDescargarSolicitud = "https://cfdidescargamasiva.clouda.sat.gob.mx/DescargaMasivaService.svc";
        static string urlDescargarSolicitudAction = "http://DescargaMasivaTerceros.sat.gob.mx/IDescargaMasivaTercerosService/Descargar";
        public static string DescargarSolicitud(
   X509Certificate2 certificate,
   string authorization,
   string idPaquete)
        {
            if (string.IsNullOrWhiteSpace(Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE")))
            {
                throw new Exception(
                    "El RFC solicitante está vacío.");
            }

            if (string.IsNullOrWhiteSpace(idPaquete))
            {
                throw new Exception(
                    "No existe un identificador " +
                    "de paquete para descargar.");
            }

            DescargarSolicitud descargarSolicitud =
                new DescargarSolicitud(
                    urlDescargarSolicitud,
                    urlDescargarSolicitudAction);

            descargarSolicitud.Generate(
                certificate,
               Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE"),
                idPaquete);

            return descargarSolicitud.Send(
                authorization);
        }
    }
}
