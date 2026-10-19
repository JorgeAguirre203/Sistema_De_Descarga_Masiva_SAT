using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
namespace sw.descargamasiva
{
    internal abstract class SoapRequestBase
    {
        protected string xml;

        protected HttpWebRequest webRequest;

        protected SoapRequestBase(string url, string SOAPAction)
        {
            this.xml = null;
            webRequest = WebRequest(url, SOAPAction);
        }

        protected SoapRequestBase(Uri URL, string SOAPAction)
        {
            this.xml = null;
            webRequest = WebRequest(URL.ToString(), SOAPAction);
        }

        public string Send(string autorization = null)
        {
            try
            {
                if (xml == null)
                    throw new Exception("No se ha proporcionado ningún valor a la propiedad \"xml\"");

                HttpWebRequest request = webRequest;
                if (!string.IsNullOrEmpty(autorization))
                {
                    request.Headers.Add(HttpRequestHeader.Authorization, autorization);
                }
                using (Stream stream = request.GetRequestStream())
                {
                    using (StreamWriter stmw = new StreamWriter(stream))
                    {
                        stmw.Write(xml);
                    }
                }

                WebResponse response = (WebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string result = sr.ReadToEnd();
                sr.Close();
                Console.WriteLine("La respuesta del sat ante mi peticion es :" + result); //uso la variable result para mostrar el resultado de la respuesta del servidor del satt ante mi token


                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                return GetResult(xmlDoc);

            }
            catch (WebException e)
            {
                Console.WriteLine("ERROR HTTP:" + e.Message);

                if (e.Response != null)
                {
                    using (WebResponse respuesta = e.Response)
                    using (Stream flujo = respuesta.GetResponseStream())
                    {
                        if (flujo != null)
                        {
                            using (StreamReader lector = new StreamReader(flujo))
                            {
                                string detalle = lector.ReadToEnd();
                                Console.WriteLine("lA RESPUESTA DEL SAT ES  :" + detalle);    //AQUI MUESTRO LOS PROBLEMAS QUE ME DIO EL SAT ANTE MI PETICION DE DESCARGA DE EMITIDOS
                            }
                        }

                    }
                }
                else
                {
                    Console.WriteLine("No hay respuesta del servidor.");

                }
          
                throw;
            }
        }

        protected string CrearPeticionFirmada(
X509Certificate2 certificado,
string operacion,
string nombrePeticion,
Dictionary<string, string> atributos)
        {
            if (certificado == null || !certificado.HasPrivateKey)
                throw new Exception("Se requiere un certificado con llave privada.");

            const string soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            const string satNs = "http://DescargaMasivaTerceros.sat.gob.mx";
            const string dsNs = SignedXml.XmlDsigNamespaceUrl;

            XmlDocument documento = new XmlDocument();
            documento.PreserveWhitespace = true;

            XmlElement sobre = documento.CreateElement("s", "Envelope", soapNs);
            documento.AppendChild(sobre);

            sobre.AppendChild(documento.CreateElement("s", "Header", soapNs));

            XmlElement cuerpo = documento.CreateElement("s", "Body", soapNs);
            sobre.AppendChild(cuerpo);

            XmlElement elementoOperacion =
                documento.CreateElement("des", operacion, satNs);
            cuerpo.AppendChild(elementoOperacion);

            XmlElement peticion =
                documento.CreateElement("des", nombrePeticion, satNs);
            elementoOperacion.AppendChild(peticion);

            peticion.SetAttribute("Id", "_0");

            foreach (KeyValuePair<string, string> atributo in atributos)
            {
                if (string.IsNullOrWhiteSpace(atributo.Value))
                    throw new Exception("Falta el atributo: " + atributo.Key);

                peticion.SetAttribute(atributo.Key, atributo.Value.Trim());
            }

            // Construir KeyInfo mediante nodos XML.
            XmlElement keyInfoXml = documento.CreateElement("KeyInfo", dsNs);
            XmlElement x509Data = documento.CreateElement("X509Data", dsNs);
            keyInfoXml.AppendChild(x509Data);

            XmlElement issuerSerial =
                documento.CreateElement("X509IssuerSerial", dsNs);
            x509Data.AppendChild(issuerSerial);

            XmlElement issuerName =
                documento.CreateElement("X509IssuerName", dsNs);
            issuerName.InnerText = certificado.Issuer;
            issuerSerial.AppendChild(issuerName);

            XmlElement serialNumber =
                documento.CreateElement("X509SerialNumber", dsNs);
            serialNumber.InnerText = BigInteger.Parse(
                "0" + certificado.SerialNumber,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture
            ).ToString(CultureInfo.InvariantCulture);
            issuerSerial.AppendChild(serialNumber);

            XmlElement certificadoXml =
                documento.CreateElement("X509Certificate", dsNs);
            certificadoXml.InnerText =
                Convert.ToBase64String(certificado.RawData);
            x509Data.AppendChild(certificadoXml);

            KeyInfo informacionClave = new KeyInfo();
            informacionClave.LoadXml(keyInfoXml);

            using (RSA clave = certificado.GetRSAPrivateKey())
            {
                if (clave == null)
                    throw new Exception("No se encontró una llave privada RSA.");

                SignedXml firmador = new SignedXml(documento);
                firmador.SigningKey = clave;
                firmador.KeyInfo = informacionClave;

                firmador.SignedInfo.CanonicalizationMethod =
                    SignedXml.XmlDsigExcC14NTransformUrl;
                firmador.SignedInfo.SignatureMethod =
                    SignedXml.XmlDsigRSASHA1Url;

                Reference referencia = new Reference("#_0");
                referencia.DigestMethod = SignedXml.XmlDsigSHA1Url;
                referencia.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                referencia.AddTransform(new XmlDsigExcC14NTransform());

                firmador.AddReference(referencia);
                firmador.ComputeSignature();

                peticion.AppendChild(
                    documento.ImportNode(firmador.GetXml(), true));
            }

            // Comprobar el XML después de serializarlo, tal como se enviará.
            string resultado = documento.OuterXml;

            XmlDocument comprobacion = new XmlDocument();
            comprobacion.PreserveWhitespace = true;
            comprobacion.LoadXml(resultado);

            XmlNamespaceManager namespaces =
                new XmlNamespaceManager(comprobacion.NameTable);
            namespaces.AddNamespace("ds", dsNs);

            XmlElement firma = comprobacion.SelectSingleNode(
                "//ds:Signature", namespaces) as XmlElement;

            if (firma == null)
                throw new Exception("No se encontró la firma generada.");

            SignedXml verificador = new SignedXml(comprobacion);
            verificador.LoadXml(firma);

            if (!verificador.CheckSignature(certificado, true))
                throw new Exception("La firma del XML no pasó la validación local.");

            Console.WriteLine("Firma local válida: " + operacion);

            xml = resultado;
            return resultado;
        }

        public abstract string GetResult(XmlDocument xmlDoc);
 
        private static HttpWebRequest WebRequest(string URL, string SOAPAction, int maxTimeMilliseconds = 120000)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(URL);
            webRequest.Timeout = maxTimeMilliseconds;//Milisecons
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.Headers.Add("SOAPAction: " + SOAPAction);
            return webRequest;
        }

        public string CreateDigest(string sourceData)
        {
            byte[] data = GetBytes(sourceData);
            return System.Convert.ToBase64String(HashAlgorithm.Create("SHA1").ComputeHash(data));
        }
        public string Sign(string sourceData, X509Certificate2 certificate)
        {
            byte[] data = GetBytes(sourceData);
            byte[] signature = null;

            using (RSA rsaCryptoServiceProvider = certificate.GetRSAPrivateKey())
            {
                signature = rsaCryptoServiceProvider.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }
            return System.Convert.ToBase64String(signature);
        }
        private byte[] GetBytes(string sourceData)
        {
            return Encoding.Default.GetBytes(sourceData);
        }
    }
}
