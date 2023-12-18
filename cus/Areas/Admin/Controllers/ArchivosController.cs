using CUS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    public class ArchivosController : Controller
    {
        Models.CUS db = new Models.CUS();
        // GET: Admin/Archivos
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Create(string expediente)
        {
            if (User.IsInRole("Expediente"))
            {
                if (expediente != null)
                {
                    var paciente = (from a in db.Paciente
                                    where a.Expediente == expediente
                                    select a).FirstOrDefault();

                    return View(paciente);
                }
                else
                {
                    return RedirectToAction("BuscarPaciente", "DerechoHabiente");
                }
            }
            else
            {
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Create(Archivos archivos, string expediente, HttpPostedFileBase imagenSubida)
        {
            try
            {
                if (imagenSubida != null && imagenSubida.ContentLength > 0)
                {
                    var nombreArchivo = Path.GetFileName(imagenSubida.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/" + archivos.tipo_archivo + "/" + expediente + "/"), nombreArchivo);
                    imagenSubida.SaveAs(path);

                    var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    var fechaDT = DateTime.Parse(fecha);

                    Archivos arch = new Archivos();
                    arch.titulo = archivos.titulo;
                    arch.tipo_archivo = archivos.tipo_archivo;
                    arch.archivo = nombreArchivo;
                    //arch.fecha_archivo = archivos.fecha_archivo;
                    arch.expediente = expediente;
                    arch.fecha_registro = fechaDT;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    //nota.Clave_hc_px = Id_claveHC;
                    db.Archivos.Add(arch);
                    db.SaveChanges();


                    TempData["message_success"] = "Archivo agregado con éxito";
                    return Redirect(Request.UrlReferrer.ToString());
                }
                else
                {
                    TempData["message_error"] = "Debes subir un archivo valido";
                    return Redirect(Request.UrlReferrer.ToString());
                }
            }
            catch (Exception ex)
            {
                //return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
                TempData["message_error"] = "Error, vuelve a intentar";
                return Redirect(Request.UrlReferrer.ToString());
            }


        }
    }
}