using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Controllers
{
    public class PXController : Controller
    {
        // GET: PX
        public ActionResult Index()
        {
            return View();
        }

        // GET: PX/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PX/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PX/Create
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

        // GET: PX/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PX/Edit/5
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

        // GET: PX/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PX/Delete/5
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
            return View();
        }
    }
}
