using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using sw.descargamasiva.InsertarBD;

namespace sw.descargamasiva.Services
{

    public class EstatusSat
    {
        public string CodigoEstatus;        // "S - Comprobante obtenido satisfactoriamente." / "N - 601: ..." / "N - 602: ..."
        public string EsCancelable;         // "Cancelable sin aceptación", "Cancelable con aceptación", "No cancelable"
        public string Estado;               // "Vigente", "Cancelado", "No Encontrado"
        public string EstatusCancelacion;   // "En proceso", "Plazo vencido", "Cancelado sin aceptación"... (puede venir vacío)
        public string ValidacionEfos;       // "200" = no está en lista 69-B
    }

    internal class Estadocfdi
    {

      
        const string Url =
            "https://consultaqr.facturaelectronica.sat.gob.mx/ConsultaCFDIService.svc";

        const string SoapAction =
            "http://tempuri.org/IConsultaCFDIService/Consulta";


        public static string ArmarExpresion(
            string uuid,
            string rfcEmisor,
            string rfcReceptor,
            decimal total,
            string version,
            string sello8)
        {
            string tt = total.ToString("0000000000.000000", CultureInfo.InvariantCulture);
            string expresion =
               "?id=" + uuid +
               "&re=" + rfcEmisor +
               "&rr=" + rfcReceptor +
               "&tt=" + tt;

            if ((version == "4.0" || version == "3.3") && !string.IsNullOrEmpty(sello8))
                expresion += "&fe=" + sello8;

            return expresion;

        }
        public static void ConsultarFacturasRecientes(int meses)
        {
            List<CfdiPendiente> facturas = CfdiPendiente.ExtraerDatosXML(meses);
            Console.WriteLine("Facturas encontradas: " + facturas.Count);

            foreach (CfdiPendiente f in facturas)
            {
                try
                {
                    string expresion = ArmarExpresion(
                        f.IdFactura, f.RfcEmisor, f.RfcReceptor,
                        f.Total, f.Version, f.Sello8);

                    EstatusSat estatus = Consultar(expresion);

                    GuardarEstado(f, estatus);   // <-- guarda en EstadoCfdiSAT

                    Console.WriteLine(f.IdFactura + " -> " + estatus.Estado + " | " + estatus.CodigoEstatus);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(f.IdFactura + " -> ERROR: " + ex.Message);

                    try { GuardarError(f, ex.Message); }
                    catch (Exception ex2)
                    {
                        Console.WriteLine("   No se pudo registrar el error en BD: " + ex2.Message);
                    }
                }
            }
        }
        public static EstatusSat Consultar(string expresion)
        {
            string soap =
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<tem:Consulta>" +
                "<tem:expresionImpresa><![CDATA[" + expresion + "]]></tem:expresionImpresa>" +
                "</tem:Consulta>" +
                "</soapenv:Body>" +
                "</soapenv:Envelope>";
            byte[] datos = Encoding.UTF8.GetBytes(soap);

            HttpWebRequest peticion = (HttpWebRequest)WebRequest.Create(Url);
            peticion.Method = "POST";
            peticion.ContentType = "text/xml; charset=utf-8";
            peticion.Headers.Add("SOAPAction", SoapAction);
            peticion.ContentLength = datos.Length;
            peticion.Timeout = 30000; // 30 segundos
            peticion.ReadWriteTimeout = 30000; // 30 segundos
              
            using (Stream salida = peticion.GetRequestStream())
            {
                salida.Write(datos, 0, datos.Length);
            }

            string respuesta;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)peticion.GetResponse())
                using (StreamReader lector = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    respuesta = lector.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                string detalle = "";

                if (ex.Response != null)
                {
                    using (StreamReader lector = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        detalle = lector.ReadToEnd();
                    }
                }

                if (detalle.Length > 300)
                    detalle = detalle.Substring(0, 300);

                throw new Exception("Error HTTP al consultar al SAT: " + ex.Message + " " + detalle);
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(respuesta);

            EstatusSat resultado = new EstatusSat
            {
                CodigoEstatus = Valor(doc, "CodigoEstatus"),
                EsCancelable = Valor(doc, "EsCancelable"),
                Estado = Valor(doc, "Estado"),
                EstatusCancelacion = Valor(doc, "EstatusCancelacion"),
                ValidacionEfos = Valor(doc, "ValidacionEFOS")
            };

            if (string.IsNullOrEmpty(resultado.Estado) && string.IsNullOrEmpty(resultado.CodigoEstatus))
                throw new Exception("Respuesta inesperada del SAT.");

            return resultado;
        }

       
        static string Valor(XmlDocument doc, string nombre)
        {
            XmlNode nodo = doc.SelectSingleNode("//*[local-name()='" + nombre + "']");
            return nodo == null ? "" : nodo.InnerText.Trim();
        }


        static void GuardarEstado(CfdiPendiente f, EstatusSat e)
        {
            using (SqlConnection conn = new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GuardarEstadoCfdiSAT", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@IdFactura", SqlDbType.NVarChar, 50).Value = f.IdFactura.ToUpper();
                cmd.Parameters.Add("@RfcEmisor", SqlDbType.VarChar, 13).Value = f.RfcEmisor;
                cmd.Parameters.Add("@RfcReceptor", SqlDbType.VarChar, 13).Value = f.RfcReceptor;

                SqlParameter pTotal = cmd.Parameters.Add("@Total", SqlDbType.Decimal);
                pTotal.Precision = 18;
                pTotal.Scale = 6;
                pTotal.Value = f.Total;

                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = e.Estado;
                cmd.Parameters.Add("@EsCancelable", SqlDbType.VarChar, 60).Value = e.EsCancelable;
                cmd.Parameters.Add("@EstatusCancelacion", SqlDbType.VarChar, 60).Value = e.EstatusCancelacion;
                cmd.Parameters.Add("@ValidacionEfos", SqlDbType.VarChar, 10).Value = e.ValidacionEfos;
                cmd.Parameters.Add("@CodigoEstatus", SqlDbType.VarChar, 200).Value = e.CodigoEstatus;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        static void GuardarError(CfdiPendiente f, string mensaje)
        {
            if (mensaje.Length > 400)
                mensaje = mensaje.Substring(0, 400);

            using (SqlConnection conn = new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_RegistrarErrorCfdiSAT", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@IdFactura", SqlDbType.NVarChar, 50).Value = f.IdFactura.ToUpper();
                cmd.Parameters.Add("@RfcEmisor", SqlDbType.VarChar, 13).Value = f.RfcEmisor;
                cmd.Parameters.Add("@RfcReceptor", SqlDbType.VarChar, 13).Value = f.RfcReceptor;

                SqlParameter pTotal = cmd.Parameters.Add("@Total", SqlDbType.Decimal);
                pTotal.Precision = 18;
                pTotal.Scale = 6;
                pTotal.Value = f.Total;

                cmd.Parameters.Add("@Error", SqlDbType.NVarChar, 400).Value = mensaje;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

    }
}
