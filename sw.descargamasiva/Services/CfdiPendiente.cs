using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using sw.descargamasiva.InsertarBD;

namespace sw.descargamasiva.Services
{
    public class CfdiPendiente
    {
        public string IdFactura;
        public string RfcEmisor;
        public string RfcReceptor;
        public decimal Total;
        public string Version;
        public string Sello8;

        public static List<CfdiPendiente> ExtraerDatosXML(int meses)
        {
            List<CfdiPendiente> facturas = new List<CfdiPendiente>();

           
            using (SqlConnection conn =
                new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_ObtenerFacturasRecientes", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.Add("@Meses", System.Data.SqlDbType.Int).Value = meses;

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        facturas.Add(new CfdiPendiente
                        {
                            IdFactura = reader["IdFactura"].ToString(),
                            RfcEmisor = reader["RfcEmisor"].ToString(),
                            RfcReceptor = reader["RfcReceptor"].ToString(),
                            Total = Convert.ToDecimal(reader["Total"]),
                            Version = reader["Version"].ToString(),
                            Sello8 = reader["Sello8"].ToString()
                        });
                    }
                }
            }

            return facturas;
        }
    }
}
