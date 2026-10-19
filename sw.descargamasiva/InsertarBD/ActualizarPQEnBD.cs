using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.InsertarBD
{
    internal class ActualizarPQEnBD
    {
        public static void ActualizarPaqueteEnBD(string idPaqueteSat, bool descargado, bool descomprimido, string rutaArchivo, string error)

        {
            string query = @"
        UPDATE PaquetesSAT

        SET
            descargado = @descargado,
            descomprimido = @descomprimido,
            rutaarchivo = @rutaarchivo,

            fechadescarga =
                CASE
                    WHEN @descargado = 1
                    THEN SYSDATETIME()
                    ELSE fechadescarga
                END,

            error = @error

        WHERE
            idpaquetesat =
                @idpaquetesat;
    ";

            using (SqlConnection conn =                        //SE CREA UNA NUEVA CONEXION A LA BASE DE DATOS CON EL STRING DE CONEXION
                new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                  "@idpaquetesat",
                  SqlDbType.VarChar,
                  100).Value =
                      idPaqueteSat;

                cmd.Parameters.Add(
                    "@descargado",
                    SqlDbType.Bit).Value =
                        descargado;
                ;
                cmd.Parameters.Add(
                    "@descomprimido",
                    SqlDbType.Bit).Value =
                        descomprimido;
                cmd.Parameters.Add(
                    "@rutaArchivo",
                    SqlDbType.NVarChar,
                    500).Value =
                        string.IsNullOrWhiteSpace(
                            rutaArchivo)
                        ? (object)DBNull.Value
                : rutaArchivo;

                cmd.Parameters.Add(
                    "@error",
                    SqlDbType.NVarChar,
                    1000).Value =
                        string.IsNullOrWhiteSpace(
                            error)
                        ? (object)DBNull.Value
                        : error;

                conn.Open();
                cmd.ExecuteNonQuery();

            }
        }
    }
}
