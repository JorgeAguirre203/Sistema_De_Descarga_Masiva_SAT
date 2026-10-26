using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.Services
{
    internal class Orquestador
    {
        public void OrquestarPrograma()
        {
            List<string> listaRfcs = ProcesadorRFC.ExtraerRFC();

            Console.WriteLine($"RFCs activos encontrados: {listaRfcs.Count}");

            foreach (string rfc in listaRfcs)
            {
                try
                {
                    Console.WriteLine($"\n----------------------------------------");
                    Console.WriteLine($"Procesando Actualmente el RFC : {rfc}");
                    Console.WriteLine($"----------------------------------------");

                    // 1. Cargamos el certificado correspondiente al RFC actual del ciclo
                    using (X509Certificate2 certificado = Generadorpfx.Cargar(rfc))
                    {
                        string authorization = Program.ObtenerAuthorization(certificado);

                        CrearAutorizacionSAT autenticacion = new CrearAutorizacionSAT(certificado);

                        DateTime fechaInicial;
                        DateTime fechaFinal;
                        string RfcReceptor;
                        string RfcEmisor;
                        string RfcSolicitante;

                        string idSolicitud = GenerarSolici.GenerarSolicitudRecibidos(
    rfc,
    certificado,
    authorization,
    out fechaInicial,
    out fechaFinal,
    out RfcReceptor,
    out RfcEmisor,
    out RfcSolicitante);

                        if (string.IsNullOrEmpty(idSolicitud))
                        {
                            Console.WriteLine($"[AVISO] No se generó ID de solicitud para el RFC {rfc}. Pasando al siguiente.");
                            continue;
                        }

                        Console.WriteLine("\nID SOLICITUD RECIBIDOS:");
                        Console.WriteLine(idSolicitud);

                        int idSolicitudBD = Guardarenbased.GuardarSolicitudEnBD(
                            idSolicitud,
                            "RECIBIDO",
                            fechaInicial,
                            fechaFinal);

                        Console.WriteLine("\nSolicitud registrada en BD.");
                        Console.WriteLine("ID interno: " + idSolicitudBD);

                        InsertarRevisarBD.RevisarYDescargarSolicitud(
                            certificado,
                            authorization,
                            idSolicitud,
                            "RECIBIDO");
                    }
                }
                catch (Exception ex)
                {
                    // Capturamos el error de este RFC en específico para que el foreach NO se detenga y siga con el siguiente
                    Console.WriteLine($"[ERROR] Ocurrió un error al procesar el RFC {rfc}: {ex.Message}");
                }
            }
        }
    }

    internal class ProcesadorRFC
    {
        public static List<string> ExtraerRFC()
        {
            List<string> rfcList = new List<string>();

            string query = "SELECT DISTINCT RFC FROM ControlRFCSAT WHERE Activo = 1;";

            using (SqlConnection conn = new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string rfc = (reader["RFC"] as string ?? "").Trim().ToUpper();

                        if (!string.IsNullOrEmpty(rfc) && !rfcList.Contains(rfc))
                        {
                            rfcList.Add(rfc);
                        }
                    }
                }
            }

            return rfcList;
        }
    }
}