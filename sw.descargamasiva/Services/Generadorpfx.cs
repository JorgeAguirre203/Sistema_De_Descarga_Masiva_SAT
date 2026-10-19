using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
namespace sw.descargamasiva.Services
{
    internal class Generadorpfx
    {

        static string Cfg(string k) => ConfigurationManager.AppSettings[k] ?? "";
        static string NomCarpetaPfx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        static string carpetaFiel = Cfg("FielCarpeta");
        static string rutaSello = Path.Combine(carpetaFiel, Cfg("FielCer"));
        static string rutaLlave = Path.Combine(carpetaFiel, Cfg("FielKey"));
        static string clavePrivada = Cfg("FielClave");
        static string password = Cfg("FielClave");

        //genera archivo pfx a partir de los archivos .cer y .key, ademas de la clave privada, si no se puede generar el pfx devuelve false
        public static bool GenerarPfx()
        {
            try
            {
                if (string.IsNullOrEmpty(NomCarpetaPfx))
                {
                    Console.WriteLine("Aviso: No se especificó la carpeta para guardar el pfx");
                    return false;
                }

                if (!Directory.Exists(NomCarpetaPfx))
                {
                    Directory.CreateDirectory(NomCarpetaPfx);
                }

                if (!File.Exists(rutaSello))
                {
                    Console.WriteLine($"Aviso: No existe el archivo .cer en la ruta: {rutaSello}");
                    return false;
                }

                if (!File.Exists(rutaLlave))
                {
                    Console.WriteLine($"Aviso: No existe el archivo .key en la ruta: {rutaLlave}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(clavePrivada))
                {
                    Console.WriteLine("Aviso: La clave privada está vacía");
                    return false;
                }

                String pathLlaveFormatoPem = Path.Combine(NomCarpetaPfx, "llave.pem");
                String pathSelloFormatoPem = Path.Combine(NomCarpetaPfx, "sello.pem");
                String pathPfx = Path.Combine(NomCarpetaPfx, "pfx.pfx");

                if (File.Exists(pathPfx)) File.Delete(pathPfx);

                String raiz = Cfg("OpenSslCarpeta");

                Console.WriteLine("Generando archivo PEM de la llave privada...");

                EjecutarComandoOpenSsl(raiz, $"pkcs8 -inform DER -in \"{rutaLlave}\" -out \"{pathLlaveFormatoPem}\" -passin pass:\"{clavePrivada}\" -provider legacy -provider default");

                Console.WriteLine("Generando archivo PEM del certificado (.cer)...");
                EjecutarComandoOpenSsl(raiz, $"x509 -inform der -in \"{rutaSello}\" -out \"{pathSelloFormatoPem}\"");

                Console.WriteLine("Empaquetando en formato PFX...");
                EjecutarComandoOpenSsl(raiz, $"pkcs12 -export -in \"{pathSelloFormatoPem}\" -inkey \"{pathLlaveFormatoPem}\" -out \"{pathPfx}\" -passout pass:\"{clavePrivada}\" -provider legacy -provider default");

                if (File.Exists(pathPfx))
                {
                    Console.WriteLine("Se ha creado el archivo PFX exitosamente");
                }
                else
                {
                    Console.WriteLine("Error: No se pudo crear el archivo PFX. Revisa los mensajes de OpenSSL arriba.");
                    return false;
                }

                if (File.Exists(pathLlaveFormatoPem)) File.Delete(pathLlaveFormatoPem);
                if (File.Exists(pathSelloFormatoPem)) File.Delete(pathSelloFormatoPem);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepción en GenerarPfx: " + ex.Message);
                return false;
            }
        }
        private static void EjecutarComandoOpenSsl(string workingDir, string arguments)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = Path.Combine(workingDir, "openssl.exe");
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();
            string errorOutput = proc.StandardError.ReadToEnd();
            string standardOutput = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(errorOutput))
            {
                Console.WriteLine("-> OpenSSL Info/Error: " + errorOutput);
            }
        }

 public  static X509Certificate2 ObtenerX509Certificado(byte[] pfxBytes)       
        {
            return new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.PersistKeySet |
                            X509KeyStorageFlags.Exportable);
        }


        public static X509Certificate2 Cargar()
        {
            if (!GenerarPfx())
            {
                Console.WriteLine("No se pudo generar/cargar PFX.");
                return null;
            }

            string pathPfx = Path.Combine(NomCarpetaPfx, "pfx.pfx");
            if (!File.Exists(pathPfx))
            {
                Console.WriteLine("No existe pfx.pfx.");
                return null;
            }

            byte[] pfxBytes = File.ReadAllBytes(pathPfx);

            return new X509Certificate2(
                pfxBytes,
                password,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);
        }






    }
}
