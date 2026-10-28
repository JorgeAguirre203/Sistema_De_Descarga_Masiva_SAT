using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using sw.descargamasiva.InsertarBD;

namespace sw.descargamasiva.Services
{
    internal class GuardaryDescomprimirPQ
    {
        // Carpeta raíz de todos los paquetes (bin\Debug\Paquetes).
        public static string RaizPaquetes =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Paquetes");


        public static string GuardarSolicitud(
            string idPaquete,
            string descargaResponse,
            string tipo,
            string rfc)
        {
            string carpetaTipo =
                Path.Combine(
                    RaizPaquetes,
                    LimpiarNombre(tipo));

            string rfcSolicitante =
                LimpiarNombre(rfc);

            if (string.IsNullOrWhiteSpace(rfcSolicitante))
                rfcSolicitante = "SIN_RFC";

            string carpetaGzip =
                Path.Combine(
                    carpetaTipo,
                    rfcSolicitante,

                    "GZIP");

            // Carpeta temporal exclusiva de ESTE paquete (mismo disco que el destino
            // final, así mover archivos es instantáneo).
            string carpetaTemp =
                Path.Combine(
                    carpetaTipo,
                    "_tmp",
                    LimpiarNombre(idPaquete));

            Directory.CreateDirectory(carpetaGzip);
            Directory.CreateDirectory(carpetaTemp);

            try
            {
                byte[] file =
                    Convert.FromBase64String(descargaResponse);

                string rutaGzip =
                    Path.Combine(
                        carpetaGzip,
                        idPaquete + ".gzip");

                File.WriteAllBytes(rutaGzip, file);

                Console.WriteLine(
                    "Archivo guardado en: " + rutaGzip);

                Console.WriteLine(
                    "----Se comenzará a descomprimir el paquete----");

                DescomprimirCon7Zip(rutaGzip, carpetaTemp);

                // Solo los XML de ESTE paquete (la carpeta temporal es exclusiva)
                string[] archivosXml =
                    Directory.GetFiles(
                        carpetaTemp,
                        "*.xml",
                        SearchOption.AllDirectories);

                Console.WriteLine(
                    "XML encontrados: " + archivosXml.Length);

                foreach (string archivoTemp in archivosXml)
                {
                    string idFacturaSat =
                        Path.GetFileNameWithoutExtension(archivoTemp);

                    try
                    {
                        // 1) Leer año, mes y RFC desde el propio XML
                        DatosCfdi datos = LeerDatosCfdi(archivoTemp);

                        // 2) Armar la carpeta destino
                        string carpetaXml =
                            Path.Combine(
                                carpetaTipo,
                                datos.RfcReceptor,
                                datos.Anio,
                                datos.Mes,
                                datos.RfcEmisor,
                                "XML");

                        Directory.CreateDirectory(carpetaXml);

                        string rutaFinal =
                            Path.Combine(
                                carpetaXml,
                                Path.GetFileName(archivoTemp));

                        // Si el CFDI ya existía (paquete reprocesado), se sobrescribe
                        if (File.Exists(rutaFinal))
                            File.Delete(rutaFinal);

                        // 3) Mover a su lugar definitivo
                        File.Move(archivoTemp, rutaFinal);

                        // 4) Guardar en BD
                        GuardarXmlBD.GuardarXmlEnBD(
                            idFacturaSat,
                            rutaFinal);

                        Console.WriteLine(
                            "Guardado: " + rutaFinal);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Error guardando " +
                            idFacturaSat +
                            ": " +
                            ex.Message);
                    }
                }

                return rutaGzip;
            }
            finally
            {

                try
                {
                    if (Directory.Exists(carpetaTemp))
                        Directory.Delete(carpetaTemp, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "No se pudo borrar la carpeta temporal: " +
                        ex.Message);
                }
            }
        }


        private class DatosCfdi
        {
            public string RfcReceptor = "SIN_RFC";
            public string RfcEmisor = "SIN_RFC";
            public string Anio = "SIN_FECHA";
            public string Mes = "SIN_FECHA";
        }

        // Lee Fecha, Emisor/@Rfc y Receptor/@Rfc del XML.
        // (cambia el namespace, pero los nombres son los mismos).
        // Si algo falta o el XML no se puede leer, esos campos quedan en SIN
        // para que el archivo no se pierda.
        private static DatosCfdi LeerDatosCfdi(string rutaXml)
        {
            DatosCfdi datos = new DatosCfdi();

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                XmlDocument doc = new XmlDocument();

                using (XmlReader reader =
                    XmlReader.Create(rutaXml, settings))
                {
                    doc.Load(reader);
                }

                XmlNode comprobante = doc.DocumentElement;

                // Fecha del comprobante, ej. 2025-01-10T12:34:56
                string fecha =
                    comprobante?.Attributes?["Fecha"]?.Value;

                DateTime fechaCfdi;

                if (DateTime.TryParse(
                        fecha,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out fechaCfdi))
                {
                    datos.Anio = fechaCfdi.Year.ToString("0000");
                    datos.Mes = fechaCfdi.Month.ToString("00");
                }

                XmlNode emisor =
                    doc.SelectSingleNode(
                        "/*[local-name()='Comprobante']/*[local-name()='Emisor']");

                XmlNode receptor =
                    doc.SelectSingleNode(
                        "/*[local-name()='Comprobante']/*[local-name()='Receptor']");

                string rfcEmisor =
                    LimpiarNombre(emisor?.Attributes?["Rfc"]?.Value);

                string rfcReceptor =
                    LimpiarNombre(receptor?.Attributes?["Rfc"]?.Value);

                if (!string.IsNullOrWhiteSpace(rfcEmisor))
                    datos.RfcEmisor = rfcEmisor;

                if (!string.IsNullOrWhiteSpace(rfcReceptor))
                    datos.RfcReceptor = rfcReceptor;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "No se pudieron leer los datos del XML " +
                    Path.GetFileName(rutaXml) +
                    ": " +
                    ex.Message);
            }

            return datos;
        }

        private static void DescomprimirCon7Zip(
            string rutaArchivo,
            string carpetaDestino)
        {
            string ejecutable = System.Configuration.ConfigurationManager.AppSettings["SevenZipExe"];

            if (!File.Exists(ejecutable))
            {
                throw new Exception(
                    "No se encontró 7-Zip en: " +
                    ejecutable);
            }

            using (Process proceso = new Process())
            {
                proceso.StartInfo.FileName = ejecutable;

                proceso.StartInfo.Arguments =
                    $"x \"{rutaArchivo}\" -o\"{carpetaDestino}\" -y";

                proceso.StartInfo.UseShellExecute = false;
                proceso.StartInfo.CreateNoWindow = true;
                proceso.StartInfo.RedirectStandardOutput = true;
                proceso.StartInfo.RedirectStandardError = true;

                proceso.Start();

                Task<string> salida =
                    proceso.StandardOutput.ReadToEndAsync();

                Task<string> errores =
                    proceso.StandardError.ReadToEndAsync();

                proceso.WaitForExit();

                Task.WaitAll(salida, errores);

                if (proceso.ExitCode != 0)
                {
                    throw new Exception(
                        "Error al descomprimir: " +
                        errores.Result +
                        Environment.NewLine +
                        salida.Result);
                }
            }
        }

        // Quita caracteres no válidos para nombres de carpeta
        private static string LimpiarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return string.Empty;

            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c.ToString(), "");

            return nombre.Trim();
        }
    }
}  