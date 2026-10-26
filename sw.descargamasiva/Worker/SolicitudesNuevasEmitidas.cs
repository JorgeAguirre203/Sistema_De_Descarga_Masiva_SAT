using Microsoft.Extensions.Hosting;
using sw.descargamasiva.InsertarBD;
using sw.descargamasiva.SAT;
using sw.descargamasiva.Services;
using System;
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
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12;

                ServicePointManager.Expect100Continue =
                    false;

                while (!stoppingToken.IsCancellationRequested)
                {
                    X509Certificate2 certificate = null;

                    try
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "====================================");

                        Console.WriteLine(
                            "SOLICITUD DE CFDI RECIBIDOS");

                        Console.WriteLine(
                            "====================================");


                        // La FIEL sale de ControlRFCSAT.
                        string rfcFiel =
                            Guardarenbased.ObtenerRFCSOLIC("SOLICITANTE");

                        certificate =
                            Generadorpfx.Cargar(rfcFiel);

                        CrearAutorizacionSAT autenticacion =
                            new CrearAutorizacionSAT(certificate);

                        string authorization =
                            autenticacion.Obtener();


                        DateTime fechaInicial;
                        DateTime fechaFinal;


                        string idSolicitud =
                            GenerarSolici.GenerarSolicitudRecibidos(
                                certificate,
                                authorization,
                                out fechaInicial,
                                out fechaFinal,
                                out string RfcReceptor,
                                out string RfcEmisor,
                                out string RfcSolicitante);


                        Console.WriteLine();
                        Console.WriteLine(
                            "ID SOLICITUD EMITIDOS:");

                        Console.WriteLine(
                            idSolicitud);


                        int idSolicitudBD =
                            Guardarenbased.GuardarSolicitudEnBD(
                                idSolicitud,
                                "EMITIDO",
                                fechaInicial,
                                fechaFinal);


                        Console.WriteLine();

                        Console.WriteLine(
                            "Solicitud registrada en BD.");

                        Console.WriteLine(
                            "ID interno: " +
                            idSolicitudBD);


                        InsertarRevisarBD.RevisarYDescargarSolicitud(
                            certificate,
                            authorization,
                            idSolicitud,
                            "EMITIDO");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();

                        Console.WriteLine(
                            "ERROR EN EL PROCESO:");

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


                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        stoppingToken);
                }
            }
        }

    }
}