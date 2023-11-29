using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{

    public class RecetasController : Controller
    {

        Models.CUS db = new Models.CUS();


        // GET: Admin/Recetas
        public ActionResult Index()
        {
            return View();
        }



        [HttpGet]
        public ActionResult Create(string expediente)
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


    }
}