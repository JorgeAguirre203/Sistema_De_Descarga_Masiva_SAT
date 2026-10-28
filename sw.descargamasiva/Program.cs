using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sw.descargamasiva.Services;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using static sw.descargamasiva.Worker.SolicitudesNuevasEmitidas;
using static sw.descargamasiva.Worker.SolicitudesNuevas;
using static sw.descargamasiva.Worker.SolicitudesPendientes;


namespace sw.descargamasiva
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("      DESCARGA MASIVA SAT           ");
            Console.WriteLine("====================================");

            if (args.Contains("--una-vez"))
            {
                try
                {
                    new Orquestador().OrquestarPrograma();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nOcurrió un error crítico en el proceso: {ex.Message}");
                }

                Console.WriteLine("\nProceso terminado.");
                Console.WriteLine("Presione cualquier tecla para salir...");
                Console.ReadKey();
                return;
            }

            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<segundoprocesoworker>();   // CFDI recibidos
                    services.AddHostedService<tercerprocesoworker>();    // CFDI emitidos
                    services.AddHostedService<PrimerProcesoWorker>();    // solicitudes pendientes
                    services.AddHostedService<VigenciaFacturasWorker>(); // vigencia de CFDI
                })
                .Build()
                .Run();
        }

        public static string ObtenerAuthorization(X509Certificate2 certificate)
        {
            string token = ObToken.ObtenerToken(certificate);

            return string.Format(
                "WRAP access_token=\"{0}\"",
                HttpUtility.UrlDecode(token));
        }
    }
}