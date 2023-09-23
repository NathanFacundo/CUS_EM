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
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/Paciente/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Admin/Paciente/Create
        public ActionResult Create()
        {
            return View();
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

        public ActionResult Registro()
        {
            ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");

            return View();
        }
    }
}
