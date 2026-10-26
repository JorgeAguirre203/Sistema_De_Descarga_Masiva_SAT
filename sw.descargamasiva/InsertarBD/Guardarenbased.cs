using System;
using System.Data;
using System.Data.SqlClient;

namespace sw.descargamasiva.InsertarBD
{
    internal class Guardarenbased
    {
        public static readonly string connectionString =
     System.Configuration.ConfigurationManager.ConnectionStrings["BD"].ConnectionString;


        public static int GuardarSolicitudEnBD(
            string idSolicitudSat,
            string tipo,
            DateTime fechaInicial,
            DateTime fechaFinal)
        {
            string query = @"
                IF EXISTS
                (
                    SELECT 1
                    FROM SolicitudesSAT
                    WHERE IdSolicitudSat = @IdSolicitudSat
                )
                BEGIN

                    SELECT id
                    FROM SolicitudesSAT
                    WHERE IdSolicitudSat = @IdSolicitudSat;

                END
                ELSE
                BEGIN

                    INSERT INTO SolicitudesSAT
                    (
                        idsolicitudsat,
                        tipo,
                        fechainicial,
                        fechafinal,
                        estado
                    )
                    VALUES
                    (
                        @IdSolicitudSat,
                        @tipo,
                        @fechainicial,
                        @fechafinal,
                        0
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);

                END;
            ";

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@IdSolicitudSat",
                    SqlDbType.VarChar,
                    100).Value = idSolicitudSat;

                cmd.Parameters.Add(
                    "@tipo",
                    SqlDbType.VarChar,
                    15).Value = tipo;

                cmd.Parameters.Add(
                    "@fechainicial",
                    SqlDbType.DateTime2).Value = fechaInicial;

                cmd.Parameters.Add(
                    "@fechafinal",
                    SqlDbType.DateTime2).Value = fechaFinal;

                conn.Open();

                return Convert.ToInt32(
                    cmd.ExecuteScalar());
            }
        }





        public static DateTime ObtenerUltimaConsultaPorRfc(string rfc, string tipo)
        {
            string query = @"
        SELECT UltimaConsulta
        FROM dbo.ControlConsultasPorRFC
        WHERE RFC = @rfc AND tipo = @tipo;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@rfc", SqlDbType.NVarChar, 15).Value = rfc;
                cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 15).Value = tipo;

                conn.Open();

                object resultado = cmd.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new Exception(
                        $"No existe configuración de consulta para el RFC: {rfc} y el tipo: {tipo}");
                }

                return Convert.ToDateTime(resultado);
            }
        }
        public static void ActualizarUltimaConsultaPorRfc(string rfc, string tipo, DateTime nuevaFecha)
        {
            string query = @"
        UPDATE dbo.ControlConsultasPorRFC 
        SET UltimaConsulta = @UltimaConsulta 
        WHERE RFC = @RFC   AND tipo = @tipo;
    ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
                cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 15).Value = tipo;
                cmd.Parameters.Add("@ultimaConsulta", SqlDbType.DateTime).Value = nuevaFecha;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public static string ObtenerRFC(string tipo)
        {
            string query = @"
                SELECT RFC
                FROM ControlRFCSAT
                WHERE tipo = @tipo;";         

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            { 
                cmd.Parameters.Add(
                    "@tipo",
                    SqlDbType.VarChar,
                    15).Value = tipo;

                conn.Open();

                object resultado =
                    cmd.ExecuteScalar();

                if (resultado == null) {

                    throw new Exception("No existe un rfc registrado" + tipo);

                }
                return resultado.ToString().Trim();

                  
            }
        }

        public static string ObtenerRFCSOLIC(string tipo)
        {
            string query = @"
                SELECT RFC
                FROM ControlRFCSAT
                WHERE tipo = @tipo;";

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@tipo",
                    SqlDbType.VarChar,
                    15).Value = tipo;

                conn.Open();

                object resultado =
                    cmd.ExecuteScalar();

                if (resultado == null)
                {

                    throw new Exception("No existe un rfc registrado" + tipo);

                }
                return resultado.ToString().Trim();


            }
        }

        public static string ObtenerRFCEMISOR(string tipo)
        {
            string query = @"
                SELECT RFC
                FROM ControlRFCSAT
                WHERE tipo = @tipo;";

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@tipo",
                    SqlDbType.VarChar,
                    15).Value = tipo;

                conn.Open();

                object resultado =
                    cmd.ExecuteScalar();

                if (resultado == null)
                {

                    throw new Exception("No existe un rfc registrado" + tipo);

                }
                return resultado.ToString().Trim();


            }
        }



        public static void ActualizarUltimaConsulta(
            string tipo,
            DateTime fecha)
        {
            string query = @"
                UPDATE ControlConsultasSAT
                SET ultimaConsulta = @fecha
                WHERE tipo = @tipo;
            ";

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@tipo",
                    SqlDbType.VarChar,
                    15).Value = tipo;

                cmd.Parameters.Add(
                    "@fecha",
                    SqlDbType.DateTime2).Value = fecha;

                conn.Open();

                int filas =
                    cmd.ExecuteNonQuery();

                if (filas == 0)
                {
                    throw new Exception(
                        "No se pudo actualizar ControlConsultasSAT.");
                }
            }
        }
    }
}