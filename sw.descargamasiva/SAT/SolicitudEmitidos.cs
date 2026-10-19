using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;
namespace sw.descargamasiva
{
    internal class SolicitudEmitidos : SoapRequestBase
    {
        public SolicitudEmitidos(string url, string action)
            : base(url, action)
        {
        }


        public string Generate(
            X509Certificate2 certificate,
            string rfcSolicitante,
            string rfcEmisor,
            DateTime fechaInicial,
            DateTime fechaFinal,
            string tipoSolicitud = "CFDI")
        {
            if (certificate == null)
            {
                throw new Exception("No se encontro el certificado");
            }
            if (!certificate.HasPrivateKey)
                throw new Exception(
                    "El certificado no tiene clave privada");
            if (string.IsNullOrWhiteSpace(rfcSolicitante))
                throw new Exception(
                    "El RFC del solicitante no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(rfcEmisor))
                throw new Exception(
                    "El RFC del emisor no puede estar vacío.");
            if (fechaInicial > fechaFinal)
                throw new Exception(
                    "La fecha inicial no puede ser mayor a la fecha final.");

            //se le da a atributos los datos necesrios para formar el xml mediante un diccionario
            Dictionary<string, string> atributos = new Dictionary<string, string>
            {
                { "RfcSolicitante", rfcSolicitante },
                { "RfcEmisor", rfcEmisor },
                { "FechaInicial", fechaInicial.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "FechaFinal", fechaFinal.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "TipoSolicitud", tipoSolicitud }
            };

            return CrearPeticionFirmada(
                certificate,
                "SolicitaDescargaEmitidos",
                "solicitud",
                atributos);

        }

        public override string GetResult(
           XmlDocument xmlDoc)
        {
            XmlNode fallo = xmlDoc.SelectSingleNode(
                "//*[local-name()='Fault']");

            if (fallo != null)
            {
                throw new Exception(
                    "Error SOAP al solicitar emitidos: " +
                    fallo.InnerText);
            }

            XmlNode resultado =
                xmlDoc.SelectSingleNode(
                    "//*[local-name()=" +
                    "'SolicitaDescargaEmitidosResult']");

            if (resultado == null)
            {
                throw new Exception(
                    "No se encontró " +
                    "SolicitaDescargaEmitidosResult.");
            }

            string codigo =
                resultado.Attributes?["CodEstatus"]?.Value;

            string mensaje =
                resultado.Attributes?["Mensaje"]?.Value;

            string idSolicitud =
                resultado.Attributes?["IdSolicitud"]?.Value;

            Console.WriteLine(
                "Código SAT: " + codigo);

            Console.WriteLine(
                "Mensaje SAT: " + mensaje);

            Console.WriteLine(
                "IdSolicitud: " + idSolicitud);

            if (codigo != "5000")
            {
                throw new Exception(
                    "[SAT Error " + codigo + "]: " +
                    mensaje);
            }

            if (string.IsNullOrWhiteSpace(idSolicitud))
            {
                throw new Exception(
                    "El SAT aceptó la solicitud, " +
                    "pero no regresó IdSolicitud.");
            }

            return idSolicitud;
        }
    }
}

    

