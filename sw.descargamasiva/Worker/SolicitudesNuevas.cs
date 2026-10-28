using Microsoft.Extensions.Hosting;
using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace sw.descargamasiva.Worker
{
    internal class SolicitudesNuevas
    { 
        public class segundoprocesoworker : BackgroundService
        {
            // Cada cuánto se piden CFDI nuevos para todos los RFC.
            private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(2);
           
            protected override async Task ExecuteAsync(
                CancellationToken stoppingToken)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = false;

                

                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine();
                    Console.WriteLine("====================================");
                    Console.WriteLine("SOLICITUD DE CFDI RECIBIDOS");
                    Console.WriteLine("====================================");

                    List<string> rfcs;

                    try
                    {
                        rfcs = ProcesadorRFC.ExtraerRFC();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR leyendo RFCs activos: " + ex.Message);
                        rfcs = new List<string>();
                    }

                    // Un RFC con error no detiene a los demás.
                    foreach (string rfc in rfcs)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        X509Certificate2 certificate = null;

                        try
                        {
                            Console.WriteLine();
                            Console.WriteLine("RFC: " + rfc);

                            certificate = Generadorpfx.Cargar(rfc);

                            CrearAutorizacionSAT autenticacion =
                                new CrearAutorizacionSAT(certificate);

                            string authorization = autenticacion.Obtener();

                            DateTime fechaInicial;
                            DateTime fechaFinal;
                            string RfcReceptor;
                            string RfcEmisor;
                            string RfcSolicitante;

                            string idSolicitud =
                                GenerarSolici.GenerarSolicitudRecibidos(
                                    rfc,
                                    certificate,
                                    authorization,
                                    out fechaInicial,
                                    out fechaFinal,
                                    out RfcReceptor,
                                    out RfcEmisor,
                                    out RfcSolicitante);

                            // null = el RFC ya está al día, no se pidió nada
                            if (string.IsNullOrEmpty(idSolicitud))
                                continue;

                            Console.WriteLine();
                            Console.WriteLine("ID SOLICITUD RECIBIDOS:");
                            Console.WriteLine(idSolicitud);

                            int idSolicitudBD =
                                Guardarenbased.GuardarSolicitudEnBD(
                                    idSolicitud,
                                    "RECIBIDO",
                                    fechaInicial,
                                    fechaFinal,
                                    rfc);

                            Console.WriteLine();
                            Console.WriteLine("Solicitud registrada en BD.");
                            Console.WriteLine("ID interno: " + idSolicitudBD);

                            InsertarRevisarBD.RevisarYDescargarSolicitud(
                                certificate,
                                authorization,
                                idSolicitud,
                                "RECIBIDO");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            Console.WriteLine("ERROR EN EL PROCESO (RFC " + rfc + "):");
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (certificate != null)
                            {
                                certificate.Dispose();
                            }
                        }
                    }

                    await Task.Delay(Intervalo, stoppingToken);
                }
            }
        }
    }
}