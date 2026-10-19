using Microsoft.Extensions.Hosting;
using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sw.descargamasiva.Worker
{
    internal class SolicitudesPendientes
    {
        public class PrimerProcesoWorker : BackgroundService
        {
            protected override async Task ExecuteAsync(
                CancellationToken stoppingToken)
            {
                X509Certificate2 certificate =
                    Generadorpfx.Cargar();


                if (certificate == null)
                {
                    Console.WriteLine(
                        "No se pudo cargar el certificado.");

                    throw new InvalidOperationException("No se pudo cargar el certificado.");
                }


                CrearAutorizacionSAT autenticacion =
                    new CrearAutorizacionSAT(certificate);


                while (!stoppingToken.IsCancellationRequested)
                {
                    try




                    {
                        await Task.Delay(
                            TimeSpan.FromHours(12),
                            stoppingToken);


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
                            "ERROR PROCESANDO SOLICITUDES:");

                        Console.WriteLine(
                            ex.Message);
                    }
                }
            }
        }
    }
}
