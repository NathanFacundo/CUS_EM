using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CUS.Models;

namespace CUS.Areas.Admin.Controllers
{
    public class PacienteController : Controller
    {
        Models.CUS db = new Models.CUS();

        // GET: Admin/Paciente
        public ActionResult Index(string expediente)
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

        // GET: Admin/Paciente/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Admin/Paciente/Create
        public ActionResult Create(string expediente)
        {
            if (expediente != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }
        }

        // POST: Admin/Paciente/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Admin/Paciente/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Admin/Paciente/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Admin/Paciente/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Admin/Paciente/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Registro(string expediente)
        {

            if (expediente != null)
            {
                ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");

                return View();
            }
            else
            {
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }
        }
    }
}
