using Microsoft.Extensions.Hosting;
using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace sw.descargamasiva.Worker
{
    internal class SolicitudesPendientes
    {
        public class PrimerProcesoWorker : BackgroundService
        {
            // Cada cuánto se revisan las solicitudes pendientes de todos los RFC.
            private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

            protected override async Task ExecuteAsync(
                CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
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

                    // Cada RFC revisa SUS pendientes con SU FIEL.
                    foreach (string rfc in rfcs)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        X509Certificate2 certificate = null;

                        try
                        {
                            Console.WriteLine();
                            Console.WriteLine("Pendientes del RFC: " + rfc);

                            certificate =
                                Generadorpfx.Cargar(rfc);

                            CrearAutorizacionSAT autenticacion =
                                new CrearAutorizacionSAT(certificate);

                            string authorization =
                                autenticacion.Obtener();

                            InsertarRevisarBD.ProcesarSolicitudesPendientes(
                                certificate,
                                authorization);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "ERROR PROCESANDO SOLICITUDES (RFC " + rfc + "):");
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            // Libera la llave privada de la FIEL.
                            if (certificate != null)
                            {
                                certificate.Dispose();
                            }
                        }
                    }

                    try
                    {
                        await Task.Delay(Intervalo, stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        // El servicio se está deteniendo.
                        break;
                    }
                }
            }
        }
    }
}