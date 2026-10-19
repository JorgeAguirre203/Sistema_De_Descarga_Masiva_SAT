using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Numerics;
using System.Globalization;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Web.UI.WebControls;
namespace sw.descargamasiva
{
    internal class Solicitud : SoapRequestBase
    {
        public Solicitud(string url, string action)
           : base(url, action)
        {
        }

        #region Crea el XML para enviar
        public string Generate(X509Certificate2 certificate, string rfcEmisor, string rfcReceptor, string rfcSolicitante, string fechaInicial = "", string fechaFinal = "", string tipoSolicitud = "CFDI")
        {
            FixFecha(fechaInicial, fechaFinal, out fechaInicial, out fechaFinal);

           
            string rfcReceptorAttr = string.IsNullOrEmpty(rfcReceptor) ? "" : " RfcReceptor=\"" + rfcReceptor + "\"";

           
            string canonicalTimestamp = "<des:SolicitaDescargaRecibidos xmlns:des=\"http://DescargaMasivaTerceros.sat.gob.mx\">"
                + "<des:solicitud Id=\"_0\" RfcEmisor=\"" + rfcEmisor + "\"" + rfcReceptorAttr + " RfcSolicitante=\"" + rfcSolicitante + "\" FechaInicial=\"" + fechaInicial + "\" FechaFinal=\"" + fechaFinal + "\" TipoSolicitud=\"" + tipoSolicitud + "\">"
                + "</des:solicitud>"
                + "</des:SolicitaDescargaRecibidos>";

            string digest = CreateDigest(canonicalTimestamp);

            string canonicalSignedInfo = "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                                            "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></CanonicalizationMethod>" +
                                            "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\"></SignatureMethod>" +
                                            "<Reference URI=\"#_0\">" +
                                               "<Transforms>" +
                                                  "<Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></Transform>" +
                                               "</Transforms>" +
                                               "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"></DigestMethod>" +
                                               "<DigestValue>" + digest + "</DigestValue>" +
                                            "</Reference>" +
                                         "</SignedInfo>";

            string signature = Sign(canonicalSignedInfo, certificate);
            string numeroSerieDecimal = BigInteger.Parse(
                "0" + certificate.SerialNumber,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            Console.WriteLine("Serie Hexadecimal: " + certificate.SerialNumber);
            Console.WriteLine("Serie Decimal: " + numeroSerieDecimal);
            // 2. Sobre SOAP final con las etiquetas de apertura y cierre corregidas
            string soap_request = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:des=\"http://DescargaMasivaTerceros.sat.gob.mx\" xmlns:xd=\"http://www.w3.org/2000/09/xmldsig#\">" +
                        "<s:Header/>" +
                        "<s:Body>" +
                            "<des:SolicitaDescargaRecibidos>" +
                                "<des:solicitud Id=\"_0\" RfcEmisor=\"" + rfcEmisor + "\"" +
                                rfcReceptorAttr +
                                " RfcSolicitante=\"" + rfcSolicitante +
                                "\" FechaInicial=\"" + fechaInicial +
                                "\" FechaFinal=\"" + fechaFinal +
                                "\" TipoSolicitud=\"" + tipoSolicitud + "\">" +
                                    "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                                    "<SignedInfo>" +
                                    "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>" +
                                    "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\"/>" +
                                    "<Reference URI=\"#_0\">" +
                                        "<Transforms>" +
                                        "<Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>" +
                                        "</Transforms>" +
                                        "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"/>" +
                                        "<DigestValue>" + digest + "</DigestValue>" +
                                    "</Reference>" +
                                    "</SignedInfo>" +
                                    "<SignatureValue>" + signature + "</SignatureValue>" +
                                    "<KeyInfo>" +
                                        "<X509Data>" +
                                            "<X509IssuerSerial>" +
                                                "<X509IssuerName>" + certificate.Issuer + "</X509IssuerName>" +
                                                "<X509SerialNumber>" + numeroSerieDecimal + "</X509SerialNumber>" +
                                            "</X509IssuerSerial>" +
                                            "<X509Certificate>" + Convert.ToBase64String(certificate.RawData) + "</X509Certificate>" +
                                        "</X509Data>" +
                                    "</KeyInfo>" +
                                    "</Signature>" +
                                "</des:solicitud>" +
                            "</des:SolicitaDescargaRecibidos>" +
                        "</s:Body>" +
                    "</s:Envelope>";
            XmlDocument documento = new XmlDocument();
            documento.PreserveWhitespace = true;
            documento.LoadXml(soap_request);
            // Localizar la solicitud y la firma manual.
            XmlNamespaceManager nsFirma = new XmlNamespaceManager(
                documento.NameTable);

            nsFirma.AddNamespace(
                "des", "http://DescargaMasivaTerceros.sat.gob.mx");

            nsFirma.AddNamespace(
                "ds", SignedXml.XmlDsigNamespaceUrl);

            XmlElement solicitudFirmar = documento.SelectSingleNode(
                "//des:SolicitaDescargaRecibidos/des:solicitud",
                nsFirma) as XmlElement;

            if (solicitudFirmar == null)
                throw new Exception("No se encontró el elemento solicitud.");
            // Configurar la búsqueda de recibidos ANTES de firmar.
            solicitudFirmar.SetAttribute("RfcReceptor", rfcReceptor);
            solicitudFirmar.SetAttribute("RfcSolicitante", rfcSolicitante);
            solicitudFirmar.SetAttribute("EstadoComprobante", "Vigente");

            // Un emisor vacío significa que no aplicamos ese filtro.
            if (string.IsNullOrWhiteSpace(rfcEmisor))
            {
                solicitudFirmar.RemoveAttribute("RfcEmisor");
            }
            else
            {
                solicitudFirmar.SetAttribute("RfcEmisor", rfcEmisor);
            }

            XmlElement firmaAnterior = solicitudFirmar.SelectSingleNode(
                "ds:Signature", nsFirma) as XmlElement;

            if (firmaAnterior == null)
                throw new Exception("No se encontró la firma anterior.");

            XmlElement datosCertificado = firmaAnterior.SelectSingleNode(
                "ds:KeyInfo", nsFirma) as XmlElement;

            if (datosCertificado == null)
                throw new Exception("No se encontró KeyInfo.");

            // Conservar los datos del certificado y la serie decimal.
            KeyInfo informacionClave = new KeyInfo();
            informacionClave.LoadXml(datosCertificado);

            // Eliminar la firma construida manualmente.
            solicitudFirmar.RemoveChild(firmaAnterior);
            solicitudFirmar.SetAttribute("Id", "_0");

            using (RSA clave = certificate.GetRSAPrivateKey())
            {
                if (clave == null)
                    throw new Exception("No se encontró una llave privada RSA.");

                SignedXml firmador = new SignedXml(documento);
                firmador.SigningKey = clave;

                firmador.SignedInfo.CanonicalizationMethod =
                    SignedXml.XmlDsigExcC14NTransformUrl;

                firmador.SignedInfo.SignatureMethod =
                    SignedXml.XmlDsigRSASHA1Url;

                Reference referencia = new Reference("#_0");
                referencia.DigestMethod = SignedXml.XmlDsigSHA1Url;

                referencia.AddTransform(
                    new XmlDsigEnvelopedSignatureTransform());

                referencia.AddTransform(
                    new XmlDsigExcC14NTransform());

                firmador.AddReference(referencia);
                firmador.KeyInfo = informacionClave;
                firmador.ComputeSignature();

                solicitudFirmar.AppendChild(
                    documento.ImportNode(firmador.GetXml(), true));
            }

            // Este es el XML actualizado que se validará y enviará.
            soap_request = documento.OuterXml;

            XmlNamespaceManager namespaces =
                new XmlNamespaceManager(documento.NameTable);

            namespaces.AddNamespace(
                "ds", "http://www.w3.org/2000/09/xmldsig#");

            XmlElement nodoFirma = documento.SelectSingleNode("//ds:Signature", namespaces) as XmlElement;

            if (nodoFirma == null)
            {
                throw new Exception("No se encontró el nodo de firma en el documento XML.");
            }
               SignedXml verificador = new SignedXml(documento);
            verificador.LoadXml(nodoFirma);

            string algoritmo = verificador.SignedInfo.SignatureMethod;
            string esperado = SignedXml.XmlDsigRSASHA1Url;


            Console.WriteLine("Algoritmo leido: " + algoritmo);
            Console.WriteLine("Algoritmo esperado: " + esperado);
            Console.WriteLine("coincidden " +
                string.Equals(algoritmo, esperado, StringComparison.Ordinal));
            Object implementacion = CryptoConfig.CreateFromName(algoritmo);

            Console.WriteLine("implementacion" +
                (implementacion == null
                ? "no encontrada"
                : implementacion.GetType().FullName));
            Console.WriteLine("Es signature description:"+ 
                (implementacion is SignatureDescription));

            bool firmaValida = verificador.CheckSignature(certificate, true);
            if (!firmaValida)
            {
                throw new Exception("La firma digital no es válida.");
            }
            xml = soap_request;
            return soap_request;
        }
        #endregion

        private void FixFecha(string fechaInicial1, string fechaFinal1, out string fechaInicial, out string fechaFinal)
        {
            fechaInicial = fechaInicial1 ;
            fechaFinal = fechaFinal1 ;
        }

        public override string GetResult(XmlDocument xmlDoc)
        {
            XmlNode resultado = xmlDoc.SelectSingleNode(
                "//*[local-name()='SolicitaDescargaRecibidosResult']");

            if (resultado == null)
                throw new Exception(
                    "No se encontró el resultado de la solicitud.");

            string codigo = resultado.Attributes?["CodEstatus"]?.Value;
            string mensaje = resultado.Attributes?["Mensaje"]?.Value;
            string idSolicitud = resultado.Attributes?["IdSolicitud"]?.Value;

            Console.WriteLine("Código SAT: " + codigo);
            Console.WriteLine("Mensaje SAT: " + mensaje);

            if (codigo != "5000")
                throw new Exception(
                    $"[SAT Error {codigo}]: {mensaje}");

            if (string.IsNullOrWhiteSpace(idSolicitud))
                throw new Exception(
                    "El SAT aceptó la solicitud, pero no devolvió IdSolicitud.");

            return idSolicitud;
        }
    }
}