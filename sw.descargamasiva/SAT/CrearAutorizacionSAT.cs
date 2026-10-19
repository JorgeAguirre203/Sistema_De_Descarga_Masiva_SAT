using sw.descargamasiva.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.SAT
{
    internal class CrearAutorizacionSAT
    {
        
        private readonly X509Certificate2 certificate;

        private string authorization;

        private DateTime fechaObtencion;

        // Tiempo máximo que conservaremos el token
        private readonly TimeSpan tiempoValidez =
            TimeSpan.FromMinutes(4);


        public CrearAutorizacionSAT(
            X509Certificate2 certificate)
        {
            this.certificate = certificate
                ?? throw new ArgumentNullException(
                    nameof(certificate));
        }


        public string Obtener()
        {
           
            // SI NO EXISTE TOKEN
         

            if (string.IsNullOrWhiteSpace(authorization))
            {
                return Renovar();
            }


            
            // COMPROBAR SI YA DEBEMOS RENOVARLO
         

            TimeSpan tiempoTranscurrido =
                DateTime.Now - fechaObtencion;


            if (tiempoTranscurrido >= tiempoValidez)
            {
                return Renovar();
            }


            // TOKEN ACTUAL TODAVÍA SE CONSERVA
           

            return authorization;
        }


        private string Renovar()
        {
            Console.WriteLine();
            Console.WriteLine(
                "Obteniendo nuevo token del SAT...");


            string token =
                ObToken.ObtenerToken(certificate);


            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception(
                    "El SAT no devolvió un token de autenticación.");
            }


            authorization =
                "WRAP access_token=\"" +
                System.Web.HttpUtility.UrlDecode(token) +
                "\"";


            fechaObtencion = DateTime.Now;


            Console.WriteLine(
                "Token SAT obtenido correctamente.");


            return authorization;
        }
    }
}
