using System;
using System.Globalization;
using System.Web.Mvc;

// Responde a /Control/Estado y /Control/Fijar (las URLs que usa el JavaScript)
public class ControlController : Controller
{
    [HttpGet]
    public JsonResult Estado(string tipo = "RECIBIDO")
    {
        try
        {
            DateTime? f = ControlConsultaRepo.Obtener(tipo);
            return Json(new { ok = true, fecha = f?.ToString("yyyy-MM-dd HH:mm:ss") },
                        JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message },
                        JsonRequestBehavior.AllowGet);
        }
    }

    [HttpPost]
    // POST /Control/FijarFechaRfc  (rfc, modo = actual|fecha, fecha)
    // Aplica la fecha a todos los tipos del RFC.
    
    public JsonResult Fijar(string rfc, string modo, string fecha)
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

            int filas = ControlConsultaRepo.FijarPorRFC(rfc, nueva);

            return Json(new { ok = true, fecha = nueva.ToString("yyyy-MM-dd HH:mm"), filas = filas });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }
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

 
}