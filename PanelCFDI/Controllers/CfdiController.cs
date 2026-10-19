using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

// Endpoints de lectura para el panel:
//   /Cfdi/Lista        -> comprobantes
//   /Cfdi/Solicitudes  -> solicitudes al SAT
//   /Cfdi/Info         -> contadores y estado del proceso
//   /Cfdi/Xml?uuid=... -> descarga del XML
public class CfdiController : Controller
{
    [HttpGet]
    public JsonResult Lista(int max = 1000)
    {
        if (max < 1) max = 1;
        if (max > 5000) max = 5000;

        var datos = CfdiRepo.ListarCfdi(max);

        // Agrega el enlace de descarga del XML a cada comprobante
        var salida = new System.Collections.Generic.List<object>();
        foreach (CfdiFila c in datos)
        {
            salida.Add(new
            {
                uuid = c.Uuid,
                fecha = c.Fecha,
                emisor = c.Emisor,
                rfc = c.Rfc,
                tipo = c.Tipo,
                total = c.Total,
                xml = Url.Content("~/Cfdi/Xml?uuid=" + HttpUtility.UrlEncode(c.Uuid)),
                pdf = ""
            });
        }

        return Json(salida, JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public JsonResult Solicitudes(int max = 200)
    {
        if (max < 1) max = 1;
        if (max > 2000) max = 2000;

        return Json(CfdiRepo.ListarSolicitudes(max), JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public JsonResult Info()
    {
        return Json(CfdiRepo.ObtenerInfo(), JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult Xml(string uuid)
    {
        // El uuid solo puede contener letras, números y guiones
        if (string.IsNullOrEmpty(uuid) || uuid.Length > 50 ||
            !Regex.IsMatch(uuid, "^[A-Za-z0-9-]+$"))
            return new HttpStatusCodeResult(400, "UUID no válido");

        string xml = CfdiRepo.ObtenerXml(uuid);

        if (xml == null)
            return HttpNotFound();

        return File(Encoding.UTF8.GetBytes(xml), "application/xml", uuid + ".xml");
    }
}