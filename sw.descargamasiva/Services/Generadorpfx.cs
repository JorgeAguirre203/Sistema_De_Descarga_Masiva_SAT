using sw.descargamasiva.InsertarBD;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace sw.descargamasiva.Services
{
    internal class Generadorpfx
    {
        // =========================================================
        // CONFIGURACION (solo lo que sigue viniendo de App.config)
        // La FIEL (rutas y contraseña) ya NO se lee de App.config:
        // sale de la tabla dbo.ControlRFCSAT.
        // =========================================================

        private static string Cfg(string k)
        {
            return ConfigurationManager.AppSettings[k] ?? "";
        }

        private static readonly string NomCarpetaPfx =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources");

        // Cabecera fija de cualquier blob protegido con DPAPI
        // (GUID del proveedor: D08C9DDF-0115-D111-8C7A-00C04FC297EB).
        private static readonly byte[] CabeceraDpapi =
            { 0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF };


        // =========================================================
        // DATOS FIEL OBTENIDOS DESDE CONTROLRFCSAT
        // =========================================================

        private sealed class DatosFiel
        {
            public string RutaCer { get; set; }

            public string RutaKey { get; set; }

            public string ClavePrivadaProtegida { get; set; }

            public string RutaCarpetafiel { get; set; }
        }


        // =========================================================
        // OBTENER CONFIGURACION FIEL DE UN RFC
        //
        // Un RFC puede tener varias filas activas (EMISOR, RECIBIDO,
        // SOLICITANTE) que comparten la misma FIEL. DISTINCT las junta
        // en una sola; solo falla si las filas apuntan a FIEL distintas.
        // =========================================================

        private static DatosFiel ObtenerDatosFiel(string rfc)
        {
            const string query = @"
                SELECT DISTINCT
                    RutaCer,
                    RutaKey,
                    ClavePrivadaProtegida,
                    RutaCarpetafiel
                FROM dbo.ControlRFCSAT
                WHERE RFC = @RFC
                  AND Activo = 1;";

            using (SqlConnection conn =
                new SqlConnection(Guardarenbased.connectionString))
            using (SqlCommand cmd =
                new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@RFC",
                    SqlDbType.NVarChar,
                    15).Value = rfc;

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException(
                            "No existe una configuración FIEL activa " +
                            "para el RFC: " + rfc);
                    }

                    DatosFiel datos = new DatosFiel
                    {
                        RutaCer =
                            Convert.ToString(reader["RutaCer"]),

                        RutaKey =
                            Convert.ToString(reader["RutaKey"]),

                        ClavePrivadaProtegida =
                            Convert.ToString(
                                reader["ClavePrivadaProtegida"]),

                        RutaCarpetafiel =
                            Convert.ToString(
                                reader["RutaCarpetafiel"])
                    };

                    if (reader.Read())
                    {
                        throw new InvalidOperationException(
                            "El RFC " + rfc + " tiene filas activas " +
                            "con FIEL distinta (rutas o contraseña " +
                            "diferentes). Todas deben coincidir.");
                    }

                    if (string.IsNullOrWhiteSpace(datos.RutaCer))
                    {
                        throw new InvalidOperationException(
                            "RutaCer está vacía para el RFC: " + rfc);
                    }

                    if (string.IsNullOrWhiteSpace(datos.RutaKey))
                    {
                        throw new InvalidOperationException(
                            "RutaKey está vacía para el RFC: " + rfc);
                    }

                    if (string.IsNullOrWhiteSpace(
                        datos.ClavePrivadaProtegida))
                    {
                        throw new InvalidOperationException(
                            "ClavePrivadaProtegida está vacía para el RFC: " +
                            rfc);
                    }

                    return datos;
                }
            }
        }


        // =========================================================
        // RESOLVER LAS RUTAS DE LOS ARCHIVOS FIEL
        // =========================================================

        private static string ResolverRuta(
            string carpeta,
            string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
            {
                throw new InvalidOperationException(
                    "La ruta de un archivo FIEL está vacía.");
            }

            rutaArchivo = rutaArchivo.Trim();

            // Si RutaCer o RutaKey ya es absoluta, se utiliza tal cual.
            if (Path.IsPathRooted(rutaArchivo))
            {
                return Path.GetFullPath(rutaArchivo);
            }

            if (string.IsNullOrWhiteSpace(carpeta))
            {
                throw new InvalidOperationException(
                    "RutaCarpetafiel está vacía y el archivo utiliza " +
                    "una ruta relativa.");
            }

            carpeta = carpeta.Trim();

            string carpetaCompleta;

            if (Path.IsPathRooted(carpeta))
            {
                carpetaCompleta = carpeta;
            }
            else
            {
                carpetaCompleta = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    carpeta);
            }

            return Path.GetFullPath(
                Path.Combine(carpetaCompleta, rutaArchivo));
        }


        // =========================================================
        // OBTENER LA CONTRASEÑA DE LA FIEL
        //
        // Acepta dos formatos en ClavePrivadaProtegida:
        //   1) Texto plano (como la tienes hoy).
        //   2) Base64 de ProtectedData.Protect con LocalMachine (DPAPI).
        //
        // Se distingue por la cabecera de DPAPI, así una contraseña
        // en texto plano que casualmente parezca Base64 no se confunde,
        // y un blob DPAPI real que no se pueda descifrar falla con un
        // mensaje claro en vez de usarse como contraseña.
        // =========================================================

        private static bool PareceDatoDpapi(byte[] datos)
        {
            if (datos == null || datos.Length < CabeceraDpapi.Length)
            {
                return false;
            }

            for (int i = 0; i < CabeceraDpapi.Length; i++)
            {
                if (datos[i] != CabeceraDpapi[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string DesprotegerClavePrivada(
            string claveProtegida)
        {
            if (string.IsNullOrWhiteSpace(claveProtegida))
            {
                throw new InvalidOperationException(
                    "No se recibió la contraseña de la FIEL.");
            }

            string valor = claveProtegida.Trim();

            byte[] datosCifrados = null;
            byte[] datosOriginales = null;

            try
            {
                try
                {
                    datosCifrados = Convert.FromBase64String(valor);
                }
                catch (FormatException)
                {
                    // No es Base64: es texto plano.
                    return valor;
                }

                if (!PareceDatoDpapi(datosCifrados))
                {
                    // Base64 válido pero sin cabecera DPAPI:
                    // es una contraseña en texto plano.
                    return valor;
                }

                try
                {
                    datosOriginales = ProtectedData.Unprotect(
                        datosCifrados,
                        null,
                        DataProtectionScope.LocalMachine);

                    return Encoding.UTF8.GetString(datosOriginales);
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException(
                        "No se pudo descifrar ClavePrivadaProtegida. " +
                        "El valor está protegido con DPAPI pero no " +
                        "con LocalMachine de esta máquina.",
                        ex);
                }
            }
            finally
            {
                if (datosCifrados != null)
                {
                    Array.Clear(
                        datosCifrados,
                        0,
                        datosCifrados.Length);
                }

                if (datosOriginales != null)
                {
                    Array.Clear(
                        datosOriginales,
                        0,
                        datosOriginales.Length);
                }
            }
        }


        // =========================================================
        // GENERAR PFX CON LOS DATOS RECIBIDOS
        //
        // La salida se genera en la carpeta que recibe el método.
        // Los PEM se eliminan después de ejecutar OpenSSL.
        // =========================================================

        private static void CrearPfx(
            string rutaCer,
            string rutaKey,
            string clave,
            string carpetaTrabajo,
            string rutaPfx)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                throw new InvalidOperationException(
                    "La contraseña de la clave privada está vacía.");
            }

            if (!File.Exists(rutaCer))
            {
                throw new FileNotFoundException(
                    "No se encontró el certificado .cer.",
                    rutaCer);
            }

            if (!File.Exists(rutaKey))
            {
                throw new FileNotFoundException(
                    "No se encontró la clave privada .key.",
                    rutaKey);
            }

            string carpetaOpenSsl = Cfg("OpenSslCarpeta");

            if (string.IsNullOrWhiteSpace(carpetaOpenSsl))
            {
                throw new InvalidOperationException(
                    "No está configurada OpenSslCarpeta en App.config.");
            }

            string ejecutableOpenSsl = Path.Combine(
                carpetaOpenSsl,
                "openssl.exe");

            if (!File.Exists(ejecutableOpenSsl))
            {
                throw new FileNotFoundException(
                    "No se encontró openssl.exe. Revisa OpenSslCarpeta.",
                    ejecutableOpenSsl);
            }

            Directory.CreateDirectory(carpetaTrabajo);

            string rutaLlavePem = Path.Combine(
                carpetaTrabajo,
                "llave.pem");

            string rutaCertificadoPem = Path.Combine(
                carpetaTrabajo,
                "certificado.pem");

            try
            {
                if (File.Exists(rutaPfx))
                {
                    File.Delete(rutaPfx);
                }

                // -------------------------------------------------
                // PASO 1: Convertir la clave .key a PEM.
                //
                // El .key del SAT es PKCS#8 CIFRADO. NO se usa
                // -nocrypt: ese parámetro le dice a OpenSSL que la
                // llave de entrada ya viene sin cifrar y provoca el
                // error "Field=version, Type=PKCS8_PRIV_KEY_INFO".
                // Sin -topk8, la salida PEM queda sin cifrar.
                // -------------------------------------------------

                Console.WriteLine(
                    "Convirtiendo la clave privada a PEM...");

                string argumentosLlave =
                    "pkcs8 -inform DER " +
                    "-in " + Citar(rutaKey) + " " +
                    "-out " + Citar(rutaLlavePem) + " " +
                    "-passin env:SAT_FIEL_PASSWORD " +
                    "-provider legacy -provider default";

                EjecutarComandoOpenSsl(
                    carpetaOpenSsl,
                    argumentosLlave,
                    clave);

                // -------------------------------------------------
                // PASO 2: Convertir el certificado .cer a PEM.
                // -------------------------------------------------

                Console.WriteLine(
                    "Convirtiendo el certificado a PEM...");

                string argumentosCertificado =
                    "x509 -inform DER " +
                    "-in " + Citar(rutaCer) + " " +
                    "-outform PEM " +
                    "-out " + Citar(rutaCertificadoPem);

                EjecutarComandoOpenSsl(
                    carpetaOpenSsl,
                    argumentosCertificado,
                    null);

                // -------------------------------------------------
                // PASO 3: Empaquetar los archivos en formato PFX.
                // -------------------------------------------------

                Console.WriteLine(
                    "Generando el archivo PFX...");

                string argumentosPfx =
                    "pkcs12 -export " +
                    "-in " + Citar(rutaCertificadoPem) + " " +
                    "-inkey " + Citar(rutaLlavePem) + " " +
                    "-out " + Citar(rutaPfx) + " " +
                    "-passout env:SAT_FIEL_PASSWORD " +
                    "-provider legacy -provider default";

                EjecutarComandoOpenSsl(
                    carpetaOpenSsl,
                    argumentosPfx,
                    clave);

                if (!File.Exists(rutaPfx) ||
                    new FileInfo(rutaPfx).Length == 0)
                {
                    throw new InvalidOperationException(
                        "OpenSSL no generó un PFX válido.");
                }

                Console.WriteLine(
                    "PFX generado correctamente.");
            }
            catch
            {
                // Evitar dejar un PFX incompleto.
                EliminarArchivo(rutaPfx);
                throw;
            }
            finally
            {
                // La clave PEM contiene la clave privada sin cifrar,
                // por lo que debe eliminarse siempre.
                EliminarArchivo(rutaLlavePem);
                EliminarArchivo(rutaCertificadoPem);
            }
        }


        // =========================================================
        // EJECUTAR OPENSSL
        // La contraseña se entrega por una variable de entorno
        // del proceso, no como argumento visible en la línea
        // de comandos.
        // =========================================================

        private static void EjecutarComandoOpenSsl(
            string carpetaOpenSsl,
            string argumentos,
            string clave)
        {
            string ejecutable = Path.Combine(
                carpetaOpenSsl,
                "openssl.exe");

            using (Process proceso = new Process())
            {
                proceso.StartInfo = new ProcessStartInfo
                {
                    FileName = ejecutable,
                    Arguments = argumentos,
                    WorkingDirectory = carpetaOpenSsl,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                if (clave != null)
                {
                    proceso.StartInfo.EnvironmentVariables[
                        "SAT_FIEL_PASSWORD"] = clave;
                }

                proceso.Start();

                // Leer simultáneamente ambas salidas evita que
                // el proceso se bloquee si una de ellas se llena.
                Task<string> tareaSalida =
                    proceso.StandardOutput.ReadToEndAsync();

                Task<string> tareaError =
                    proceso.StandardError.ReadToEndAsync();

                proceso.WaitForExit();

                Task.WaitAll(tareaSalida, tareaError);

                string salida = tareaSalida.Result;
                string error = tareaError.Result;

                if (proceso.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "OpenSSL falló con código " +
                        proceso.ExitCode + ". " +
                        error + " " + salida);
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine(
                        "Mensaje de OpenSSL: " + error);
                }
            }
        }


        // =========================================================
        // CITAR RUTAS PARA LA LINEA DE COMANDOS
        // =========================================================

        private static string Citar(string valor)
        {
            return "\"" + valor.Replace("\"", "\\\"") + "\"";
        }


        // =========================================================
        // ELIMINAR ARCHIVOS TEMPORALES
        // =========================================================

        private static void EliminarArchivo(string ruta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ruta) &&
                    File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Aviso: no se pudo eliminar un archivo temporal: " +
                    ex.Message);
            }
        }


        // =========================================================
        // CARGAR FIEL POR RFC
        //
        // Lee la FIEL de ControlRFCSAT, genera un PFX temporal
        // (carpeta con GUID, así varios workers no se pisan) y
        // devuelve el certificado. Quien lo reciba debe hacer
        // Dispose() cuando termine de usarlo.
        // =========================================================

        public static X509Certificate2 Cargar(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
            {
                throw new ArgumentException(
                    "El RFC no puede estar vacío.",
                    nameof(rfc));
            }

            rfc = rfc.Trim();

            Console.WriteLine(
                "Obteniendo configuración FIEL para: " + rfc);

            // 1. Consultar ControlRFCSAT.
            DatosFiel datos = ObtenerDatosFiel(rfc);

            // 2. Resolver las rutas.
            string rutaCer = ResolverRuta(
                datos.RutaCarpetafiel,
                datos.RutaCer);

            string rutaKey = ResolverRuta(
                datos.RutaCarpetafiel,
                datos.RutaKey);

            // 3. Obtener la contraseña (texto plano o DPAPI).
            string clave = DesprotegerClavePrivada(
                datos.ClavePrivadaProtegida);

            // 4. Validar los archivos.
            if (!File.Exists(rutaCer))
            {
                throw new FileNotFoundException(
                    "No existe el certificado del RFC " + rfc,
                    rutaCer);
            }

            if (!File.Exists(rutaKey))
            {
                throw new FileNotFoundException(
                    "No existe la clave privada del RFC " + rfc,
                    rutaKey);
            }

            // 5. Crear una carpeta temporal exclusiva.
            Directory.CreateDirectory(NomCarpetaPfx);

            string carpetaTemporal = Path.Combine(
                NomCarpetaPfx,
                "Temporal_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(carpetaTemporal);

            string rutaPfx = Path.Combine(
                carpetaTemporal,
                "certificado.pfx");

            try
            {
                // 6. Generar PFX utilizando exclusivamente
                // la FIEL de este RFC.
                CrearPfx(
                    rutaCer,
                    rutaKey,
                    clave,
                    carpetaTemporal,
                    rutaPfx);

                // 7. Leer el PFX.
                byte[] pfxBytes = File.ReadAllBytes(rutaPfx);

                try
                {
                    X509Certificate2 certificado =
                        new X509Certificate2(
                            pfxBytes,
                            clave,
                            X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.Exportable);

                    if (!certificado.HasPrivateKey)
                    {
                        certificado.Dispose();

                        throw new CryptographicException(
                            "El PFX no contiene la clave privada del RFC " +
                            rfc);
                    }

                    Console.WriteLine(
                        "FIEL cargada correctamente para: " + rfc);

                    return certificado;
                }
                finally
                {
                    Array.Clear(
                        pfxBytes,
                        0,
                        pfxBytes.Length);
                }
            }
            finally
            {
                // No conservar el PFX ni los archivos temporales.
                try
                {
                    if (Directory.Exists(carpetaTemporal))
                    {
                        Directory.Delete(
                            carpetaTemporal,
                            true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "Aviso: no se pudo eliminar la carpeta " +
                        "temporal del PFX: " + ex.Message);
                }

                clave = null;
            }
        }
    }
}