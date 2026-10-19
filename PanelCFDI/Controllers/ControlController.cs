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
    public JsonResult Fijar(string tipo, string modo, string fecha)
    {
        try
        {
            if (tipo != "RECIBIDO" && tipo != "EMITIDO")
                return Json(new { ok = false, error = "Tipo no válido." });

            DateTime nueva;

            if (modo == "actual")    // if que valida que la variable modo sea de que tipo si actual con el boton en la pagina o de modo fecha y cambiarla a una anterior
            {
                nueva = DateTime.Today;          // si es actual la pone en el dia de hoy con la hora 00:00:00
            }
            else if (modo == "fecha")
            {
                DateTime f;
                // El input datetime-local manda "2025-02-10T12:00"
                if (!DateTime.TryParseExact(
                        fecha,
                        new[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out f))
                    return Json(new { ok = false, error = "Fecha no válida." });

                if (f > DateTime.Now)
                    return Json(new { ok = false, error = "La fecha no puede ser futura." });

                nueva = f;
            }
            else
            {
                return Json(new { ok = false, error = "Datos incompletos." });
            }

            ControlConsultaRepo.Fijar(tipo, nueva);
            return Json(new { ok = true, fecha = nueva.ToString("yyyy-MM-dd HH:mm:ss") });
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