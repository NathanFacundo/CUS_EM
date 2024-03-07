using CUS.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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


        public class LstMedicamentos
        {
            public int id { get; set; }
            public string clave { get; set; }
            public string descripcion { get; set; }
            public int? nivel { get; set; }
            public string grupo { get; set; }

        }

        public JsonResult ListaMedicamentos(string id)
        {
            //Models.SERVMEDEntities4 db4 = new Models.SERVMEDEntities4();
            //Models.SMDEVEntities33 db2 = new Models.SMDEVEntities33();
            var fecha = DateTime.Now.AddMonths(-3).ToString("yyyy-MM-ddTHH:mm:ss.fff");
            var fechaDT = DateTime.Parse(fecha);
            //RECETAS
            //string query = "SELECT Sustancia_1.Nivel_21 AS nivel_21, Sustancia_1.SobranteInv2022 as sobranteinv2022, InvFarm_1.Id_Sustancia as id_sustancia, InvFarm_1.Usuario_Registra AS usuario_registra, InvFarm_1.Id AS id, InvFarm_1.Inv_Sal AS sal, InvFarm_1.Inv_Act AS inv_act, Sustancia_1.Clave AS clave, Grupo_1.descripcion AS descripcion_grupo, Sal_1.Descripcion_Sal AS descripcion_sal, Sustancia_1.descripcion_21 AS presentacion FROM SERVMED.dbo.grupo_21 AS Grupo_1 INNER JOIN SERVMED.dbo.Sustancia AS Sustancia_1 ON Grupo_1.Id = Sustancia_1.id_grupo_21 INNER JOIN SERVMED.dbo.Sal AS Sal_1 ON Sustancia_1.Id_Sal = Sal_1.Id INNER JOIN SERVMED.dbo.InvFarm AS InvFarm_1 ON Sustancia_1.Id = InvFarm_1.Id_Sustancia INNER JOIN SERVMED.dbo.Inventario AS Inventario_1 ON InvFarm_1.InvFarmId = Inventario_1.id WHERE(Inventario_1.status = 1) AND(Inventario_1.tipo = 1) and Sustancia_1.descripcion_21 is not null and Sustancia_1.descripcion_21 != '' and Sustancia_1.Clave != '251001'";
            //string query = "SELECT Sustancia_1.SobranteInv2022 as sobranteinv2022, InvFarm_1.Id_Sustancia as id_sustancia, InvFarm_1.Usuario_Registra AS usuario_registra, InvFarm_1.Id AS id, InvFarm_1.Inv_Sal AS sal, InvFarm_1.Inv_Act AS inv_act, Sustancia_1.Clave AS clave, Grupo_1.descripcion AS descripcion_grupo, Sal_1.Descripcion_Sal AS descripcion_sal, Sustancia_1.descripcion_21 AS presentacion, Nivel_1.id AS nivel_21 FROM SERVMED.dbo.grupo_21 AS Grupo_1 INNER JOIN SERVMED.dbo.Sustancia AS Sustancia_1 ON Grupo_1.Id = Sustancia_1.id_grupo_21 INNER JOIN SERVMED.dbo.Sal AS Sal_1 ON Sustancia_1.Id_Sal = Sal_1.Id LEFT JOIN SERVMED.dbo.nivel_21 AS Nivel_1 ON Sustancia_1.Nivel_21 = Nivel_1.id INNER JOIN SERVMED.dbo.InvFarm AS InvFarm_1 ON Sustancia_1.Id = InvFarm_1.Id_Sustancia INNER JOIN SERVMED.dbo.Inventario AS Inventario_1 ON InvFarm_1.InvFarmId = Inventario_1.id WHERE(Inventario_1.status = 1) AND(Inventario_1.tipo = 1) and Sustancia_1.descripcion_21 is not null and Sustancia_1.descripcion_21 != '' and Sustancia_1.Clave != '251001'";
            //var result = db4.Database.SqlQuery<LstInv>(query);
            //var res = result.ToList();

            var res = (from s in db.Sustancias
                       join g in db.Grupo_Sustancia on s.Grupo equals g.id into gX
                       from gIn in gX.DefaultIfEmpty()
                       //where s.Descripcion != null && s.Descripcion != ""
                       select new
                       {
                           Id = s.id,
                           Clave = s.Clave,
                           Descripcion = s.Descripcion,
                           Nivel = s.Nivel,
                           Grupo = gIn.grupo,
                       })
                       .ToList();

            //System.Diagnostics.Debug.WriteLine(res);

            var medicamentos = new List<LstMedicamentos>();


            foreach (var item in res)
            {

                var listamedicamentos = new LstMedicamentos
                {
                    id = item.Id,
                    clave = item.Clave,
                    descripcion = item.Descripcion,
                    nivel = item.Nivel,
                    grupo = item.Grupo,
                };

                medicamentos.Add(listamedicamentos);

            }




            return new JsonResult { Data = medicamentos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };



        }



        public JsonResult MedicamentoDetalle(int? id)
        {

            //string query = "select id_sustancia as id_sustancia, cactual as cactual from inv_mederos";
            //var result = db.Database.SqlQuery<RecetasMederos>(query);
            //var res = result.ToList();

            //var medicamentos = new List<Result>();

            var res2 = (from a in db.Sustancias
                        join grupo in db.Grupo_Sustancia on a.Grupo equals grupo.id into grupoX
                        from grupoIn in grupoX.DefaultIfEmpty()
                        where a.id == id
                        select new
                        {

                            Id = a.id,
                            Clave = a.Clave,
                            Descripcion = a.Descripcion,
                            Grupo = grupoIn.grupo,

                        }).FirstOrDefault();

            //System.Diagnostics.Debug.WriteLine(inventariofarmacia.Clave);

            if (res2 != null)
            {
                var resultNew = new Object();

                resultNew = new
                {
                    Id = res2.Id,
                    Clave = res2.Clave,
                    Descripcion = res2.Descripcion,
                    Grupo = res2.Grupo,
                    mensajeRcta = 0,
                };

                return new JsonResult { Data = resultNew, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }

            var medicamentos = "";
            return new JsonResult { Data = medicamentos, JsonRequestBehavior = JsonRequestBehavior.AllowGet };



            //return new JsonResult { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        [HttpPost]
        public ActionResult Create(Recetas recetas, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la Nota de Evolucion.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                var username = User.Identity.GetUserName();

                var ip_realiza = Request.UserHostAddress;
                var unidad = Request.UserHostAddress;

                //Buscamos si a ese px se le acaba de crear registro en la tbl NotaEvolucion.
                if (paciente != null)
                {
                    //Se crea la receta 
                    Recetas rcta = new Recetas();
                    rcta.expediente = paciente.Expediente;
                    rcta.usuario = username;
                    rcta.fecha = fechaDT;
                    rcta.ip_realiza = ip_realiza;
                    rcta.estatus = 1;
                    rcta.unidad = 6;
                    db.Recetas.Add(rcta);
                    db.SaveChanges();

                    var registroReciente = (from a in db.Recetas
                                               where a.expediente == paciente.Expediente
                                               select a).
                             OrderByDescending(r => r.fecha)
                             .FirstOrDefault();

                    registroReciente.folio = "21E"+registroReciente.id;
                    db.Entry(registroReciente).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["idreceta"] = registroReciente.id;
                    TempData["message_success"] = "Medicamento agregado con éxito";
                }
            }
            catch (Exception ex)
            {
                TempData["message_success"] = "Error, vuelve a intentar";
            }

            return Redirect(Request.UrlReferrer.ToString());


        }



        public JsonResult DetalleReceta(int? idreceta)
        {
            
            
            var recetadetalle = (from a in db.Recetas
                        join uniAfil in db.UnidadAfiliacion on a.unidad equals uniAfil.Id into uniAfilX
                        from uniAfilIn in uniAfilX.DefaultIfEmpty()
                        join usuario in db.AspNetUsers on a.usuario equals usuario.UserName into usuarioX
                        from usuarioIn in usuarioX.DefaultIfEmpty()
                        where a.id == idreceta
                        select new
                        {

                            idreceta = a.id,
                            expediente = a.expediente,
                            unidad = uniAfilIn.NombreUnidad,
                            medico = usuarioIn.Name

                        }).FirstOrDefault();


            var fecha = DateTime.Now.AddHours(-6).ToString("yyyy-MM-ddTHH:mm:ss.fff");
            var fechaDT = DateTime.Parse(fecha);

            //Buscar signos vitales de hoy
            var signosvit = (from a in db.SignosVitales
                             where a.expediente == recetadetalle.expediente
                             where a.fecha >= fechaDT
                             select new
                             {
                                talla = a.talla,
                                peso = a.peso
                             }).FirstOrDefault();

            var resultdata = new { data1 = recetadetalle, data2 = signosvit };

            return new JsonResult { Data = resultdata, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }

    }
}