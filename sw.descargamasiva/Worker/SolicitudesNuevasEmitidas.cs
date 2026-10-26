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
    internal class SolicitudesNuevasEmitidas
    {
        public class tercerprocesoworker : BackgroundService
        {
            protected override async Task ExecuteAsync(
                CancellationToken stoppingToken)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = false;

                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine();
                    Console.WriteLine("====================================");
                    Console.WriteLine("SOLICITUD DE CFDI EMITIDOS");
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

                            // Antes llamaba a GenerarSolicitudRecibidos por error.
                            string idSolicitud =
                                GenerarSolicEmit.GenerarSolicitudEmitidos(
                                    rfc,
                                    certificate,
                                    authorization,
                                    out fechaInicial,
                                    out fechaFinal);

                            Console.WriteLine();
                            Console.WriteLine("ID SOLICITUD EMITIDOS:");
                            Console.WriteLine(idSolicitud);

                            int idSolicitudBD =
                                Guardarenbased.GuardarSolicitudEnBD(
                                    idSolicitud,
                                    "EMITIDO",
                                    fechaInicial,
                                    fechaFinal);

                            Console.WriteLine();
                            Console.WriteLine("Solicitud registrada en BD.");
                            Console.WriteLine("ID interno: " + idSolicitudBD);

                            InsertarRevisarBD.RevisarYDescargarSolicitud(
                                certificate,
                                authorization,
                                idSolicitud,
                                "EMITIDO");
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

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}