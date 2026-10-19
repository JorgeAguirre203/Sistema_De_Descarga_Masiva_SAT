using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace sw.descargamasiva.InsertarBD
{
    internal class GuardarXmlBD
    {
        //esta funcion es la encargada de inyectar los datos en la base de datos, esta funcion es temporal y estara sujeta a cambios
        public static void GuardarXmlEnBD(string idfactura, string rutaArchivoXml)
        {
            string connectionString = Guardarenbased.connectionString;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(rutaArchivoXml);


            string contenidoXml = xmlDoc.DocumentElement.OuterXml; //lee el texto xml

            string query = "INSERT INTO DatosXML (id_factura, Archivoxml, fecha_guardado) VALUES (@idfactura, @xml, @fecha)"; // hace el INSERT
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@idfactura", SqlDbType.NVarChar, 50).Value = idfactura;  //parametros de la tabla
                cmd.Parameters.Add("@xml", SqlDbType.Xml).Value = contenidoXml;
                cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = DateTime.Now;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
