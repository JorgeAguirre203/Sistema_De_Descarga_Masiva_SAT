using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// Responde a /Control/EstadoRfc, /Control/FijarFechaRfc y /Control/FijarRfc
// (las URLs que usa el JavaScript de index.html)
public class ControlController : Controller
{
    // GET /Control/EstadoRfc -> fechas de ControlConsultasPorRFC
    [HttpGet]
    public JsonResult EstadoRfc()
    {
        try
        {
            return Json(new { ok = true, filas = ControlConsultaRepo.ListarPorRfc() },
                        JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message },
                        JsonRequestBehavior.AllowGet);
        }
    }

    // POST /Control/FijarFechaRfc  (rfc, modo = actual|fecha, fecha)
    // Aplica la fecha a todos los tipos del RFC.
    [HttpPost]
    public JsonResult FijarFechaRfc(string rfc, string modo, string fecha)
    {
        try
        {
            rfc = (rfc ?? "").Trim().ToUpperInvariant();

            if (rfc.Length < 12 || rfc.Length > 13)
                return Json(new { ok = false, error = "RFC no válido." });

            DateTime nueva;

            if (modo == "actual")
            {
                nueva = DateTime.Today;
            }
            else if (modo == "fecha")
            {
                // El input datetime-local manda "2025-02-10T12:00"
                if (!DateTime.TryParseExact(
                        fecha,
                        new[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out nueva))
                    return Json(new { ok = false, error = "Fecha no válida." });

                if (nueva > DateTime.Now)
                    return Json(new { ok = false, error = "La fecha no puede ser futura." });
            }
            else
            {
                return Json(new { ok = false, error = "Datos incompletos." });
            }

            int filas = ControlConsultaRepo.FijarPorRfc(rfc, nueva);

            return Json(new { ok = true, fecha = nueva.ToString("yyyy-MM-dd HH:mm"), filas = filas });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    // POST /Control/FijarRfc  (tipo, rfc) -> ControlRFCSAT
    [HttpPost]
    public JsonResult FijarRfc(string tipo, string rfc)
    {
        try
        {
            ControlConsultaRepo.FijarRfc(tipo, rfc);
            return Json(new { ok = true, rfc = rfc });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult FijarIntervaloRfc(string rfc, int intervalo)
    {
        try
        {
            rfc = (rfc ?? "").Trim().ToUpperInvariant();

            if (rfc.Length < 12 || rfc.Length > 13)
                return Json(new { ok = false, error = "RFC no válido." });

            if (intervalo < 1 || intervalo > 365)
                return Json(new { ok = false, error = "El intervalo debe estar entre 1 y 365 días." });

            int filas = ControlConsultaRepo.FijarIntervaloPorRfc(rfc, intervalo);

            if (filas == 0)
                return Json(new { ok = false, error = "Ese RFC no existe." });

            return Json(new { ok = true, intervalo = intervalo, filas = filas });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    // GET /Control/EstadoFiel -> qué RFC ya tienen FIEL configurada
    [HttpGet]
    public JsonResult EstadoFiel()
    {
        try
        {
            string carpetaBase = FielService.CarpetaBase();
            var filas = ControlConsultaRepo.ListarFiel();

            foreach (var f in filas)
            {
                string dir = string.IsNullOrWhiteSpace(f.Carpeta) ? carpetaBase : f.Carpeta;
                f.ArchivosOk =
                    !string.IsNullOrWhiteSpace(f.Cer) && !string.IsNullOrWhiteSpace(f.Key) &&
                    System.IO.File.Exists(System.IO.Path.Combine(dir, f.Cer)) &&
                    System.IO.File.Exists(System.IO.Path.Combine(dir, f.Key));
            }

            // Minúsculas: es lo que espera fiel.js (el serializador respetaría Rfc, Cer...)
            var salida = filas.Select(f => new
            {
                rfc = f.Rfc,
                cer = f.Cer,
                key = f.Key,
                carpeta = f.Carpeta,
                tieneClave = f.TieneClave,
                archivosOk = f.ArchivosOk
            }).ToList();

            return Json(new { ok = true, carpeta = carpetaBase, filas = salida },
                        JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
        }
    }

    // POST /Control/SubirFiel  (rfc, clave, cer, key)
    // Guarda .cer y .key en la carpeta FIEL (la crea si no existe) y actualiza ControlRFCSAT.
    [HttpPost]
    public JsonResult SubirFiel(string rfc, string clave, HttpPostedFileBase cer, HttpPostedFileBase key)
    {
        try
        {
            rfc = (rfc ?? "").Trim().ToUpperInvariant();

            if (!FielService.RfcValido(rfc))
                return Json(new { ok = false, error = "RFC no válido." });

            if (string.IsNullOrWhiteSpace(clave))
                return Json(new { ok = false, error = "Escribe la contraseña de la clave privada." });

            if (!ControlConsultaRepo.ExisteRfcSat(rfc))
                return Json(new { ok = false, error = "Ese RFC no está dado de alta. Agrégalo primero en \"Fecha de búsqueda\"." });

            // 1) archivos en disco (verifica / crea la carpeta)
            ResultadoFiel r = FielService.Guardar(rfc, cer, key);

            // 2) base de datos (todas las filas del RFC: RECIBIDO, EMISOR, SOLICITANTE)
            int filas = ControlConsultaRepo.GuardarFiel(
                rfc, r.Carpeta, r.ArchivoCer, r.ArchivoKey, FielService.ProtegerClave(clave));

            return Json(new
            {
                ok = true,
                carpeta = r.Carpeta,
                carpetaCreada = r.CarpetaCreada,
                cer = r.ArchivoCer,
                key = r.ArchivoKey,
                vence = r.Vence.ToString("yyyy-MM-dd"),
                filas = filas,
                aviso = r.Aviso
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = "Error: " + ex.Message });
        }
    }

    [HttpPost]
    public JsonResult EliminarRfc(string rfc)
    {
        if (string.IsNullOrWhiteSpace(rfc))
            return Json(new { ok = false, mensaje = "Escribe el RFC a eliminar." });

        try
        {
            int filas = ControlConsultaRepo.EliminaRFC(rfc);

            if (filas == 0)
                return Json(new { ok = false, mensaje = "Ese RFC no existe." });

            return Json(new { ok = true, mensaje = "RFC eliminado correctamente." });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mensaje = "Error: " + ex.Message });
        }
    }
}