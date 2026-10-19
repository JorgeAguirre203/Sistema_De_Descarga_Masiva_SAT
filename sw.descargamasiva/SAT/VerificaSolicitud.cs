using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace sw.descargamasiva
{
    internal class VerificaSolicitud : SoapRequestBase
    {
        public string EstadoSolicitud { get; private set; }
        public string CodigoEstadoSolicitud { get; private set; }

        public List<string> IdsPaquetes { get; private set; }
            = new List<string>();
        public int NumeroCFDIs { get; private set; }
        public string Mensaje { get; private set; }
        public VerificaSolicitud(string url, string action)
            : base(url, action)
        {
        }

        public string Generate(
            X509Certificate2 certificate,
            string rfcSolicitante,
            string idSolicitud)
        {
            Guid identificador;

            if (!Guid.TryParse(idSolicitud, out identificador))
                throw new Exception("El IdSolicitud no tiene un formato válido.");

            return CrearPeticionFirmada(
                certificate,
                "VerificaSolicitudDescarga",
                "solicitud",
                new Dictionary<string, string>
                {
                    { "IdSolicitud", identificador.ToString("D") },
                    { "RfcSolicitante", rfcSolicitante }
                });
        }

        public override string GetResult(XmlDocument xmlDoc)
        {
            IdsPaquetes.Clear();
            EstadoSolicitud = null;
            CodigoEstadoSolicitud = null;

            XmlNode fallo = xmlDoc.SelectSingleNode(
                "//*[local-name()='Fault']");

            if (fallo != null)
                throw new Exception("Error SOAP: " + fallo.InnerText);

            XmlNode resultado = xmlDoc.SelectSingleNode(
                "//*[local-name()='VerificaSolicitudDescargaResult']");

            if (resultado == null)
                throw new Exception("La respuesta no contiene la verificación.");

            string codigo = resultado.Attributes?["CodEstatus"]?.Value;
            string mensaje = resultado.Attributes?["Mensaje"]?.Value;
            string CFDIs = resultado.Attributes?["NumeroCFDIs"]?.Value;  //declara CFDIS como cadena y obtiene el numero de cfdis 

            EstadoSolicitud =
                resultado.Attributes?["EstadoSolicitud"]?.Value;
            CodigoEstadoSolicitud =
                resultado.Attributes?["CodigoEstadoSolicitud"]?.Value;

            int numero;   // declara la varable numero
            if (int.TryParse(CFDIs, out numero))   // intenta convertir CFDIs a un entero y lo asigna a numero
                NumeroCFDIs = numero;   // si la conversion es exitosa, asigna el valor de numero a NumeroCFDIs
            else
                NumeroCFDIs = 0; //SI EL NUMERO DE CFDIS ES 0 ENTONCES IMPRIME EL MENSAJE
            Console.WriteLine(mensaje);

            Console.WriteLine("Código SAT: " + codigo);
            Console.WriteLine("Estado: " + EstadoSolicitud);
            Console.WriteLine("Código de solicitud: " + CodigoEstadoSolicitud);
            Console.WriteLine("CFDI: " + NumeroCFDIs);
            Console.WriteLine("Mensaje: " + mensaje);
            if (codigo != "5000")
                throw new Exception($"[SAT {codigo}]: {mensaje}");

            if (EstadoSolicitud == "1" || EstadoSolicitud == "2")
            {
                Console.WriteLine(
                    "Sigue pendiente. Consulta después con el mismo IdSolicitud.");
                return string.Empty;
            }

            if (CodigoEstadoSolicitud == "5004")
            {
                Console.WriteLine("No se encontraron datos para generar paquetes.");
                return string.Empty;
            }

            if (EstadoSolicitud != "3")
            {
                throw new Exception(
                    "La solicitud finalizó sin estar lista para descargar. " +
                    "Estado: " + EstadoSolicitud +
                    "; código: " + CodigoEstadoSolicitud);
            }

            XmlNodeList paquetes = resultado.SelectNodes(
                "./*[local-name()='IdsPaquetes']");

            if (paquetes != null)
            {
                foreach (XmlNode paquete in paquetes)
                {
                    string id = paquete.InnerText.Trim();

                    if (!string.IsNullOrEmpty(id) && !IdsPaquetes.Contains(id))
                        IdsPaquetes.Add(id);
                }
            }

            Console.WriteLine("Paquetes disponibles: " + IdsPaquetes.Count);

            // Send() exige devolver string; la lista completa queda
            // disponible en la propiedad IdsPaquetes.
            return IdsPaquetes.Count > 0
                ? IdsPaquetes[0]
                : string.Empty;
        }
    }
}