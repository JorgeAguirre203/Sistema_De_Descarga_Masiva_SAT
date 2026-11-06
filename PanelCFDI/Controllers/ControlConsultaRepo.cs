using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

// Acceso a datos del panel. Es la única clase que habla con SQL Server.
public static class ControlConsultaRepo
{
    // Cadena "DescargaMasiva" definida en Web.config
    static readonly string cn =
        ConfigurationManager.ConnectionStrings["DescargaMasiva"].ConnectionString;


    // ---------- Fecha de búsqueda por RFC (la que usa el proceso) ----------

    // Lee las fechas de ControlConsultasPorRFC (una fila por RFC + tipo)
    public static List<object> ListarPorRfc()
    {
        var lista = new List<object>();

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(
            "SELECT RFC, tipo, UltimaConsulta FROM dbo.ControlConsultasPorRFC ORDER BY RFC, tipo", conn))
        {
            conn.Open();

            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    lista.Add(new
                    {
                        rfc = Convert.ToString(r["RFC"]).Trim(),
                        tipo = Convert.ToString(r["tipo"]).Trim(),
                        fecha = r["UltimaConsulta"] == DBNull.Value
                            ? ""
                            : Convert.ToDateTime(r["UltimaConsulta"]).ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
        }

        return lista;
    }

    // Cambia la fecha de TODOS los tipos del RFC (RECIBIDO, EMITIDO, SOLICITANTE).
    // Si el RFC no tiene filas, las crea. Devuelve cuántas filas quedaron con la fecha.
    public static int FijarPorRfc(string rfc, DateTime fecha)
    {
        const string sql = @"
            UPDATE dbo.ControlConsultasPorRFC
            SET UltimaConsulta = @fecha
            WHERE RFC = @rfc;

            DECLARE @n INT = @@ROWCOUNT;

            IF @n = 0
            BEGIN
                INSERT INTO dbo.ControlConsultasPorRFC (RFC, tipo, UltimaConsulta)
                VALUES (@rfc, 'RECIBIDO', @fecha),
                       (@rfc, 'EMITIDO', @fecha),
                       (@rfc, 'SOLICITANTE', @fecha);

                SET @n = 3;
            END
                
                INSERT INTO ControlRFCSAT (RFC, tipo, activo)
                VALUES (@rfc, 'RECIBIDO', 1),
                       (@rfc, 'EMISOR', 1),
                       (@rfc, 'SOLICITANTE', 1);

                SET @n = 3;
            

            SELECT @n;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = fecha;
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    public static int EliminaRFC(string rfc)
    {
        const string sql = @"
        
            DELETE FROM dbo.ControlConsultasPorRFC WHERE RFC = @rfc;
            DELETE FROM dbo.ControlRFCSAT          WHERE RFC = @rfc;
        ";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc.Trim().ToUpperInvariant();
            conn.Open();
            return cmd.ExecuteNonQuery();   // total de filas borradas
        }
    }


    // ---------- RFC por tipo (ControlRFCSAT) ----------

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