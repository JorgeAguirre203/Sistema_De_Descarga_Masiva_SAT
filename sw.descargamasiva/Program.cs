using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sw.descargamasiva.Services;
using sw.descargamasiva.Worker;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using static sw.descargamasiva.Worker.SolicitudesNuevasEmitidas;
using static sw.descargamasiva.Worker.SolicitudesNuevas;
using static sw.descargamasiva.Worker.SolicitudesPendientes;

namespace sw.descargamasiva
{
    class Program
    {
        // Nombre con el que se registra en services.msc
        public const string NombreServicio = "DescargaMasivaSAT";

        static async Task Main(string[] args)
        {
           
            if (!Environment.UserInteractive)
                LogArchivo.Iniciar();

            Console.WriteLine("====================================");
            Console.WriteLine("      DESCARGA MASIVA SAT");
            Console.WriteLine("====================================");

            try
            {
               
                var builder = Host.CreateApplicationBuilder(
                    new HostApplicationBuilderSettings
                    {
                        Args = args,
                        ContentRootPath = AppContext.BaseDirectory
                    });

               
                builder.Services.AddWindowsService(opciones =>
                {
                    opciones.ServiceName = NombreServicio;
                });
                builder.Services.AddHostedService<VigenciaFacturasWorker>();
                builder.Services.AddHostedService<PrimerProcesoWorker>();    // revisa pendientes y descarga
                builder.Services.AddHostedService<segundoprocesoworker>();   // solicita al SAT y guarda
               

                var host = builder.Build();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("EL SERVICIO SE DETUVO POR UN ERROR:");
                Console.WriteLine(ex.ToString());

            
                throw;
            }

            Console.WriteLine("Proceso terminado.");
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