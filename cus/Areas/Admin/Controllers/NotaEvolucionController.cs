using CUS.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    public class NotaEvolucionController : Controller
    {
        public DerechohabienteController _dhController;

        /*
        public NotaEvolucionController(DerechohabienteController dhController)
        {
            _dhController = dhController;
        }
        */
        


        Models.CUS db = new Models.CUS();

        // GET: Admin/NotaEvolucion
        public ActionResult Index(string expediente)
        {
            //var mensajeGlobal = BuscarPaciente();
            //var mensajeGlobal = _dhController.BuscarPaciente();
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



        public ActionResult Paciente(string expediente)
        {
            //var mensajeGlobal = BuscarPaciente();
            //var mensajeGlobal = _dhController.BuscarPaciente();
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


        public ActionResult Create(string expediente)
        {
                if (expediente != null)
                {
                    var paciente = (from a in db.Paciente
                                    where a.Expediente == expediente
                                    select a).FirstOrDefault();
                    if (User.IsInRole("Enfermeria"))
                    {
                        //return RedirectToAction("Create/" + expediente, "Enfermeria");
                        return RedirectToAction("Create", "Enfermeria", new { expediente = expediente });

                    }
                    else
                    {
                        if (User.IsInRole("Expediente"))
                        {
                            return View(paciente);
                        }
                        else
                        {
                            return RedirectToAction("BuscarPaciente", "DerechoHabiente");
                        }
                    }

                }
                else
                {
                    return RedirectToAction("BuscarPaciente", "DerechoHabiente");
                }
        }



        public int buscaNotaEvolucion(int notaEvoId)
        {
            //Buscar id de la nota de evolucion
            var notaEvo = (from a in db.NotaEvolucion
                               where a.id == notaEvoId
                           select a).FirstOrDefault();

            var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var fechaDT = DateTime.Parse(fecha);

            int claveNE = 0;

            if (notaEvo != null)
            {
                claveNE = notaEvo.id;
            }
            
            return claveNE;
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


        [HttpPost]
        public ActionResult Create(NotaEvolucion notaEvolucion, string expediente, string clave1, string clave2, string clave3, string clave4, string clave5)
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

                //Buscamos si a ese px se le acaba de crear registro en la tbl NotaEvolucion.
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.NotaEvolucion
                                               where a.num_exp == paciente.Expediente
                                               select a).
                              OrderByDescending(r => r.fecha)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;

                    //si NO existe registro en la bd en la tbl NotaEvolucion por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        //calcular la fecha actual y luego restarle 3 horas para obtener la fecha límite para las últimas 3 horas.
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-3);

                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas
                        pacienteTieneRegistroEnUltimas3Horas = db.NotaEvolucion
                        .Any(r => r.num_exp == paciente.Expediente && r.fecha >= fechaL && r.fecha <= fechaActual);
                    }

                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.NotaEvolucion
                                                .Where(r => r.num_exp == paciente.Expediente && r.fecha <= fechaLimite && r.fecha <= fechaDT)
                                                .OrderByDescending(r => r.fecha)
                                                .FirstOrDefault();

                        // Actualiza el último registro con los datos proporcionados
                        registroReciente.nota_subjetivo = notaEvolucion.nota_subjetivo;
                        registroReciente.nota_objetivo = notaEvolucion.nota_objetivo;
                        registroReciente.nota_plan = notaEvolucion.nota_plan;
                        registroReciente.diagnostico1 = clave1;
                        registroReciente.diagnostico2 = clave2;
                        registroReciente.diagnostico3 = clave3;
                        registroReciente.diagnostico4 = clave4;
                        registroReciente.diagnostico5 = clave5;
                        registroReciente.tipo_diagnostico1 = notaEvolucion.tipo_diagnostico1;
                        registroReciente.tipo_diagnostico2 = notaEvolucion.tipo_diagnostico2;
                        registroReciente.tipo_diagnostico3 = notaEvolucion.tipo_diagnostico3;
                        registroReciente.tipo_diagnostico4 = notaEvolucion.tipo_diagnostico4;
                        registroReciente.tipo_diagnostico5 = notaEvolucion.tipo_diagnostico5;
                        registroReciente.num_exp = paciente.Expediente;
                        registroReciente.medico = username;
                        registroReciente.fecha = fechaDT;
                        db.Entry(registroReciente).State = EntityState.Modified;
                        db.SaveChanges();

                        TempData["message_success"] = "Nota de evolución editada con éxito";
                        return Redirect(Request.UrlReferrer.ToString());

                        //Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        //Se crea la HC de esta sección/pestaña
                        NotaEvolucion nota = new NotaEvolucion();
                        nota.nota_subjetivo = notaEvolucion.nota_subjetivo;
                        nota.nota_objetivo = notaEvolucion.nota_objetivo;
                        nota.nota_plan = notaEvolucion.nota_plan;
                        nota.diagnostico1 = clave1;
                        nota.diagnostico2 = clave2;
                        nota.diagnostico3 = clave3;
                        nota.diagnostico4 = clave4;
                        nota.diagnostico5 = clave5;
                        nota.tipo_diagnostico1 = notaEvolucion.tipo_diagnostico1;
                        nota.tipo_diagnostico2 = notaEvolucion.tipo_diagnostico2;
                        nota.tipo_diagnostico3 = notaEvolucion.tipo_diagnostico3;
                        nota.tipo_diagnostico4 = notaEvolucion.tipo_diagnostico4;
                        nota.tipo_diagnostico5 = notaEvolucion.tipo_diagnostico5;
                        nota.num_exp = paciente.Expediente;
                        nota.medico = username;
                        nota.fecha = fechaDT;
                        //Historia.Id_HistoriaClinica = Id_claveHC;
                        //nota.Clave_hc_px = Id_claveHC;
                        db.NotaEvolucion.Add(nota);
                        db.SaveChanges();
                    }


                    TempData["message_success"] = "Nota de evolución terminada con éxito";
                    return Redirect(Request.UrlReferrer.ToString());

                }
                else
                {
                    TempData["message_error"] = "Paciente no encontrado";
                    return Redirect(Request.UrlReferrer.ToString());
                }


            }
            catch (Exception ex)
            {
                //return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
                TempData["message_success"] = "Error, vuelve a intentar";
                return Redirect(Request.UrlReferrer.ToString());
            }
        }


        public class NotaEvolucionLista
        {
            public string Expediente { get; set; }
            public string Paciente { get; set; }
            public string Medico { get; set; }
            public string Fecha { get; set; }
            public string Boton { get; set; }
        }


        public JsonResult ListaNotaEvolucion(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.NotaEvolucion
                                join pa in db.Paciente on ne.num_exp equals pa.Expediente into pax
                                from paIn in pax.DefaultIfEmpty()
                                where ne.medico == username
                                //where ne.num_exp == expediente
                                select new
                                {
                                    Expediente = ne.num_exp,
                                    Paciente = paIn.Nombre+" "+ paIn.PrimerApellido + " " + paIn.SegundoApellido,
                                    Medico = ne.medico,
                                    Fecha = ne.fecha,
                                    Boton = ne.id,

                                }).ToList().OrderByDescending(n => n.Fecha);



            var listaNotas = new List<NotaEvolucionLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new NotaEvolucionLista
                {
                    //Expediente = item.Expediente,
                    Paciente = item.Paciente,
                    Medico = item.Medico,
                    Fecha = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.Fecha, new CultureInfo("es-ES")),
                    Boton = "<button data-id='" + item.Boton + "' class='btn btn-primary vermas'>Ver más</button>",
                };

                listaNotas.Add(listaLlenar);
            }



            return new JsonResult { Data = listaNotas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        public JsonResult ListaNotaEvolucionPx(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.NotaEvolucion
                         join pa in db.Paciente on ne.num_exp equals pa.Expediente into pax
                         from paIn in pax.DefaultIfEmpty()
                         where ne.medico == username
                         where ne.num_exp == expediente
                         select new
                         {
                             Expediente = ne.num_exp,
                             Paciente = paIn.Nombre + " " + paIn.PrimerApellido + " " + paIn.SegundoApellido,
                             Medico = ne.medico,
                             Fecha = ne.fecha,
                             Boton = ne.id,

                         }).ToList().OrderByDescending(n => n.Fecha);



            var listaNotas = new List<NotaEvolucionLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new NotaEvolucionLista
                {
                    //Expediente = item.Expediente,
                    Paciente = item.Paciente,
                    Medico = item.Medico,
                    Fecha = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.Fecha, new CultureInfo("es-ES")),
                    Boton = "<button data-id='" + item.Boton + "' class='btn btn-primary vermas'>Ver más</button>",
                };

                listaNotas.Add(listaLlenar);
            }



            return new JsonResult { Data = listaNotas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        public JsonResult DtsNotaEvolucion(int? id)
        {

            var nota = (from ne in db.NotaEvolucion
                        join diag1 in db.Diagnosticos on ne.diagnostico1 equals diag1.clave into diagX1
                        from diaIn1 in diagX1.DefaultIfEmpty()
                        join diag2 in db.Diagnosticos on ne.diagnostico2 equals diag2.clave into diagX2
                        from diaIn2 in diagX2.DefaultIfEmpty()
                        join diag3 in db.Diagnosticos on ne.diagnostico3 equals diag3.clave into diagX3
                        from diaIn3 in diagX3.DefaultIfEmpty()
                        join diag4 in db.Diagnosticos on ne.diagnostico4 equals diag4.clave into diagX4
                        from diaIn4 in diagX4.DefaultIfEmpty()
                        join diag5 in db.Diagnosticos on ne.diagnostico5 equals diag5.clave into diagX5
                        from diaIn5 in diagX5.DefaultIfEmpty()
                        where ne.id == id
                         select new
                         {
                             nota_subjetivo = ne.nota_subjetivo,
                             nota_objetivo = ne.nota_objetivo,
                             nota_plan = ne.nota_plan,
                             diagnostico1 = diaIn1.diagnostico,
                             diagnostico2 = diaIn2.diagnostico,
                             diagnostico3 = diaIn3.diagnostico,
                             diagnostico4 = diaIn4.diagnostico,
                             diagnostico5 = diaIn5.diagnostico,

                         }).FirstOrDefault();

            var rst = new Object();

            if(nota != null)
            {

                var diagnosticos = "";
                if (nota.diagnostico5 != null && nota.diagnostico5 != "")
                {
                    diagnosticos = "<li>" + nota.diagnostico1 + "</li>" + "<li>" + nota.diagnostico2 + "</li>" + "<li>" + nota.diagnostico3 + "</li>" + "<li>" + nota.diagnostico4 + "</li>" + "<li>" + nota.diagnostico5 + "</li>";
                }
                else
                {
                    if (nota.diagnostico4 != null && nota.diagnostico4 != "")
                    {
                        diagnosticos = "<li>" + nota.diagnostico1 + "</li>" + "<li>" + nota.diagnostico2 + "</li>" + "<li>" + nota.diagnostico3 + "</li>" + "<li>" + nota.diagnostico4 + "</li>";
                    }
                    else
                    {
                        if (nota.diagnostico3 != null && nota.diagnostico3 != "")
                        {
                            diagnosticos = "<li>" + nota.diagnostico1 + "</li>" + "<li>" + nota.diagnostico2 + "</li>" + "<li>" + nota.diagnostico3 + "</li>";
                        }
                        else
                        {
                            if (nota.diagnostico2 != null && nota.diagnostico2 != "")
                            {
                                diagnosticos = "<li>" + nota.diagnostico1 + "</li>" + "<li>" + nota.diagnostico2 + "</li>";
                            }
                            else
                            {
                                diagnosticos = "<li>" + nota.diagnostico1 + "</li>";
                            }
                        }
                    }
                }

                rst = new
                {
                    nota_subjetivo = nota.nota_subjetivo,
                    nota_objetivo = nota.nota_objetivo,
                    nota_plan = nota.nota_plan,
                    nota_analisis = diagnosticos,
                };
            }

            return new JsonResult { Data = rst, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}