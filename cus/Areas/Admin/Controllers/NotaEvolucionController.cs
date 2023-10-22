using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    public class NotaEvolucionController : Controller
    {
        Models.CUS db = new Models.CUS();

        // GET: Admin/NotaEvolucion
        public ActionResult Index()
        {
            return View();
        }


        //Diagnosticos frecuentes del medico
        public JsonResult DiagnosticosMedico()
        {
            var fecha = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-ddT00:00:00.000");
            var fecha_correcta = DateTime.Parse(fecha);
            var username = User.Identity.GetUserName();

            var diagnosticos = (from r in db.NotaEvolucion
                                join diag1 in db.Diagnosticos on r.diagnostico1 equals diag1.clave into diagX1
                                from diaIn1 in diagX1.DefaultIfEmpty()
                                    //join diagnostico2Nombre in db.DIAGNOSTICOS on r.diagnostico2 equals diagnostico2Nombre.Clave
                                    //join diagnostico3Nombre in db.DIAGNOSTICOS on r.diagnostico3 equals diagnostico3Nombre.Clave
                                    //where r.DesCorta.Contains(diagnostico)
                                    //|| r.DescCompleta.Contains(diagnostico)
                                    //|| r.Clave.Contains(diagnostico)
                                where r.medico == username
                                where r.fecha >= fecha_correcta
                                where r.diagnostico1 != null
                                select new
                                {
                                    //diagnostico = r.diagnostico,
                                    DesCorta = diaIn1.diagnostico,
                                    Clave = diaIn1.clave,
                                })
                                .GroupBy(p => new
                                {
                                    p.DesCorta,
                                    p.Clave,
                                })
                                .Select(g => new
                                {
                                    DesCorta = g.Key.DesCorta,
                                    Clave = g.Key.Clave,
                                    Count = g.Count(),
                                })
                                .OrderByDescending(g => g.Count)
                                .ToList()
                                .Take(6);

            return new JsonResult { Data = diagnosticos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }


        //Diagnosticos frecuentes del paciente
        public JsonResult DiagnosticosPaciente(string numemp)
        {
            var fecha = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-ddT00:00:00.000");
            var fecha_correcta = DateTime.Parse(fecha);
            var username = User.Identity.GetUserName();

            var diagnosticos = (from r in db.NotaEvolucion
                                join diag1 in db.Diagnosticos on r.diagnostico1 equals diag1.clave into diagX1
                                from diaIn1 in diagX1.DefaultIfEmpty()
                                where r.num_exp == numemp
                                where r.fecha >= fecha_correcta
                                where r.diagnostico1 != null
                                select new
                                {
                                    //diagnostico = r.diagnostico,
                                    DesCorta = diaIn1.diagnostico,
                                    Clave = diaIn1.clave,
                                })
                                .GroupBy(p => new
                                {
                                    p.DesCorta,
                                    p.Clave,
                                })
                                .Select(g => new
                                {
                                    DesCorta = g.Key.DesCorta,
                                    Clave = g.Key.Clave,
                                    Count = g.Count(),
                                })
                                .OrderByDescending(g => g.Count)
                                .ToList()
                                .Take(6);

            return new JsonResult { Data = diagnosticos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }


        //Diagnosticos frecuentes de la especialidad
        public JsonResult DiagnosticosEspecialidad(string numemp)
        {
            var fecha = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-ddT00:00:00.000");
            var fecha_correcta = DateTime.Parse(fecha);
            var username = User.Identity.GetUserName();
            //var especialidad = username.Substring(0, 2);

            var diagnosticos = (from r in db.NotaEvolucion
                                join diag1 in db.Diagnosticos on r.diagnostico1 equals diag1.clave into diagX1
                                from diaIn1 in diagX1.DefaultIfEmpty()
                                //where r.medico.Substring(0, 2) == especialidad
                                where r.fecha >= fecha_correcta
                                where r.diagnostico1 != null
                                select new
                                {
                                    //diagnostico = r.diagnostico,
                                    DesCorta = diaIn1.diagnostico,
                                    Clave = diaIn1.clave,
                                })
                                .GroupBy(p => new
                                {
                                    p.DesCorta,
                                    p.Clave,
                                })
                                .Select(g => new
                                {
                                    DesCorta = g.Key.DesCorta,
                                    Clave = g.Key.Clave,
                                    Count = g.Count(),
                                })
                                .OrderByDescending(g => g.Count)
                                .ToList()
                                .Take(6);

            return new JsonResult { Data = diagnosticos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }


        public partial class DIAGNOSTICOS
        {
            public string clave { get; set; }
            public string diagnostico { get; set; }
        }

        public JsonResult BuscarDiagnosticos(string diagnostico)
        {
            var diagnosticoChido = diagnostico;
            var diag = diagnosticoChido.Replace(" ", "%");

            string query = "SELECT clave, diagnostico FROM Diagnosticos WHERE diagnostico like '%" + diag + "%' COLLATE Latin1_General_CI_AI OR clave like '%" + diag + "%'";
            var result = db.Database.SqlQuery<DIAGNOSTICOS>(query);
            var diagnosticos = result.ToList();

            return new JsonResult { Data = diagnosticos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }


    }
}