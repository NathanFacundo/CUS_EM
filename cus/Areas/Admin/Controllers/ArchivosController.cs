using System;
using System.Collections.Generic;
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
            if(expediente != null)
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


    }
}