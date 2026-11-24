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



    public static int FijarIntervaloPorRfc(string rfc, int intervalo)
    {
        const string sql = @"
        UPDATE dbo.ControlConsultasPorRFC
        SET intervalo = @intervalo
        WHERE RFC = @rfc;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@intervalo", SqlDbType.Int).Value = intervalo;
            conn.Open();
            return cmd.ExecuteNonQuery();
        }
    }







    //================================= NUEVO CODIGO AQUIIIIIIIIIII ================================
    public static int FijartiempoWorker(string tipo, int tiempo)
    {
        string columna;

        switch (tipo)
        {
            case "Recibidos": columna = "tiempoRecibidos"; break;
            case "Emitidos": columna = "tiempoEmitidos"; break;
            case "Pendientes": columna = "tiempoPendientes"; break;
            case "Vigencia": columna = "tiempoVigencia"; break;
            default:
                throw new ArgumentException("Tipo no válido.");
        }

        // La columna sale de una lista cerrada (switch), por eso es seguro concatenarla
        string query = "UPDATE dbo.appconfig SET " + columna + " = @tiempo WHERE id = 1;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.Add("@tiempo", SqlDbType.Int).Value = tiempo;
            conn.Open();

            int filas = cmd.ExecuteNonQuery();

            if (filas == 0)
                throw new Exception("No existe configuración en appconfig (id = 1).");

            return filas;
        }
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

            -- Solo da de alta el RFC en ControlRFCSAT si todavía no existe
            -- (antes insertaba 3 filas más en cada llamada).
            IF NOT EXISTS (SELECT 1 FROM dbo.ControlRFCSAT WHERE RFC = @rfc)
                INSERT INTO dbo.ControlRFCSAT (RFC, tipo, activo)
                VALUES (@rfc, 'RECIBIDO', 1),
                       (@rfc, 'EMISOR', 1),
                       (@rfc, 'SOLICITANTE', 1);

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

    // ---------- FIEL por RFC (ControlRFCSAT) ----------

    public class FilaFiel
    {
        public string Rfc { get; set; }
        public string Cer { get; set; }
        public string Key { get; set; }
        public string Carpeta { get; set; }
        public bool TieneClave { get; set; }
        public bool ArchivosOk { get; set; }
    }

    // El RFC existe si está en la tabla de fechas o en ControlRFCSAT
    public static bool ExisteRfcSat(string rfc)
    {
        const string sql = @"
            SELECT COUNT(*) FROM (
                SELECT RFC FROM dbo.ControlConsultasPorRFC WHERE RFC = @rfc
                UNION ALL
                SELECT RFC FROM dbo.ControlRFCSAT WHERE RFC = @rfc
            ) x;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.NVarChar, 15).Value = rfc;
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }

    public static List<FilaFiel> ListarFiel()
    {
        // Los RFC salen de la tabla de fechas (la misma que ves en "Fecha de búsqueda")
        // y de ControlRFCSAT; la FIEL se une si ya existe.
        const string sql = @"
            SELECT r.RFC,
                   MAX(s.RutaCer)         AS RutaCer,
                   MAX(s.RutaKey)         AS RutaKey,
                   MAX(s.RutaCarpetafiel) AS Carpeta,
                   MAX(CASE WHEN s.ClavePrivadaProtegida IS NULL OR s.ClavePrivadaProtegida = '' THEN 0 ELSE 1 END) AS TieneClave
            FROM (SELECT RFC FROM dbo.ControlConsultasPorRFC
                  UNION
                  SELECT RFC FROM dbo.ControlRFCSAT) r
            LEFT JOIN dbo.ControlRFCSAT s ON s.RFC = r.RFC
            GROUP BY r.RFC
            ORDER BY r.RFC;";

        var lista = new List<FilaFiel>();

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            conn.Open();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    lista.Add(new FilaFiel
                    {
                        Rfc = Convert.ToString(r["RFC"]).Trim(),
                        Cer = r["RutaCer"] == DBNull.Value ? "" : Convert.ToString(r["RutaCer"]).Trim(),
                        Key = r["RutaKey"] == DBNull.Value ? "" : Convert.ToString(r["RutaKey"]).Trim(),
                        Carpeta = r["Carpeta"] == DBNull.Value ? "" : Convert.ToString(r["Carpeta"]).Trim(),
                        TieneClave = Convert.ToInt32(r["TieneClave"]) == 1
                    });
                }
            }
        }

        return lista;
    }

    // Actualiza la FIEL en TODAS las filas del RFC (RECIBIDO, EMISOR, SOLICITANTE).
    // Generadorpfx exige que todas coincidan.
    public static int GuardarFiel(string rfc, string carpeta, string cer, string key, string claveProtegida)
    {
        // Actualiza las filas del RFC; si no tiene ninguna en ControlRFCSAT, las crea.
        const string sql = @"
            UPDATE dbo.ControlRFCSAT
            SET RutaCer = @cer,
                RutaKey = @key,
                ClavePrivadaProtegida = @clave,
                RutaCarpetafiel = @carpeta
            WHERE RFC = @rfc;

            IF @@ROWCOUNT = 0
                INSERT INTO dbo.ControlRFCSAT (RFC, tipo, Activo, RutaCer, RutaKey, ClavePrivadaProtegida, RutaCarpetafiel)
                VALUES (@rfc, 'RECIBIDO',    1, @cer, @key, @clave, @carpeta),
                       (@rfc, 'EMISOR',      1, @cer, @key, @clave, @carpeta),
                       (@rfc, 'SOLICITANTE', 1, @cer, @key, @clave, @carpeta);

            SELECT COUNT(*) FROM dbo.ControlRFCSAT WHERE RFC = @rfc;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@rfc", SqlDbType.NVarChar, 15).Value = rfc;
            cmd.Parameters.Add("@cer", SqlDbType.NVarChar, 260).Value = cer;
            cmd.Parameters.Add("@key", SqlDbType.NVarChar, 260).Value = key;
            cmd.Parameters.Add("@carpeta", SqlDbType.NVarChar, 260).Value = carpeta;
            cmd.Parameters.Add("@clave", SqlDbType.NVarChar, 1000).Value = claveProtegida;
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