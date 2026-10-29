using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

// Acceso a datos del panel. Es la única clase que habla con SQL Server.
public static class ControlConsultaRepo
{
    // Cadena "DescargaMasiva" definida en Web.config
    static readonly string cn =
        ConfigurationManager.ConnectionStrings["DescargaMasiva"].ConnectionString;

    // Lee ultimaConsulta del tipo indicado (null si no hay fila o está vacía)
    public static DateTime? Obtener(string tipo)
    {
        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(
            "SELECT ultimaConsulta FROM ControlConsultasSAT WHERE tipo = @tipo", conn)) //obtiene la ultima consulta para la descarga automtia y extrae la fecha
        {
            cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 15).Value = tipo;
            conn.Open();
            object r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(r);
        }
    }

    // Cambia ultimaConsulta (equivale al UPDATE que hacías en SSMS)
    public static int FijarPorRFC(string rfc, DateTime fecha)
    {
       const string sql = @"
            UPDATE ControlConsultasPorRFC
            SET ultimaConsulta = @fecha
            WHERE RFC = @rfc;

               DECLARE @n INT = @@ROWCOUNT;
    
            IF @n = 0
                BEGIN
                    INSERT INTO ControlConsultasPorRFC (RFC, tipo, UltimaConsulta)
                           VALUES (@rfc, 'RECIBIDO', @fecha),
                                  (@rfc, 'EMITIDO', @fecha),
                                  (@rfc, 'SOLICITANTE', @fecha);
                
            SET @n = 3;
            END
            SELECT @n;";
        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = fecha;
            conn.Open();
            return
                Convert.ToInt32(cmd.ExecuteScalar());

        }

    }
    public static void FijarRfc(string tipo, string rfc)
    {
        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(
            "UPDATE ControlRFCSAT SET RFC = @rfc WHERE tipo = @tipo", conn))
        {
            cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 15).Value = tipo;   
            cmd.Parameters.Add("@rfc", SqlDbType.NVarChar, 15).Value = rfc;
            conn.Open();

            if (cmd.ExecuteNonQuery() == 0)
                throw new Exception("No existe configuración para el tipo " + tipo);
        }
    }

   
}