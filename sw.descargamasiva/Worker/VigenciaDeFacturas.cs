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


public class VigenciaFacturasWorker : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Esperar solo 10 segundos al iniciar
                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);

               
                Estadocfdi.ConsultarFacturasRecientes(6);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error en vigencia de facturas: " + ex.Message);
            }

            // Esperar 24 horas antes de la siguiente ejecución
            try
            {
                await Task.Delay(
                    TimeSpan.FromHours(24),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}