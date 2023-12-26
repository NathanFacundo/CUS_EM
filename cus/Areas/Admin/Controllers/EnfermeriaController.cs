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
    public class EnfermeriaController : Controller
    {

        Models.CUS db = new Models.CUS();

        // GET: Admin/Enfermeria
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


        [HttpGet]
        public ActionResult Create(string expediente)
        {
            if (User.IsInRole("Enfermeria")) {
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
            }else{
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }

        }


        [HttpGet]
        public ActionResult Paciente(string expediente)
        {
            //var mensajeGlobal = BuscarPaciente();
            //var mensajeGlobal = _dhController.BuscarPaciente();
            if (User.IsInRole("Enfermeria"))
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
        public ActionResult Create(SignosVitales signosVitales, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var ip_realiza = Request.UserHostAddress;

                //Buscamos al px del que se le quiere hacer la Nota de Evolucion.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl NotaEvolucion.
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.SignosVitales
                                               where a.expediente == paciente.Expediente
                                               select a).
                              OrderByDescending(r => r.fecha)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    var userName = User.Identity.GetUserName();

                    //si NO existe registro en la bd en la tbl NotaEvolucion por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        //calcular la fecha actual y luego restarle 3 horas para obtener la fecha límite para las últimas 3 horas.
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);

                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas
                        pacienteTieneRegistroEnUltimas3Horas = db.SignosVitales
                        .Any(r => r.expediente == paciente.Expediente && r.fecha >= fechaL && r.fecha <= fechaActual);
                    }

                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroSignos = db.SignosVitales
                                                .Where(r => r.expediente == paciente.Expediente && r.fecha <= fechaLimite && r.fecha <= fechaDT)
                                                .OrderByDescending(r => r.fecha)
                                                .FirstOrDefault();

                        // Actualiza el último registro con los datos proporcionados
                        registroSignos.expediente = signosVitales.expediente;
                        registroSignos.usuario = userName;
                        registroSignos.escala_dolor = signosVitales.escala_dolor;
                        registroSignos.peso = signosVitales.peso;
                        registroSignos.talla = signosVitales.talla;
                        registroSignos.temperatura = signosVitales.temperatura;
                        registroSignos.fresp = signosVitales.fresp;
                        registroSignos.fcard = signosVitales.fcard;
                        registroSignos.ta1 = signosVitales.ta1;
                        registroSignos.ta2 = signosVitales.ta2;
                        registroSignos.dstx = signosVitales.dstx;
                        registroSignos.fecha = fechaDT;
                        registroSignos.ip_realiza = ip_realiza;
                        db.Entry(registroSignos).State = EntityState.Modified;
                        db.SaveChanges();

                        TempData["message_success"] = "Signos vitales editados con éxito";
                        return Redirect(Request.UrlReferrer.ToString());

                        //Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        //Se crea la HC de esta sección/pestaña
                        SignosVitales signos = new SignosVitales();
                        signos.expediente = signosVitales.expediente;
                        signos.usuario = userName;
                        signos.escala_dolor = signosVitales.escala_dolor;
                        signos.peso = signosVitales.peso;
                        signos.talla = signosVitales.talla;
                        signos.temperatura = signosVitales.temperatura;
                        signos.fresp = signosVitales.fresp;
                        signos.fcard = signosVitales.fcard;
                        signos.ta1 = signosVitales.ta1;
                        signos.ta2 = signosVitales.ta2;
                        signos.dstx = signosVitales.dstx;
                        signos.fecha = fechaDT;
                        signos.ip_realiza = ip_realiza;
                        db.SignosVitales.Add(signos);
                        db.SaveChanges();
                    }


                    TempData["message_success"] = "Signos vitales guardados con éxito";
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
;
        }


        public JsonResult ConsultarSignosVitales(string expediente)
        {
            DateTime fechaActual = DateTime.Now;
            DateTime fechaL = fechaActual.AddHours(-1.5);

            var ultimoRegistro = (from a in db.SignosVitales
                                       where a.expediente == expediente
                                  select a).
                              OrderByDescending(r => r.fecha)
                              .FirstOrDefault();

            if (ultimoRegistro != null)
            {
                //Si hay un registro, revisa que sea dentro de la ultima hora y media
                if (ultimoRegistro.fecha > fechaL)
                {
                    return new JsonResult { Data = ultimoRegistro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
                return new JsonResult { Data = "", JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            }
            else
            {
                return new JsonResult { Data = "", JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            }


        }


        public class SignosLista
        {
            public string Expediente { get; set; }
            public string Paciente { get; set; }
            public string Medico { get; set; }
            public string Fecha { get; set; }
            public string Boton { get; set; }

        }


        public JsonResult ListaSignosVitales(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.SignosVitales
                         join pa in db.Paciente on ne.expediente equals pa.Expediente into pax
                         from paIn in pax.DefaultIfEmpty()
                         where ne.usuario == username
                         //where ne.num_exp == expediente
                         select new
                         {
                             Expediente = ne.expediente,
                             Paciente = paIn.Nombre + " " + paIn.PrimerApellido + " " + paIn.SegundoApellido,
                             Medico = ne.usuario,
                             fecha = ne.fecha,
                             Boton = ne.id,

                         }).ToList().OrderByDescending(n => n.fecha);



            var listaNotas = new List<SignosLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new SignosLista
                {
                    //Expediente = item.Expediente,
                    Paciente = item.Paciente,
                    Medico = item.Medico,
                    Fecha = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.fecha, new CultureInfo("es-ES")),
                    Boton = "<button data-id='" + item.Boton + "' class='btn btn-primary vermas'>Ver más</button>",
                };

                listaNotas.Add(listaLlenar);
            }



            return new JsonResult { Data = listaNotas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        public JsonResult ListaSignosVitalesPx(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.SignosVitales
                         join pa in db.Paciente on ne.expediente equals pa.Expediente into pax
                         from paIn in pax.DefaultIfEmpty()
                         where ne.usuario == username
                         where ne.expediente == expediente
                         select new
                         {
                             Expediente = ne.expediente,
                             Paciente = paIn.Nombre + " " + paIn.PrimerApellido + " " + paIn.SegundoApellido,
                             Medico = ne.usuario,
                             Fecha = ne.fecha,
                             Boton = ne.id,

                         }).ToList().OrderByDescending(n => n.Fecha);



            var listaNotas = new List<SignosLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new SignosLista
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



        public JsonResult DtsSignosVitales(int? id)
        {

            var archivo = (from ar in db.SignosVitales
                           join paci in db.Paciente on ar.expediente equals paci.Expediente into paciX
                           from paciIn in paciX.DefaultIfEmpty()
                           where ar.id == id
                           select new
                           {
                               escala_dolor = ar.escala_dolor,
                               peso = ar.peso,
                               talla = ar.talla,
                               temperatura = ar.temperatura,
                               fresp = ar.fresp,
                               fcard = ar.fcard,
                               ta1 = ar.ta1,
                               ta2 = ar.ta2,
                               dstx = ar.dstx,
                               fecha = ar.fecha,

                           }).FirstOrDefault();

            var rst = new Object();

            if (archivo != null)
            {

                rst = new
                {
                    escala_dolor = archivo.escala_dolor,
                    peso = archivo.peso,
                    talla = archivo.talla,
                    temperatura = archivo.temperatura,
                    fresp = archivo.fresp,
                    fcard = archivo.fcard,
                    ta1 = archivo.ta1,
                    ta2 = archivo.ta2,
                    dstx = archivo.dstx,
                    fecha = string.Format("{0:dddd, dd MMMM yyyy}", archivo.fecha, new CultureInfo("es-ES")),
                };
            }

            return new JsonResult { Data = rst, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }




    }
}