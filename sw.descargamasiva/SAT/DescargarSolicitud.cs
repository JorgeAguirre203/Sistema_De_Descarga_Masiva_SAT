using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace sw.descargamasiva
{
    internal class DescargarSolicitud : SoapRequestBase
    {
        public DescargarSolicitud(string url, string action)
            : base(url, action)
        {
        }

        public string Generate(
            X509Certificate2 certificate,
            string rfcSolicitante,
            string idPaquete)
        {
            return CrearPeticionFirmada(
                certificate,
                "PeticionDescargaMasivaTercerosEntrada",
                "peticionDescarga",
                new Dictionary<string, string>
                {
                    { "IdPaquete", idPaquete },
                    { "RfcSolicitante", rfcSolicitante }
                });
        }

        public override string GetResult(XmlDocument xmlDoc)
        {
            XmlNode fallo = xmlDoc.SelectSingleNode(
                "//*[local-name()='Fault']");

            if (fallo != null)
                throw new Exception("Error SOAP de descarga: " + fallo.InnerText);

            // El estatus puede venir en la cabecera SOAP.
            XmlNode estado = xmlDoc.SelectSingleNode(
                "//*[@CodEstatus]");

            if (estado == null)
                throw new Exception(
                    "La respuesta de descarga no contiene CodEstatus.");

            string codigo = estado.Attributes?["CodEstatus"]?.Value;
            string mensaje = estado.Attributes?["Mensaje"]?.Value;

            Console.WriteLine("Código de descarga: " + codigo);
            Console.WriteLine("Mensaje de descarga: " + mensaje);

            if (codigo != "5000")
                throw new Exception($"[SAT descarga {codigo}]: {mensaje}");

            XmlNode paquete = xmlDoc.SelectSingleNode(
                "//*[local-name()='Paquete']");

            if (paquete == null || string.IsNullOrWhiteSpace(paquete.InnerText))
                throw new Exception(
                    "El SAT respondió sin contenido de paquete.");

            return paquete.InnerText.Trim();
        }
    }
}