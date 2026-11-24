using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

// Guarda en disco los archivos FIEL (.cer y .key) de un contribuyente.
// - Verifica que la carpeta exista y, si no, la crea.
// - Valida extensión, tamaño y que el .cer sea un certificado real y vigente.
// - Guarda los archivos como {RFC}.cer y {RFC}.key (así no se pisan entre contribuyentes).
public class ResultadoFiel
{
    public string Carpeta;
    public string ArchivoCer;
    public string ArchivoKey;
    public bool CarpetaCreada;
    public DateTime Vence;
    public string Aviso;
}

public static class FielService
{
    const int MaxBytes = 200 * 1024; // un .cer / .key real pesa ~2 KB

    static readonly Regex ReRfc =
        new Regex(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

    // RFC válido. Importante: también evita rutas tipo "..\..\algo" en el nombre del archivo.
    public static bool RfcValido(string rfc)
    {
        return !string.IsNullOrEmpty(rfc) && ReRfc.IsMatch(rfc);
    }

    // Carpeta donde se guardan los archivos (Web.config -> appSettings "FielCarpeta")
    public static string CarpetaBase()
    {
        string c = (ConfigurationManager.AppSettings["FielCarpeta"] ?? "").Trim();

        if (c.Length == 0)
            throw new InvalidOperationException("Falta la clave FielCarpeta en el Web.config.");

        if (!Path.IsPathRooted(c))
            throw new InvalidOperationException("FielCarpeta debe ser una ruta absoluta (ej. C:\\...\\FIEL).");

        return Path.GetFullPath(c);
    }

    static byte[] Leer(HttpPostedFileBase f, string extension, string etiqueta)
    {
        if (f == null || f.ContentLength == 0)
            throw new InvalidOperationException("Selecciona el archivo " + etiqueta + ".");

        if (!string.Equals(Path.GetExtension(f.FileName), extension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El archivo " + etiqueta + " debe tener extensión " + extension + ".");

        if (f.ContentLength > MaxBytes)
            throw new InvalidOperationException("El archivo " + etiqueta + " es demasiado grande.");

        using (var ms = new MemoryStream())
        {
            f.InputStream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    // Cifra la contraseña con DPAPI (LocalMachine). Generadorpfx ya sabe descifrarla.
    // Solo funciona si el panel y el servicio corren en la MISMA máquina.
    // Se desactiva con <add key="ProtegerClaveFiel" value="false" /> en el Web.config.
    public static string ProtegerClave(string clave)
    {
        string cfg = (ConfigurationManager.AppSettings["ProtegerClaveFiel"] ?? "true").Trim();

        if (cfg.Equals("false", StringComparison.OrdinalIgnoreCase))
            return clave;

        byte[] datos = Encoding.UTF8.GetBytes(clave);
        try
        {
            byte[] cifrado = ProtectedData.Protect(datos, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(cifrado);
        }
        finally
        {
            Array.Clear(datos, 0, datos.Length);
        }
    }

    public static ResultadoFiel Guardar(string rfc, HttpPostedFileBase cer, HttpPostedFileBase key)
    {
        byte[] bytesCer = Leer(cer, ".cer", ".cer");
        byte[] bytesKey = Leer(key, ".key", ".key");

        var res = new ResultadoFiel();

        // El .cer debe ser un certificado válido y no estar vencido.
        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(bytesCer);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("El archivo .cer no es un certificado válido.");
        }

        using (cert)
        {
            if (cert.NotAfter < DateTime.Now)
                throw new InvalidOperationException(
                    "El certificado venció el " + cert.NotAfter.ToString("yyyy-MM-dd") + ".");

            res.Vence = cert.NotAfter;

            // Aviso (no bloquea): el RFC del certificado normalmente aparece en el Subject.
            if (cert.Subject.IndexOf(rfc, StringComparison.OrdinalIgnoreCase) < 0)
                res.Aviso = "No pude confirmar que el certificado pertenezca a " + rfc +
                            ". Revisa que sea el correcto.";
        }

        string carpeta = CarpetaBase();

        try
        {
            // Si la carpeta no existe, se crea (incluyendo las carpetas padre que falten).
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
                res.CarpetaCreada = true;
            }

            res.Carpeta = carpeta;
            res.ArchivoCer = rfc + ".cer";
            res.ArchivoKey = rfc + ".key";

            File.WriteAllBytes(Path.Combine(carpeta, res.ArchivoCer), bytesCer);
            File.WriteAllBytes(Path.Combine(carpeta, res.ArchivoKey), bytesKey);
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                "Sin permiso para escribir en " + carpeta +
                ". Da permiso de escritura a la identidad del grupo de aplicaciones de IIS.");
        }

        return res;
    }
}