using System;
using System.Globalization;
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