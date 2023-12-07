
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace CUS.Areas.Admin.Controllers
{
    public class HistoriaMedicinaController : Controller
    {
        Models.CUS db = new Models.CUS();
        Models.HC_Medicina hcMed = new Models.HC_Medicina();

        // GET: Admin/HistoriaMedicina
        public ActionResult Index(string expediente)
        {
            if (User.IsInRole("Expediente"))
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

        //********      Función para Guarda nueva HC MEDICINA
        public string buscaHisotriaClinica(string expediente)
        {
            var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var fechaDT = DateTime.Parse(fecha);
            var claveHC = "";
            
            //Busca id de paciente
            var paciente = (from a in db.Paciente
                            where a.Expediente == expediente
                            select a).FirstOrDefault();

            if (paciente != null)
            {
                //Busca el ultimo registro del Historia Clinica
                int idConsecutivo = 1;
                var hcId = (from a in db.HistoriaClinica
                            select a).
                                OrderByDescending(g => g.Id)
                                .FirstOrDefault();
                if (hcId != null)
                {
                    idConsecutivo = hcId.Id + 1;
                }

                //Buscamos la ULTIMA hc común del px para obtener el Identificador y hacer match en el registro de la hc Medicina
                var Ultima_HCcomun = (from a in db.HistoriaClinica
                                      where a.Id_Paciente == paciente.Id
                                      where a.TipoHistoria == "Común" || a.TipoHistoria == "Datos Grales."
                                      select a).
                                OrderByDescending(g => g.Id)
                                .FirstOrDefault();

                Models.HistoriaClinica hc = new Models.HistoriaClinica();
                hc.Clave_hc_px = paciente.Expediente + "HC" + idConsecutivo;
                hc.Medico = User.Identity.GetUserName();
                hc.FechaRegistroHC = fechaDT;
                hc.Id_Paciente = paciente.Id;
                hc.TipoHistoria = "Medicina";
                hc.Ident_HCcomun = Ultima_HCcomun.Clave_hc_px;//Este es el identificador de la ultima HC Común, que hará matcha con la HC Medicina
                db.HistoriaClinica.Add(hc);
                db.SaveChanges();

                claveHC = paciente.Expediente + "HC" + idConsecutivo;
            }
            return claveHC;
        }

        #region Guardar Pestañas de la H.C. Medicina

        [HttpPost]
        public ActionResult HabitusExterior(Models.hc_MED_HabitusExterior HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                
                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_HabitusExterior Historia = new Models.hc_MED_HabitusExterior();
                    Historia.BuenEstadoGral = HistoriaClinica.BuenEstadoGral;
                    Historia.Tranquilo = HistoriaClinica.Tranquilo;
                    Historia.Cooperador = HistoriaClinica.Cooperador;
                    Historia.Orientado = HistoriaClinica.Orientado;
                    Historia.FaciesCaract = HistoriaClinica.FaciesCaract;
                    Historia.Depresivo = HistoriaClinica.Depresivo;
                    Historia.Ansioso = HistoriaClinica.Ansioso;
                    Historia.Agresivo = HistoriaClinica.Agresivo;
                    Historia.Temeroso = HistoriaClinica.Temeroso;
                    Historia.Irritable = HistoriaClinica.Irritable;
                    Historia.Marcha = HistoriaClinica.Marcha;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_HabitusExterior.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Habitos(Models.hc_MED_Habitos HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Habitos Historia = new Models.hc_MED_Habitos();
                    Historia.HorasSuenio = HistoriaClinica.HorasSuenio;
                    Historia.TieneInsomnio = HistoriaClinica.TieneInsomnio;
                    Historia.TieneEuresis = HistoriaClinica.TieneEuresis;
                    Historia.TienePesadillas = HistoriaClinica.TienePesadillas;
                    Historia.HorasOcio = HistoriaClinica.HorasOcio;
                    Historia.ActividadFisica = HistoriaClinica.ActividadFisica;
                    Historia.ActFi_Tiempo = HistoriaClinica.ActFi_Tiempo;
                    Historia.ActFi_Frecuencia = HistoriaClinica.ActFi_Frecuencia;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Habitos.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AntecedentesPerinatales(Models.hc_MED_AntecedentesPeri HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_AntecedentesPeri Historia = new Models.hc_MED_AntecedentesPeri();
                    Historia.NumeroEmbarazo = HistoriaClinica.NumeroEmbarazo;
                    Historia.EnfermedadesEmbarazo = HistoriaClinica.EnfermedadesEmbarazo;
                    Historia.Especifica_EnfermedadesEmb = HistoriaClinica.Especifica_EnfermedadesEmb;
                    Historia.TratamientosEmbarazo = HistoriaClinica.TratamientosEmbarazo;
                    Historia.Especifica_TratamientosEmb = HistoriaClinica.Especifica_TratamientosEmb;
                    Historia.LugarParto = HistoriaClinica.LugarParto;
                    Historia.Otra_LugarParto = HistoriaClinica.Otra_LugarParto;
                    Historia.EdadGestional = HistoriaClinica.EdadGestional;
                    Historia.Especifica_EdadGes = HistoriaClinica.Especifica_EdadGes;
                    Historia.Apgar = HistoriaClinica.Apgar;
                    Historia.Especifica_Apgar = HistoriaClinica.Especifica_Apgar;
                    Historia.TipoParto = HistoriaClinica.TipoParto;
                    Historia.PartoFue_TipoParto = HistoriaClinica.PartoFue_TipoParto;
                    Historia.Distocica_TipoParto = HistoriaClinica.Distocica_TipoParto;
                    Historia.Cesaria_TipoParto = HistoriaClinica.Cesaria_TipoParto;
                    Historia.ComplicacionAtenObst = HistoriaClinica.ComplicacionAtenObst;
                    Historia.Especifica_Complicacion = HistoriaClinica.Especifica_Complicacion;
                    Historia.TamizMetabolico = HistoriaClinica.TamizMetabolico;
                    Historia.Seleccione_TamizMetabolico = HistoriaClinica.Seleccione_TamizMetabolico;
                    Historia.TamizAuditivo = HistoriaClinica.TamizAuditivo;
                    Historia.Seleccione_TamizAuditivo = HistoriaClinica.Seleccione_TamizAuditivo;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_AntecedentesPeri.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Inmunizaciones(Models.hc_MED_Inmunizaciones HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Inmunizaciones Historia = new Models.hc_MED_Inmunizaciones();
                    Historia.CartillaVacunacion = HistoriaClinica.CartillaVacunacion;
                    Historia.EsquemaVacunacion = HistoriaClinica.EsquemaVacunacion;
                    Historia.Especifica_EsquemaVac = HistoriaClinica.Especifica_EsquemaVac;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Inmunizaciones.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Alimentacion(Models.hc_MED_Alimentacion HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Alimentacion Historia = new Models.hc_MED_Alimentacion();
                    Historia.TipoLactancia = HistoriaClinica.TipoLactancia;
                    Historia.TiempoLactancia = HistoriaClinica.TiempoLactancia;
                    Historia.EdadAblactacion = HistoriaClinica.EdadAblactacion;
                    Historia.AlimentosInicio = HistoriaClinica.AlimentosInicio;
                    Historia.EdadIntegracion = HistoriaClinica.EdadIntegracion;
                    Historia.MuertCuna = HistoriaClinica.MuertCuna;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Alimentacion.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult HabitosAlimenta(Models.hc_MED_HabitosAlimentacion HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_HabitosAlimentacion Historia = new Models.hc_MED_HabitosAlimentacion();
                    Historia.HabitosAlimentacion = HistoriaClinica.HabitosAlimentacion;
                    Historia.EspecificaBueno = HistoriaClinica.EspecificaBueno;
                    Historia.EspecificaRegular = HistoriaClinica.EspecificaRegular;
                    Historia.EspecificaMalo = HistoriaClinica.EspecificaMalo;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_HabitosAlimentacion.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AntecedentesDesarrollo(Models.hc_MED_AntecedentesDes HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_AntecedentesDes Historia = new Models.hc_MED_AntecedentesDes();
                    Historia.SostuvoCabeza = HistoriaClinica.SostuvoCabeza;
                    Historia.Especifica_SostuvoCab = HistoriaClinica.Especifica_SostuvoCab;
                    Historia.SeSento = HistoriaClinica.SeSento;
                    Historia.Especifica_SeSento = HistoriaClinica.Especifica_SeSento;
                    Historia.Camino = HistoriaClinica.Camino;
                    Historia.Especifica_Camino = HistoriaClinica.Especifica_Camino;
                    Historia.Habla = HistoriaClinica.Habla;
                    Historia.Especifica_Habla = HistoriaClinica.Especifica_Habla;
                    Historia.ControlEsfinteres = HistoriaClinica.ControlEsfinteres;
                    Historia.Especifica_ControlEsfin = HistoriaClinica.Especifica_ControlEsfin;
                    Historia.PruebaEDI = HistoriaClinica.PruebaEDI;
                    Historia.Especifica_PruebaEDI = HistoriaClinica.Especifica_PruebaEDI;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_AntecedentesDes.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class AntecedentesGinecoPropiedades
        {
            public string Menarquia { get; set; }
            public string Motivo_Menarquia { get; set; }
            public string Ritmo_Menarquia { get; set; }
            public string Tipo_Menarquia { get; set; }
            public string Cantidad_Menarquia { get; set; }
            public string Coloracion_Menarquia { get; set; }
            public string Especifica_Coloracion { get; set; }
            public string FenomenosAsoc { get; set; }
            public string DolorPelvico { get; set; }
            public string SangradoAnormal { get; set; }
            public string UltimoMetodoAnti { get; set; }
            public string Especifica_UltimoMetodoAnti { get; set; }
            public string SangradoPostcoito { get; set; }
            public string FlujoTransvaginal { get; set; }
            public string Gesta { get; set; }
            public string Partos { get; set; }
            public string Especifica_Partos { get; set; }
            public string Cesarea { get; set; }
            public string Especifica_Cesarea { get; set; }
            public string Abortos { get; set; }
            public string Especifica_Abortos { get; set; }
            public string HijosTerminos { get; set; }
            public string Especifica_HijosTerminos { get; set; }
            public string Prematuros { get; set; }
            public string Especifica_Prematuros { get; set; }
            public string FechaUltimoParto { get; set; }
            public DateTime? Especifica_FechaUltimoParto { get; set; }
            public string FechaUltimaCesarea { get; set; }
            public DateTime? Especifica_FechaUltimaCesarea { get; set; }
            public string Motivo_FechaUltimaCesarea { get; set; }
            public string FechaUltimoAborto { get; set; }
            public DateTime? Especifica_FechaUltimoAborto { get; set; }
            public string FechaUltimaMenstruacion { get; set; }
            public DateTime? Especifica_FechaUltimaMenstruacion { get; set; }
            public string FechaProbableParto { get; set; }
            public DateTime? Especifica_FechaProbableParto { get; set; }
            public string FechaUltimoPapanicolau { get; set; }
            public DateTime? Especifica_FechaUltimoPapanicolau { get; set; }
            public string NoRecuerda_FechaUltimoPapanicolau { get; set; }
            public string Resultado_FechaUltimoPapanicolau { get; set; }

        }

        [HttpPost]
        public ActionResult AntecedentesGineco(AntecedentesGinecoPropiedades HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_AntecedentesGinecoObs Historia = new Models.hc_MED_AntecedentesGinecoObs();
                    Historia.Menarquia = HistoriaClinica.Menarquia;
                    Historia.Motivo_Menarquia = HistoriaClinica.Motivo_Menarquia;
                    Historia.Ritmo_Menarquia = HistoriaClinica.Ritmo_Menarquia;
                    Historia.Tipo_Menarquia = HistoriaClinica.Tipo_Menarquia;
                    Historia.Cantidad_Menarquia = HistoriaClinica.Cantidad_Menarquia;
                    Historia.Coloracion_Menarquia = HistoriaClinica.Coloracion_Menarquia;
                    Historia.Especifica_Coloracion = HistoriaClinica.Especifica_Coloracion;
                    Historia.FenomenosAsoc = HistoriaClinica.FenomenosAsoc;
                    Historia.DolorPelvico = HistoriaClinica.DolorPelvico;
                    Historia.SangradoAnormal = HistoriaClinica.SangradoAnormal;
                    Historia.UltimoMetodoAnti = HistoriaClinica.UltimoMetodoAnti;
                    Historia.Especifica_UltimoMetodoAnti = HistoriaClinica.Especifica_UltimoMetodoAnti;
                    Historia.SangradoPostcoito = HistoriaClinica.SangradoPostcoito;
                    Historia.FlujoTransvaginal = HistoriaClinica.FlujoTransvaginal;
                    Historia.Gesta = HistoriaClinica.Gesta;
                    Historia.Partos = HistoriaClinica.Partos;
                    Historia.Especifica_Partos = HistoriaClinica.Especifica_Partos;
                    Historia.Cesarea = HistoriaClinica.Cesarea;
                    Historia.Especifica_Cesarea = HistoriaClinica.Especifica_Cesarea;
                    Historia.Abortos = HistoriaClinica.Abortos;
                    Historia.Especifica_Abortos = HistoriaClinica.Especifica_Abortos;
                    Historia.HijosTerminos = HistoriaClinica.HijosTerminos;
                    Historia.Especifica_HijosTerminos = HistoriaClinica.Especifica_HijosTerminos;
                    Historia.Prematuros = HistoriaClinica.Prematuros;
                    Historia.Especifica_Prematuros = HistoriaClinica.Especifica_Prematuros;
                    Historia.FechaUltimoParto = HistoriaClinica.FechaUltimoParto;
                    //Historia.Especifica_FechaUltimoParto = (DateTime)HistoriaClinica.Especifica_FechaUltimoParto;
                    if (HistoriaClinica.Especifica_FechaUltimoParto.HasValue)
                    {
                        Historia.Especifica_FechaUltimoParto = HistoriaClinica.Especifica_FechaUltimoParto.Value;
                    }
                    else
                    {
                        //Historia.Especifica_FechaUltimoParto = null; // O simplemente no asignar ningún valor si es nulo.
                    }


                    Historia.FechaUltimaCesarea = HistoriaClinica.FechaUltimaCesarea;
                    //Historia.Especifica_FechaUltimaCesarea = (DateTime)HistoriaClinica.Especifica_FechaUltimaCesarea;
                    if (HistoriaClinica.Especifica_FechaUltimaCesarea.HasValue)
                    {
                        Historia.Especifica_FechaUltimaCesarea = HistoriaClinica.Especifica_FechaUltimaCesarea.Value;
                    }
                    else
                    {
                    }
                    Historia.Motivo_FechaUltimaCesarea = HistoriaClinica.Motivo_FechaUltimaCesarea;
                    Historia.FechaUltimoAborto = HistoriaClinica.FechaUltimoAborto;
                    //Historia.Especifica_FechaUltimoAborto = (DateTime)HistoriaClinica.Especifica_FechaUltimoAborto;
                    if (HistoriaClinica.Especifica_FechaUltimoAborto.HasValue)
                    {
                        Historia.Especifica_FechaUltimoAborto = HistoriaClinica.Especifica_FechaUltimoAborto.Value;
                    }
                    else
                    {
                    }
                    Historia.FechaUltimaMenstruacion = HistoriaClinica.FechaUltimaMenstruacion;
                    //Historia.Especifica_FechaUltimaMenstruacion = (DateTime)HistoriaClinica.Especifica_FechaUltimaMenstruacion;
                    if (HistoriaClinica.Especifica_FechaUltimaMenstruacion.HasValue)
                    {
                        Historia.Especifica_FechaUltimaMenstruacion = HistoriaClinica.Especifica_FechaUltimaMenstruacion.Value;
                    }
                    else
                    {
                    }
                    Historia.FechaProbableParto = HistoriaClinica.FechaProbableParto;
                    //Historia.Especifica_FechaProbableParto = (DateTime)HistoriaClinica.Especifica_FechaProbableParto;
                    if (HistoriaClinica.Especifica_FechaProbableParto.HasValue)
                    {
                        Historia.Especifica_FechaProbableParto = HistoriaClinica.Especifica_FechaProbableParto.Value;
                    }
                    else
                    {
                    }
                    Historia.FechaUltimoPapanicolau = HistoriaClinica.FechaUltimoPapanicolau;
                    //Historia.Especifica_FechaUltimoPapanicolau = (DateTime)HistoriaClinica.Especifica_FechaUltimoPapanicolau;
                    if (HistoriaClinica.Especifica_FechaUltimoPapanicolau.HasValue)
                    {
                        Historia.Especifica_FechaUltimoPapanicolau = HistoriaClinica.Especifica_FechaUltimoPapanicolau.Value;
                    }
                    else
                    {
                    }

                    if (HistoriaClinica.NoRecuerda_FechaUltimoPapanicolau == "on")
                    {
                        Historia.NoRecuerda_FechaUltimoPapanicolau = true;
                    }
                    else
                    {
                        Historia.NoRecuerda_FechaUltimoPapanicolau = false;
                    }
                    Historia.Resultado_FechaUltimoPapanicolau = HistoriaClinica.Resultado_FechaUltimoPapanicolau;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_AntecedentesGinecoObs.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AntecedentesVidaSexual(Models.hc_MED_AntecedentesVidaSex HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_AntecedentesVidaSex Historia = new Models.hc_MED_AntecedentesVidaSex();
                    Historia.InicioVidaSexual = HistoriaClinica.InicioVidaSexual;
                    Historia.Edad_InicioVidaSexual = HistoriaClinica.Edad_InicioVidaSexual;
                    Historia.Numero_ParejasSexuales = HistoriaClinica.Numero_ParejasSexuales;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_AntecedentesVidaSex.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult PrincipioEvolucion(Models.hc_MED_PrincipioEvolEstado HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_PrincipioEvolEstado Historia = new Models.hc_MED_PrincipioEvolEstado();
                    Historia.PEE = HistoriaClinica.PEE;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_PrincipioEvolEstado.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ResultadosLaboratorio(Models.hc_MED_ResultadosLaboratorio HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_ResultadosLaboratorio Historia = new Models.hc_MED_ResultadosLaboratorio();
                    Historia.RLGR = HistoriaClinica.RLGR;
                    Historia.Especifica_RLGR = HistoriaClinica.Especifica_RLGR;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_ResultadosLaboratorio.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ImpresionDiag(Models.hc_MED_ImpresionDiag HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";

                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_ImpresionDiag Historia = new Models.hc_MED_ImpresionDiag();
                    Historia.diagnostico1 = HistoriaClinica.diagnostico1;
                    Historia.diagnostico2 = HistoriaClinica.diagnostico2;
                    Historia.diagnostico3 = HistoriaClinica.diagnostico3;
                    Historia.diagnostico4 = HistoriaClinica.diagnostico4;
                    Historia.diagnostico5 = HistoriaClinica.diagnostico5;
                    Historia.tipo_diagnostico1 = HistoriaClinica.tipo_diagnostico1;
                    Historia.tipo_diagnostico2 = HistoriaClinica.tipo_diagnostico2;
                    Historia.tipo_diagnostico3 = HistoriaClinica.tipo_diagnostico3;
                    Historia.tipo_diagnostico4 = HistoriaClinica.tipo_diagnostico4;
                    Historia.tipo_diagnostico5 = HistoriaClinica.tipo_diagnostico5;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_ImpresionDiag.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Plan(Models.hc_MED_Plan HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Plan Historia = new Models.hc_MED_Plan();
                    Historia.Plan = HistoriaClinica.Plan;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Plan.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class PronosticoPropiedades
        {
            public string LigadoEvolucion { get; set; }
            public string Favorable { get; set; }
            public string Desfavorable { get; set; }
        }

        [HttpPost]
        public ActionResult Pronostico(PronosticoPropiedades HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Pronostico Historia = new Models.hc_MED_Pronostico();
                    
                    if (HistoriaClinica.LigadoEvolucion == "on")
                    {
                        Historia.LigadoEvolucion = true;
                    }
                    else
                    {
                        Historia.LigadoEvolucion = false;
                    }
                    if (HistoriaClinica.Favorable == "on")
                    {
                        Historia.Favorable = true;
                    }
                    else
                    {
                        Historia.Favorable = false;
                    }
                    if (HistoriaClinica.Desfavorable == "on")
                    {
                        Historia.Desfavorable = true;
                    }
                    else
                    {
                        Historia.Desfavorable = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Pronostico.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Otros(Models.hc_MED_Otros HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica
                if (paciente != null)
                {
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;
                    //si NO existe registro en la bd en la tbl HistoriaClinica por default 'pacienteTieneRegistroEnUltimas3Horas' será null,
                    //quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Medicina");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 3 horas
                    {
                        //Obtenemos los datos del registro del px
                        var registroReciente = db.HistoriaClinica
                                                .Where(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fechaLimite && r.FechaRegistroHC <= fechaDT)
                                                .OrderByDescending(r => r.FechaRegistroHC)
                                                .FirstOrDefault();

                        Id_claveHC = registroReciente.Clave_hc_px;
                    }
                    else// No hay registro reciente, puedes guardar el nuevo registro.
                    {
                        string claveHC = buscaHisotriaClinica(expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    Models.hc_MED_Otros Historia = new Models.hc_MED_Otros();
                    Historia.Interconsulta = HistoriaClinica.Interconsulta;
                    Historia.PadecimientoActual = HistoriaClinica.PadecimientoActual;
                    Historia.Especifica_PadecimientoActual = HistoriaClinica.Especifica_PadecimientoActual;
                    Historia.ProximaCita = HistoriaClinica.ProximaCita;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_MED_Otros.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        public class Propiedades_HC
        {
            //HabitusExterior
            public string BuenEstadoGral { get; set; }
            public string Tranquilo { get; set; }
            public string Cooperador { get; set; }
            public string Orientado { get; set; }
            public string FaciesCaract { get; set; }
            public string Depresivo { get; set; }
            public string Ansioso { get; set; }
            public string Agresivo { get; set; }
            public string Temeroso { get; set; }
            public string Irritable { get; set; }
            public string Marcha { get; set; }
            //Habitos
            public string HorasSuenio { get; set; }
            public string TieneInsomnio { get; set; }
            public string TieneEuresis { get; set; }
            public string TienePesadillas { get; set; }
            public string HorasOcio { get; set; }
            public string ActividadFisica { get; set; }
            public string ActFi_Tiempo { get; set; }
            public string ActFi_Frecuencia { get; set; }
            //AntecedentesPeri
            public int? NumeroEmbarazo { get; set; }
            public string EnfermedadesEmbarazo { get; set; }
            public string Especifica_EnfermedadesEmb { get; set; }
            public string TratamientosEmbarazo { get; set; }
            public string Especifica_TratamientosEmb { get; set; }
            public string LugarParto { get; set; }
            public string Otra_LugarParto { get; set; }
            public string EdadGestional { get; set; }
            public string Especifica_EdadGes { get; set; }
            public string Apgar { get; set; }
            public string Especifica_Apgar { get; set; }
            public string TipoParto { get; set; }
            public string PartoFue_TipoParto { get; set; }
            public string Distocica_TipoParto { get; set; }
            public string Cesaria_TipoParto { get; set; }
            public string ComplicacionAtenObst { get; set; }
            public string Especifica_Complicacion { get; set; }
            public string TamizMetabolico { get; set; }
            public string Seleccione_TamizMetabolico { get; set; }
            public string TamizAuditivo { get; set; }
            public string Seleccione_TamizAuditivo { get; set; }
            //Inmunizaciones
            public string CartillaVacunacion { get; set; }
            public string EsquemaVacunacion { get; set; }
            public string Especifica_EsquemaVac { get; set; }
            //Alimentacion
            public string TipoLactancia { get; set; }
            public string TiempoLactancia { get; set; }
            public string EdadAblactacion { get; set; }
            public string AlimentosInicio { get; set; }
            public string EdadIntegracion { get; set; }
            public string MuertCuna { get; set; }
            //HabitosAlimentacion
            public string HabitosAlimentacion { get; set; }
            public string EspecificaBueno { get; set; }
            public string EspecificaRegular { get; set; }
            public string EspecificaMalo { get; set; }
            //AntecedentesDes
            public string SostuvoCabeza { get; set; }
            public string Especifica_SostuvoCab { get; set; }
            public string SeSento { get; set; }
            public string Especifica_SeSento { get; set; }
            public string Camino { get; set; }
            public string Especifica_Camino { get; set; }
            public string Habla { get; set; }
            public string Especifica_Habla { get; set; }
            public string ControlEsfinteres { get; set; }
            public string Especifica_ControlEsfin { get; set; }
            public string PruebaEDI { get; set; }
            public string Especifica_PruebaEDI { get; set; }
            //AntecedentesGinecoObs
            public string Menarquia { get; set; }
            public string Motivo_Menarquia { get; set; }
            public string Ritmo_Menarquia { get; set; }
            public string Tipo_Menarquia { get; set; }
            public string Cantidad_Menarquia { get; set; }
            public string Coloracion_Menarquia { get; set; }
            public string Especifica_Coloracion { get; set; }
            public string FenomenosAsoc { get; set; }
            public string DolorPelvico { get; set; }
            public string SangradoAnormal { get; set; }
            public string UltimoMetodoAnti { get; set; }
            public string Especifica_UltimoMetodoAnti { get; set; }
            public string SangradoPostcoito { get; set; }
            public string FlujoTransvaginal { get; set; }
            public string Gesta { get; set; }
            public string Partos { get; set; }
            public string Especifica_Partos { get; set; }
            public string Cesarea { get; set; }
            public string Especifica_Cesarea { get; set; }
            public string Abortos { get; set; }
            public string Especifica_Abortos { get; set; }
            public string HijosTerminos { get; set; }
            public string Especifica_HijosTerminos { get; set; }
            public string Prematuros { get; set; }
            public string Especifica_Prematuros { get; set; }
            public string FechaUltimoParto { get; set; }
            public DateTime? Especifica_FechaUltimoParto { get; set; }
            public string FechaUltimaCesarea { get; set; }
            public DateTime? Especifica_FechaUltimaCesarea { get; set; }
            public string Motivo_FechaUltimaCesarea { get; set; }
            public string FechaUltimoAborto { get; set; }
            public DateTime? Especifica_FechaUltimoAborto { get; set; }
            public string FechaUltimaMenstruacion { get; set; }
            public DateTime? Especifica_FechaUltimaMenstruacion { get; set; }
            public string FechaProbableParto { get; set; }
            public DateTime? Especifica_FechaProbableParto { get; set; }
            public string FechaUltimoPapanicolau { get; set; }
            public DateTime? Especifica_FechaUltimoPapanicolau { get; set; }
            public bool? NoRecuerda_FechaUltimoPapanicolau { get; set; }
            public string Resultado_FechaUltimoPapanicolau { get; set; }
            //AntecedentesVidaSex
            public string InicioVidaSexual { get; set; }
            public string Edad_InicioVidaSexual { get; set; }
            public string Numero_ParejasSexuales { get; set; }
            //PrincipioEvolEstado
            public string PEE { get; set; }
            //ResultadosLaboratorio
            public string RLGR { get; set; }
            public string Especifica_RLGR { get; set; }
            //ImpresionDiag
            public string Diagnostico1 { get; set; }
            public string Diagnostico2 { get; set; }
            public string Diagnostico3 { get; set; }
            public string Diagnostico4 { get; set; }
            public string Diagnostico5 { get; set; }
            public string tipo_diagnostico1 { get; set; }
            public string tipo_diagnostico2 { get; set; }
            public string tipo_diagnostico3 { get; set; }
            public string tipo_diagnostico4 { get; set; }
            public string tipo_diagnostico5 { get; set; }
            //Plan
            public string Plan { get; set; }
            //Pronostico
            public bool? LigadoEvolucion { get; set; }
            public bool? Favorable { get; set; }
            public bool? Desfavorable { get; set; }
            //Otros
            public string Interconsulta { get; set; }
            public string PadecimientoActual { get; set; }
            public string Especifica_PadecimientoActual { get; set; }
            public string ProximaCita { get; set; }
        }

        //********      Función para buscar el detalle de la H.C. en el MODAL
        [HttpPost]
        public ActionResult ConsultarHC_Med(string Clave_hc_px, string TipoHistoria)//Este parametro lo recivimos de la vista, "Clave_hc_px" viene siendo el Identificador armado de la HC que se desea ver
        {
            try
            {
                Propiedades_HC HC = new Propiedades_HC();

                string query =
                    "SELECT HE.BuenEstadoGral, HE.Tranquilo, HE.Cooperador, HE.Orientado, HE.FaciesCaract, HE.Depresivo, HE.Ansioso, HE.Agresivo, HE.Temeroso, HE.Irritable, HE.Marcha, " +
                    "H.HorasSuenio, H.TieneInsomnio, H.TieneEuresis, H.TienePesadillas, H.HorasOcio, H.ActividadFisica, H.ActFi_Tiempo, H.ActFi_Frecuencia, " +
                    "AP.NumeroEmbarazo, AP.EnfermedadesEmbarazo, AP.Especifica_EnfermedadesEmb, AP.TratamientosEmbarazo, AP.Especifica_TratamientosEmb, AP.LugarParto, AP.Otra_LugarParto, AP.EdadGestional, AP.Especifica_EdadGes, AP.Apgar, AP.Especifica_Apgar, AP.TipoParto, AP.PartoFue_TipoParto, AP.Distocica_TipoParto, AP.Cesaria_TipoParto, AP.ComplicacionAtenObst, AP.Especifica_Complicacion, AP.TamizMetabolico, AP.Seleccione_TamizMetabolico, AP.TamizAuditivo, AP.Seleccione_TamizAuditivo, " +
                    "I.CartillaVacunacion, I.EsquemaVacunacion, I.Especifica_EsquemaVac, " +
                    "A.TipoLactancia, A.TiempoLactancia, A.EdadAblactacion, A.AlimentosInicio, A.EdadIntegracion, A.MuertCuna, " +
                    "HA.HabitosAlimentacion, HA.EspecificaBueno, HA.EspecificaRegular, HA.EspecificaMalo, " +
                    "AD.SostuvoCabeza, AD.Especifica_SostuvoCab, AD.SeSento, AD.Especifica_SeSento, AD.Camino, AD.Especifica_Camino, AD.Habla, AD.Especifica_Habla, AD.ControlEsfinteres, AD.Especifica_ControlEsfin, AD.PruebaEDI, AD.Especifica_PruebaEDI, " +
                    "AGO.Menarquia, AGO.Motivo_Menarquia, AGO.Ritmo_Menarquia, AGO.Tipo_Menarquia, AGO.Cantidad_Menarquia, AGO.Coloracion_Menarquia, AGO.Especifica_Coloracion, AGO.FenomenosAsoc, AGO.DolorPelvico, AGO.SangradoAnormal, AGO.UltimoMetodoAnti, AGO.Especifica_UltimoMetodoAnti, AGO.SangradoPostcoito, AGO.FlujoTransvaginal, AGO.Gesta, AGO.Partos, AGO.Especifica_Partos, AGO.Cesarea, AGO.Especifica_Cesarea, AGO.Abortos, AGO.Especifica_Abortos, AGO.HijosTerminos, AGO.Especifica_HijosTerminos, AGO.Prematuros, AGO.Especifica_Prematuros, AGO.FechaUltimoParto, AGO.Especifica_FechaUltimoParto, AGO.FechaUltimaCesarea, AGO.Especifica_FechaUltimaCesarea, AGO.Motivo_FechaUltimaCesarea, AGO.FechaUltimoAborto, AGO.Especifica_FechaUltimoAborto, AGO.FechaUltimaMenstruacion, AGO.Especifica_FechaUltimaMenstruacion, AGO.FechaProbableParto, AGO.Especifica_FechaProbableParto, AGO.FechaUltimoPapanicolau, AGO.Especifica_FechaUltimoPapanicolau, AGO.NoRecuerda_FechaUltimoPapanicolau, AGO.Resultado_FechaUltimoPapanicolau, " +
                    "AVS.InicioVidaSexual, AVS.Edad_InicioVidaSexual, AVS.Numero_ParejasSexuales, " +
                    "PEE.PEE, " +
                    "RL.RLGR, RL.Especifica_RLGR, " +
                    "ID.Diagnostico1, ID.Diagnostico2, ID.Diagnostico3, ID.Diagnostico4, ID.Diagnostico5, ID.tipo_diagnostico1, ID.tipo_diagnostico2, ID.tipo_diagnostico3, ID.tipo_diagnostico4, ID.tipo_diagnostico5, " +
                    "PL.[Plan], " +
                    "PR.LigadoEvolucion, PR.Favorable, PR.Desfavorable, " +
                    "O.Interconsulta, O.PadecimientoActual, O.Especifica_PadecimientoActual, O.ProximaCita " +
                                    "FROM HistoriaClinica HCli " +
                                    "LEFT JOIN hc_MED_HabitusExterior HE ON HE.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Habitos H ON H.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_AntecedentesPeri AP ON AP.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Inmunizaciones I ON I.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Alimentacion A ON A.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_HabitosAlimentacion HA ON HA.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_AntecedentesDes AD ON AD.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_AntecedentesGinecoObs AGO ON AGO.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_AntecedentesVidaSex AVS ON AVS.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_PrincipioEvolEstado PEE ON PEE.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_ResultadosLaboratorio RL ON RL.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_ImpresionDiag ID ON ID.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Plan PL ON PL.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Pronostico PR ON PR.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_MED_Otros O ON O.Clave_hc_px = HCli.Clave_hc_px " +
                                    "WHERE HCli.Clave_hc_px = '" + Clave_hc_px + "' ";

                var result = hcMed.Database.SqlQuery<Propiedades_HC>(query);
                HC = result.FirstOrDefault();

                return new JsonResult { Data = HC, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}
