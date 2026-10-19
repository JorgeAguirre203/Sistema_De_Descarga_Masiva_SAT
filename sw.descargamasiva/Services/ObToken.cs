using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.Services
{
    internal class ObToken
    {
        static string urlAutentica = "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/Autenticacion/Autenticacion.svc";
        static string urlAutenticaAction = "http://DescargaMasivaTerceros.gob.mx/IAutenticacion/Autentica";
        
        //posible MOVIMIENTO POR REDUCCION DE CODIGO A CARPETA SERVICE 28/9/26
        public static string ObtenerToken(X509Certificate2 certifcate)
        {
            Autenticacion service = new Autenticacion(urlAutentica, urlAutenticaAction);   //funcion que obtiene el token de autorizacion del sat, recibe 2 parametros url y urlaction
            string xml = service.Generate(certifcate);
            return service.Send();
        }

    }
}
