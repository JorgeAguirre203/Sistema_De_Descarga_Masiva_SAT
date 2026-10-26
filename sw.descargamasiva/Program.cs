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
       
       
            static void Main(string[] args)
            {
                Console.WriteLine("====================================");
                Console.WriteLine("      DESCARGA MASIVA SAT           ");
                Console.WriteLine("====================================");

                try
                {
                    // 1. Instanciamos la clase Orquestador
                    Orquestador orquestador = new Orquestador();

                    // 2. Llamamos al método para que comience todo el flujo
                    orquestador.OrquestarPrograma();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nOcurrió un error crítico en el proceso: {ex.Message}");
                }

                Console.WriteLine("\nProceso terminado.");
                Console.WriteLine("Presione cualquier tecla para salir...");
                Console.ReadKey();
            }

            // Aquí se mantienen tus métodos auxiliares de Program.cs (como ObtenerAuthorization, etc.)
           
        
    

    public static string ObtenerAuthorization(X509Certificate2 certificate)
        {
            string token = ObToken.ObtenerToken(certificate);

            return string.Format(
                "WRAP access_token=\"{0}\"",
                HttpUtility.UrlDecode(token));
        }
    }
}