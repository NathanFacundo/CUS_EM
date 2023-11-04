using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace CUS.Areas.Admin.Controllers
{
    public class HistoriaNutricionController : Controller
    {
        Models.CUS db = new Models.CUS();
        Models.HC_Nutricion hcNut = new Models.HC_Nutricion();

        // GET: Admin/HistoriaNutricion
        public ActionResult Index()
        {
            return View();
        }

        
    }
}
