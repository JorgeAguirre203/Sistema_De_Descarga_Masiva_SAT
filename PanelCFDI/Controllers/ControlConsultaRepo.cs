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
    public static void Fijar(string tipo, DateTime fecha)
    {
        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(
            "UPDATE ControlConsultasSAT SET ultimaConsulta = @fecha WHERE tipo = @tipo", conn))
        {
            cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 15).Value = tipo;
            cmd.Parameters.Add("@fecha", SqlDbType.DateTime2).Value = fecha;
            conn.Open();

            if (cmd.ExecuteNonQuery() == 0)
                throw new Exception("No existe configuración para el tipo " + tipo);
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