using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.InsertarBD
{
    internal class GuardarpaqenBD
    {
        public static void GuardarPaqueteEnBD(string idsolicitudSat, string idpaqueteSat)
        {

            string query = @"
            INSERT INTO PaquetesSAT
             (
                 solicitudid,
                 idpaquetesat
             )
             SELECT
                  s.id,
                  @idpaquetesat
            FROM SolicitudesSAT s
            WHERE
                 s.idsolicitudsat = @idsolicitudsat
            AND NOT EXISTS
             (
            SELECT 1
            FROM PaquetesSAT p
            WHERE p.idpaquetesat = @idpaquetesat
             );
           ";

            using (SqlConnection conn =
               new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@idsolicitudsat",
                    SqlDbType.VarChar,
                    100).Value =
                        idsolicitudSat;

                cmd.Parameters.Add(
                    "@idpaquetesat",
                    SqlDbType.VarChar,
                    150).Value =
                        idpaqueteSat;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
