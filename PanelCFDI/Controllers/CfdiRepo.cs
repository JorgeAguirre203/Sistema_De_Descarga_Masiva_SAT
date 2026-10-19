using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

// Fila de comprobante que se envía al panel
public class CfdiFila
{
    public string Uuid;
    public string Fecha;
    public string Emisor;
    public string Rfc;
    public string Tipo;
    public decimal Total;
}

// Lecturas del panel: comprobantes (DatosXML) y solicitudes al SAT (SolicitudesSAT).
public static class CfdiRepo
{
    static readonly string cn =
        ConfigurationManager.ConnectionStrings["DescargaMasiva"].ConnectionString;

    // ---------- Comprobantes ----------
    // Los datos (fecha, emisor, tipo, total) se extraen con XQuery de la columna
    // XML Archivoxml. El TOP se aplica por fecha_guardado (los más recientes).
    public static List<CfdiFila> ListarCfdi(int max)
    {
        const string sql = @"
            SELECT TOP (@max)
                d.id_factura AS uuid,
                d.Archivoxml.value('(/*:Comprobante/@Fecha)[1]', 'varchar(19)')                    AS fecha,
                d.Archivoxml.value('(/*:Comprobante/*:Emisor/@Nombre)[1]', 'nvarchar(300)')        AS emisor,
                d.Archivoxml.value('(/*:Comprobante/*:Emisor/@Rfc)[1]', 'varchar(13)')             AS rfc,
                d.Archivoxml.value('(/*:Comprobante/@TipoDeComprobante)[1]', 'varchar(1)')         AS tipo,
                d.Archivoxml.value('(/*:Comprobante/@Total)[1]', 'decimal(18,2)')                  AS total
            FROM DatosXML d
            ORDER BY d.fecha_guardado DESC;";

        var lista = new List<CfdiFila>();

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@max", SqlDbType.Int).Value = max;
            conn.Open();

            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    lista.Add(new CfdiFila
                    {
                        Uuid = r["uuid"] as string ?? "",
                        Fecha = r["fecha"] as string ?? "",
                        Emisor = r["emisor"] as string ?? "",
                        Rfc = r["rfc"] as string ?? "",
                        Tipo = NombreTipo(r["tipo"] as string),
                        Total = r["total"] == DBNull.Value ? 0m : (decimal)r["total"]
                    });
                }
            }
        }

        return lista;
    }

    // Devuelve el XML completo de un CFDI (o null si no existe)
    public static string ObtenerXml(string uuid)
    {
        const string sql =
            "SELECT TOP 1 CAST(Archivoxml AS nvarchar(max)) FROM DatosXML WHERE id_factura = @uuid";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@uuid", SqlDbType.NVarChar, 50).Value = uuid;
            conn.Open();
            object r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? null : (string)r;
        }
    }

    static string NombreTipo(string t)
    {
        switch (t)
        {
            case "I": return "Ingreso";
            case "E": return "Egreso";
            case "P": return "Pago";
            case "N": return "Nómina";
            case "T": return "Traslado";
            default: return t ?? "";
        }
    }

    // ---------- Solicitudes al SAT ----------
    public static List<object> ListarSolicitudes(int max)
    {
        const string sql = @"
            SELECT TOP (@max)
                idsolicitudsat, fechainicial, fechafinal, estado, codigoestado, numerocfdis
            FROM SolicitudesSAT
            ORDER BY id DESC;";

        var lista = new List<object>();

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.Add("@max", SqlDbType.Int).Value = max;
            conn.Open();

            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int estado = r["estado"] == DBNull.Value ? 0 : Convert.ToInt32(r["estado"]);
                    string codigo = r["codigoestado"] == DBNull.Value ? "" : Convert.ToString(r["codigoestado"]);
                    string idSat = r["idsolicitudsat"] as string ?? "";
                    int cfdi = r["numerocfdis"] == DBNull.Value ? 0 : Convert.ToInt32(r["numerocfdis"]);

                    string texto = NombreEstado(estado, codigo);
                    if (estado == 3) texto += " (" + cfdi + " CFDI)";

                    lista.Add(new
                    {
                        id = idSat.Length >= 8 ? idSat.Substring(0, 8) : idSat,
                        desde = Convert.ToDateTime(r["fechainicial"]).ToString("yyyy-MM-dd HH:mm"),
                        hasta = Convert.ToDateTime(r["fechafinal"]).ToString("yyyy-MM-dd HH:mm"),
                        estado = texto
                    });
                }
            }
        }

        return lista;
    }

    // Estados del SAT: 1 Aceptada, 2 En proceso, 3 Terminada, 4 Error, 5 Rechazada, 6 Vencida.
    // Rechazada con código 5004 = "no se encontró información" (no es un fallo).
    static string NombreEstado(int estado, string codigo)
    {
        switch (estado)
        {
            case 0: return "Registrada";
            case 1: return "Aceptada";
            case 2: return "En proceso";
            case 3: return "Terminada";
            case 4: return "Error";
            case 5: return codigo == "5004" ? "Sin CFDI" : "Rechazada";
            case 6: return "Vencida";
            default: return "Estado " + estado;
        }
    }

    // ---------- Resumen / encabezado ----------
    public static object ObtenerInfo()
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM DatosXML)                         AS total,
                (SELECT MAX(fecha_guardado) FROM DatosXML)              AS ultima,
                (SELECT MAX(fechasolicitud) FROM SolicitudesSAT)        AS ultimaSolicitud,
                (SELECT TOP 1 Archivoxml.value('(/*:Comprobante/*:Receptor/@Rfc)[1]', 'varchar(13)')
                   FROM DatosXML ORDER BY fecha_guardado DESC)          AS rfc;";

        using (var conn = new SqlConnection(cn))
        using (var cmd = new SqlCommand(sql, conn))
        {
            conn.Open();

            using (var r = cmd.ExecuteReader())
            {
                r.Read();

                int total = Convert.ToInt32(r["total"]);
                string ultima = r["ultima"] == DBNull.Value
                    ? "—"
                    : Convert.ToDateTime(r["ultima"]).ToString("dd/MM/yyyy HH:mm");

                string estado = "Sin información del proceso";
          
                if (r["ultimaSolicitud"] != DBNull.Value)
                {
                    DateTime us = Convert.ToDateTime(r["ultimaSolicitud"]);
                    bool activo = (DateTime.Now - us).TotalMinutes <= 30;

                    estado = (activo ? "Proceso activo" : "Sin actividad reciente")
                           + " · última solicitud " + us.ToString("dd/MM/yyyy HH:mm");
                }

                return new
                {
                    total = total,
                    ultima = ultima,
                    estado = estado,
                    rfc = r["rfc"] as string ?? ""
                };
            }
        }
    }
}