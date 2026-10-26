using Microsoft.Extensions.Hosting;
using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using System;
using System.Security.Cryptography.X509Certificates;
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
                while (!stoppingToken.IsCancellationRequested)
                {
                    X509Certificate2 certificate = null;

                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromHours(12),
                            stoppingToken);


                        // La FIEL sale de ControlRFCSAT. Se carga en cada
                        // vuelta; si falla, solo se registra el error y
                        // se reintenta en la siguiente vuelta.
                        string rfcFiel =
                            Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE");

                        certificate =
                            Generadorpfx.Cargar(rfcFiel);

                        CrearAutorizacionSAT autenticacion =
                            new CrearAutorizacionSAT(certificate);

                        string authorization =
                            autenticacion.Obtener();

                       
                           


                        InsertarRevisarBD.ProcesarSolicitudesPendientes(
                            certificate,
                            authorization
                          );
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        // El servicio se está deteniendo.
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();

                        Console.WriteLine(
                            "ERROR PROCESANDO SOLICITUDES:");

                        Console.WriteLine(
                            ex.Message);
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
            }
        }
    }
}