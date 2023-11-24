using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CUS.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace CUS.Areas.Admin.Controllers
{
    public class HistoriaClinicaController : Controller
    {

        Models.CUS db = new Models.CUS();

        // GET: Admin/HistoriaClinica
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

        //********      Crear registro en tbl HISTORIA CLINICA (nueva)
        public string buscaHisotriaClinica(int HistoriaClinica, string expediente)
        {
            //Buscar id del historia clinica
            var histClinica = (from a in db.HistoriaClinica
                               where a.Id == HistoriaClinica
                               select a).FirstOrDefault();

            var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var fechaDT = DateTime.Parse(fecha);

            var claveHC = "";

            if (histClinica == null)
            {
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

                    //Guarda en tabla de historia clinica
                    HistoriaClinica hc = new HistoriaClinica();
                    hc.Clave_hc_px = paciente.Expediente + "HC" + idConsecutivo;
                    hc.Medico = User.Identity.GetUserName();
                    hc.FechaRegistroHC = fechaDT;
                    hc.Id_Paciente = paciente.Id;
                    hc.TipoHistoria = "Datos Grales.";
                    db.HistoriaClinica.Add(hc);
                    db.SaveChanges();

                    claveHC = paciente.Expediente + "HC" + idConsecutivo;
                }
            }
            else
            {
                claveHC = histClinica.Clave_hc_px;
            }
            return claveHC;
        }

        #region Guardar Pestañas de la H.C. Común

        [HttpPost]
        public ActionResult pat_prescriptions(hc_ficha_identificacion HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //DateTime fechaLimite = (DateTime)fechaUltimoRegistro.FechaRegistroHC;
                    //DateTime fl3horas = fechaLimite.AddHours(+3);
                    //bool pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                    //.Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fl3horas && r.FechaRegistroHC >= fechaDT);

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
                    if (fechaUltimoRegistro == null)
                    {
                        pacienteTieneRegistroEnUltimas3Horas = false;
                    }
                    else
                    {
                        //fechaLimite = (DateTime)fechaUltimoRegistro.FechaRegistroHC;
                        //DateTime fl3horas = fechaLimite.AddHours(+3);
                        //calcular la fecha actual y luego restarle 3 horas para obtener la fecha límite para las últimas 3 horas.
                        DateTime fechaActual = DateTime.Now;
                        DateTime fechaL = fechaActual.AddHours(-1.5);

                        //pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        //.Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC <= fl3horas && r.FechaRegistroHC <= fechaDT);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_ficha_identificacion Historia = new hc_ficha_identificacion();
                    Historia.acompanante = HistoriaClinica.acompanante;
                    Historia.NombreAcompa = HistoriaClinica.NombreAcompa;
                    Historia.alergia = HistoriaClinica.alergia;
                    Historia.NombreAlergia = HistoriaClinica.NombreAlergia;
                    Historia.MotivoCons = HistoriaClinica.MotivoCons;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_ficha_identificacion.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult evaluacionsocial(hc_evaluacion_social HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

                //if(HistoriaClinica.Id_HistoriaClinica == null)
                //{
                //    id_hc = 0;
                //}
                //else
                //{
                //    id_hc = (int)HistoriaClinica.Id_HistoriaClinica;
                //}

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_evaluacion_social Historia = new hc_evaluacion_social();
                    Historia.estadocivil = HistoriaClinica.estadocivil;
                    Historia.numeropersonas = HistoriaClinica.numeropersonas;
                    Historia.numerohabitaciones = HistoriaClinica.numerohabitaciones;
                    Historia.Ocupacio = HistoriaClinica.Ocupacio;
                    Historia.hacinamiento = HistoriaClinica.hacinamiento;
                    Historia.escolaridad = HistoriaClinica.escolaridad;
                    Historia.GradoEsc = HistoriaClinica.GradoEsc;
                    Historia.PromedioEsc = HistoriaClinica.PromedioEsc;
                    Historia.reprobado = HistoriaClinica.reprobado;
                    Historia.EspecifiqueReprobado = HistoriaClinica.EspecifiqueReprobado;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_evaluacion_social.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult valoracionfamiliar(hc_valoracion_familiar HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_valoracion_familiar Historia = new hc_valoracion_familiar();
                    Historia.mamaPaciente = HistoriaClinica.mamaPaciente;
                    Historia.EdadMama = HistoriaClinica.EdadMama;
                    Historia.GradoMama = HistoriaClinica.GradoMama;
                    Historia.OcupacionMama = HistoriaClinica.OcupacionMama;
                    Historia.TipoTrabajoMama = HistoriaClinica.TipoTrabajoMama;
                    Historia.papaPaciente = HistoriaClinica.papaPaciente;
                    Historia.EdadPapa = HistoriaClinica.EdadPapa;
                    Historia.GradoPapa = HistoriaClinica.GradoPapa;
                    Historia.OcupacionPapa = HistoriaClinica.OcupacionPapa;
                    Historia.TipoTrabajoPapa = HistoriaClinica.TipoTrabajoPapa;
                    Historia.ViveCon = HistoriaClinica.ViveCon;
                    Historia.tieneHermanos = HistoriaClinica.tieneHermanos;
                    Historia.CuantosHermanos = HistoriaClinica.CuantosHermanos;
                    Historia.EspecifiqueEnf = HistoriaClinica.EspecifiqueEnf;
                    Historia.involucraTratamiento = HistoriaClinica.involucraTratamiento;
                    Historia.EspecifiqueInvol = HistoriaClinica.EspecifiqueInvol;
                    Historia.riesgoSocial = HistoriaClinica.riesgoSocial;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_valoracion_familiar.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult evaluacioneconomica(hc_evaluacion_economica HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_evaluacion_economica Historia = new hc_evaluacion_economica();
                    Historia.beneficiarioPrograma = HistoriaClinica.beneficiarioPrograma;
                    Historia.EspecifiqueBeneficiario = HistoriaClinica.EspecifiqueBeneficiario;
                    Historia.rolEconimico = HistoriaClinica.rolEconimico;
                    Historia.EspecifiqueRol = HistoriaClinica.EspecifiqueRol;
                    Historia.riesgoEconomico = HistoriaClinica.riesgoEconomico;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_evaluacion_economica.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult valorescreencias(hc_valores_creencias HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_valores_creencias Historia = new hc_valores_creencias();
                    Historia.perteneceReligion = HistoriaClinica.perteneceReligion;
                    Historia.religion = HistoriaClinica.religion;
                    Historia.EspecifiqueReligion = HistoriaClinica.EspecifiqueReligion;
                    Historia.creenciaReligiosa = HistoriaClinica.creenciaReligiosa;
                    Historia.EspecifiqueCreencia = HistoriaClinica.EspecifiqueCreencia;
                    Historia.costumbreValores = HistoriaClinica.costumbreValores;
                    Historia.EspecifiqueCostumbres = HistoriaClinica.EspecifiqueCostumbres;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_valores_creencias.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult riesgopsicologico(hc_factores_riesgo_psicologicos HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_factores_riesgo_psicologicos Historia = new hc_factores_riesgo_psicologicos();
                    Historia.CambiosSueño = HistoriaClinica.CambiosSueño;
                    Historia.CambiosEnergia = HistoriaClinica.CambiosEnergia;
                    Historia.CambiosApetito = HistoriaClinica.CambiosApetito;
                    Historia.Pesimismo = HistoriaClinica.Pesimismo;
                    Historia.Irritabilidad = HistoriaClinica.Irritabilidad;
                    Historia.PerdidaPlacer = HistoriaClinica.PerdidaPlacer;
                    Historia.PalpitacionesFuertes = HistoriaClinica.PalpitacionesFuertes;
                    Historia.SensacionAahogo = HistoriaClinica.SensacionAahogo;
                    Historia.MiedoPreocupacion = HistoriaClinica.MiedoPreocupacion;
                    Historia.IdeacionSuicida = HistoriaClinica.IdeacionSuicida;
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_factores_riesgo_psicologicos.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class _habitos
        {
            //public string HorasSueño { get; set; }
            //public string TieneInsomnio { get; set; }
            //public string TieneEnuresis { get; set; }
            //public string TienePesadillas { get; set; }
            //public string HorasOcio { get; set; }
            //public string ActividadFisica { get; set; }
            //public string TiempoActividadFisica { get; set; }
            //public string FrecuenciaActividadFisica { get; set; }
            public string Check_Adicciones_Padre { get; set; }
            public string Check_Adicciones_Madre { get; set; }
            public string Check_Adicciones_Ambas { get; set; }
            public string Check_Adicciones_NA { get; set; }
            public string Check_Cardiopatia_Padre { get; set; }
            public string Check_Cardiopatia_Madre { get; set; }
            public string Check_Cardiopatia_Ambas { get; set; }
            public string Check_Cardiopatia_NA { get; set; }
            public string Check_Diabetes_Padre { get; set; }
            public string Check_Diabetes_Madre { get; set; }
            public string Check_Diabetes_Ambas { get; set; }
            public string Check_Diabetes_NA { get; set; }
            public string Check_Dislipidemias_Padre { get; set; }
            public string Check_Dislipidemias_Madre { get; set; }
            public string Check_Dislipidemias_Ambas { get; set; }
            public string Check_Dislipidemias_NA { get; set; }
            public string Check_Epilepsia_Padre { get; set; }
            public string Check_Epilepsia_Madre { get; set; }
            public string Check_Epilepsia_Ambas { get; set; }
            public string Check_Epilepsia_NA { get; set; }
            public string Check_Hipertension_Padre { get; set; }
            public string Check_Hipertension_Madre { get; set; }
            public string Check_Hipertension_Ambas { get; set; }
            public string Check_Hipertension_NA { get; set; }
            public string Check_Infectocontagiosas_Padre { get; set; }
            public string Check_Infectocontagiosas_Madre { get; set; }
            public string Check_Infectocontagiosas_Ambas { get; set; }
            public string Check_Infectocontagiosas_NA { get; set; }
            public string Check_Malformaciones_Padre { get; set; }
            public string Check_Malformaciones_Madre { get; set; }
            public string Check_Malformaciones_Ambas { get; set; }
            public string Check_Malformaciones_NA { get; set; }
            public string Check_Nefropatias_Padre { get; set; }
            public string Check_Nefropatias_Madre { get; set; }
            public string Check_Nefropatias_Ambas { get; set; }
            public string Check_Nefropatias_NA { get; set; }
            public string Check_Obesidad_Padre { get; set; }
            public string Check_Obesidad_Madre { get; set; }
            public string Check_Obesidad_Ambas { get; set; }
            public string Check_Obesidad_NA { get; set; }
            public string Check_Oncologicos_Padre { get; set; }
            public string Check_Oncologicos_Madre { get; set; }
            public string Check_Oncologicos_Ambas { get; set; }
            public string Check_Oncologicos_NA { get; set; }
        }

        [HttpPost]
        public ActionResult habitos(_habitos HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_habitos Historia = new hc_habitos();
                    //Historia.HorasSueño = HistoriaClinica.HorasSueño;
                    //Historia.TieneInsomnio = HistoriaClinica.TieneInsomnio;
                    //Historia.TieneEnuresis = HistoriaClinica.TieneEnuresis;
                    //Historia.HorasOcio = HistoriaClinica.HorasOcio;
                    //Historia.ActividadFisica = HistoriaClinica.ActividadFisica;
                    //Historia.TiempoActividadFisica = HistoriaClinica.TiempoActividadFisica;
                    //Historia.FrecuenciaActividadFisica = HistoriaClinica.FrecuenciaActividadFisica;
                    if (HistoriaClinica.Check_Adicciones_Padre == "on")
                    {
                        Historia.Check_Adicciones_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Adicciones_Padre = false;
                    }
                    if (HistoriaClinica.Check_Adicciones_Madre == "on")
                    {
                        Historia.Check_Adicciones_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Adicciones_Madre = false;
                    }
                    if (HistoriaClinica.Check_Adicciones_Ambas == "on")
                    {
                        Historia.Check_Adicciones_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Adicciones_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Adicciones_NA == "on")
                    {
                        Historia.Check_Adicciones_NA = true;
                    }
                    else
                    {
                        Historia.Check_Adicciones_NA = false;
                    }
                    if (HistoriaClinica.Check_Cardiopatia_Padre == "on")
                    {
                        Historia.Check_Cardiopatia_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Cardiopatia_Padre = false;
                    }
                    if (HistoriaClinica.Check_Cardiopatia_Madre == "on")
                    {
                        Historia.Check_Cardiopatia_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Cardiopatia_Madre = false;
                    }
                    if (HistoriaClinica.Check_Cardiopatia_Ambas == "on")
                    {
                        Historia.Check_Cardiopatia_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Cardiopatia_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Cardiopatia_NA == "on")
                    {
                        Historia.Check_Cardiopatia_NA = true;
                    }
                    else
                    {
                        Historia.Check_Cardiopatia_NA = false;
                    }
                    if (HistoriaClinica.Check_Diabetes_Padre == "on")
                    {
                        Historia.Check_Diabetes_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Diabetes_Padre = false;
                    }
                    if (HistoriaClinica.Check_Diabetes_Madre == "on")
                    {
                        Historia.Check_Diabetes_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Diabetes_Madre = false;
                    }
                    if (HistoriaClinica.Check_Diabetes_Ambas == "on")
                    {
                        Historia.Check_Diabetes_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Diabetes_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Diabetes_NA == "on")
                    {
                        Historia.Check_Diabetes_NA = true;
                    }
                    else
                    {
                        Historia.Check_Diabetes_NA = false;
                    }
                    if (HistoriaClinica.Check_Dislipidemias_Padre == "on")
                    {
                        Historia.Check_Dislipidemias_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Dislipidemias_Padre = false;
                    }
                    if (HistoriaClinica.Check_Dislipidemias_Madre == "on")
                    {
                        Historia.Check_Dislipidemias_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Dislipidemias_Madre = false;
                    }
                    if (HistoriaClinica.Check_Dislipidemias_Ambas == "on")
                    {
                        Historia.Check_Dislipidemias_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Dislipidemias_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Dislipidemias_NA == "on")
                    {
                        Historia.Check_Dislipidemias_NA = true;
                    }
                    else
                    {
                        Historia.Check_Dislipidemias_NA = false;
                    }
                    if (HistoriaClinica.Check_Epilepsia_Padre == "on")
                    {
                        Historia.Check_Epilepsia_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Epilepsia_Padre = false;
                    }
                    if (HistoriaClinica.Check_Epilepsia_Madre == "on")
                    {
                        Historia.Check_Epilepsia_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Epilepsia_Madre = false;
                    }
                    if (HistoriaClinica.Check_Epilepsia_Ambas == "on")
                    {
                        Historia.Check_Epilepsia_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Epilepsia_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Epilepsia_NA == "on")
                    {
                        Historia.Check_Epilepsia_NA = true;
                    }
                    else
                    {
                        Historia.Check_Epilepsia_NA = false;
                    }
                    if (HistoriaClinica.Check_Hipertension_Padre == "on")
                    {
                        Historia.Check_Hipertension_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Hipertension_Padre = false;
                    }
                    if (HistoriaClinica.Check_Hipertension_Madre == "on")
                    {
                        Historia.Check_Hipertension_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Hipertension_Madre = false;
                    }
                    if (HistoriaClinica.Check_Hipertension_Ambas == "on")
                    {
                        Historia.Check_Hipertension_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Hipertension_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Hipertension_NA == "on")
                    {
                        Historia.Check_Hipertension_NA = true;
                    }
                    else
                    {
                        Historia.Check_Hipertension_NA = false;
                    }
                    if (HistoriaClinica.Check_Infectocontagiosas_Padre == "on")
                    {
                        Historia.Check_Infectocontagiosas_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Infectocontagiosas_Padre = false;
                    }
                    if (HistoriaClinica.Check_Infectocontagiosas_Madre == "on")
                    {
                        Historia.Check_Infectocontagiosas_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Infectocontagiosas_Madre = false;
                    }
                    if (HistoriaClinica.Check_Infectocontagiosas_Ambas == "on")
                    {
                        Historia.Check_Infectocontagiosas_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Infectocontagiosas_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Infectocontagiosas_NA == "on")
                    {
                        Historia.Check_Infectocontagiosas_NA = true;
                    }
                    else
                    {
                        Historia.Check_Infectocontagiosas_NA = false;
                    }
                    if (HistoriaClinica.Check_Malformaciones_Padre == "on")
                    {
                        Historia.Check_Malformaciones_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Malformaciones_Padre = false;
                    }
                    if (HistoriaClinica.Check_Malformaciones_Madre == "on")
                    {
                        Historia.Check_Malformaciones_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Malformaciones_Madre = false;
                    }
                    if (HistoriaClinica.Check_Malformaciones_Ambas == "on")
                    {
                        Historia.Check_Malformaciones_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Malformaciones_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Malformaciones_NA == "on")
                    {
                        Historia.Check_Malformaciones_NA = true;
                    }
                    else
                    {
                        Historia.Check_Malformaciones_NA = false;
                    }
                    if (HistoriaClinica.Check_Nefropatias_Padre == "on")
                    {
                        Historia.Check_Nefropatias_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Nefropatias_Padre = false;
                    }
                    if (HistoriaClinica.Check_Nefropatias_Madre == "on")
                    {
                        Historia.Check_Nefropatias_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Nefropatias_Madre = false;
                    }
                    if (HistoriaClinica.Check_Nefropatias_Ambas == "on")
                    {
                        Historia.Check_Nefropatias_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Nefropatias_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Nefropatias_NA == "on")
                    {
                        Historia.Check_Nefropatias_NA = true;
                    }
                    else
                    {
                        Historia.Check_Nefropatias_NA = false;
                    }
                    if (HistoriaClinica.Check_Obesidad_Padre == "on")
                    {
                        Historia.Check_Obesidad_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Obesidad_Padre = false;
                    }
                    if (HistoriaClinica.Check_Obesidad_Madre == "on")
                    {
                        Historia.Check_Obesidad_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Obesidad_Madre = false;
                    }
                    if (HistoriaClinica.Check_Obesidad_Ambas == "on")
                    {
                        Historia.Check_Obesidad_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Obesidad_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Obesidad_NA == "on")
                    {
                        Historia.Check_Obesidad_NA = true;
                    }
                    else
                    {
                        Historia.Check_Obesidad_NA = false;
                    }
                    if (HistoriaClinica.Check_Oncologicos_Padre == "on")
                    {
                        Historia.Check_Oncologicos_Padre = true;
                    }
                    else
                    {
                        Historia.Check_Oncologicos_Padre = false;
                    }
                    if (HistoriaClinica.Check_Oncologicos_Madre == "on")
                    {
                        Historia.Check_Oncologicos_Madre = true;
                    }
                    else
                    {
                        Historia.Check_Oncologicos_Madre = false;
                    }
                    if (HistoriaClinica.Check_Oncologicos_Ambas == "on")
                    {
                        Historia.Check_Oncologicos_Ambas = true;
                    }
                    else
                    {
                        Historia.Check_Oncologicos_Ambas = false;
                    }
                    if (HistoriaClinica.Check_Oncologicos_NA == "on")
                    {
                        Historia.Check_Oncologicos_NA = true;
                    }
                    else
                    {
                        Historia.Check_Oncologicos_NA = false;
                    }
                    Historia.Id_Paciente = paciente.Id;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_habitos.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class _patologicos
        {
            public string Antecedentes { get; set; }
            public string PxHospitalizado { get; set; }
            public string EspecifiqueHospitalizado { get; set; }
            public string RealizadoCirugia { get; set; }
            public string EspecifiqueCirugia { get; set; }
            public string CheckRespiratorios { get; set; }
            public string CheckEndocrinologicos { get; set; }
            public string CheckCardiovasculares { get; set; }
            public string CheckOncologicos { get; set; }
            public string CheckSaludMental { get; set; }
            public string CheckNeurologicos { get; set; }
            public string CheckInfectoContagiosos { get; set; }
            public string CheckProblemasAparatoR { get; set; }
            public string CheckProblemasGastro { get; set; }
            public string CheckReumatologicos { get; set; }
            public string CheckNinguna { get; set; }
            public string DetallarRespiratorios { get; set; }
            public string DetallarEndocrinologicos { get; set; }
            public string DetallarCardiovasculares { get; set; }
            public string DetallarOncologicos { get; set; }
            public string DetallarSaludMental { get; set; }
            public string DetallarNeurologicos { get; set; }
            public string DetallarInfectoContagiosos { get; set; }
            public string DetallarProblemasAparatoR { get; set; }
            public string DetallarProblemasGastro { get; set; }
            public string DetallarReumatologicos { get; set; }
        }

        [HttpPost]
        public ActionResult patologicos(_patologicos HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

                //Buscamos al px del que se le quiere hacer la H.C.
                var paciente = (from a in db.Paciente
                                where a.Expediente == expediente
                                select a).FirstOrDefault();

                //Buscamos si a ese px se le acaba de crear registro en la tbl HistoriaClinica (en un rango de 2-3 horas)
                if (paciente != null)
                {
                    //Consultamos el último registro del px
                    var fechaUltimoRegistro = (from a in db.HistoriaClinica
                                               where a.Id_Paciente == paciente.Id
                                               select a).
                              OrderByDescending(r => r.FechaRegistroHC)
                              .FirstOrDefault();

                    bool pacienteTieneRegistroEnUltimas3Horas;
                    DateTime fechaLimite = DateTime.Now;

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_antecedentes_patologicos Historia = new hc_antecedentes_patologicos();
                    Historia.Antecedentes = HistoriaClinica.Antecedentes;
                    Historia.PxHospitalizado = HistoriaClinica.PxHospitalizado;
                    Historia.EspecifiqueHospitalizado = HistoriaClinica.EspecifiqueHospitalizado;
                    Historia.RealizadoCirugia = HistoriaClinica.RealizadoCirugia;
                    Historia.EspecifiqueCirugia = HistoriaClinica.EspecifiqueCirugia;
                    #region GUARDAR CHECKS
                    if (HistoriaClinica.CheckRespiratorios == "on")
                    {
                        Historia.CheckRespiratorios = true;
                    }
                    else
                    {
                        Historia.CheckRespiratorios = false;
                    }
                    if (HistoriaClinica.CheckEndocrinologicos == "on")
                    {
                        Historia.CheckEndocrinologicos = true;
                    }
                    else
                    {
                        Historia.CheckEndocrinologicos = false;
                    }
                    if (HistoriaClinica.CheckCardiovasculares == "on")
                    {
                        Historia.CheckCardiovasculares = true;
                    }
                    else
                    {
                        Historia.CheckCardiovasculares = false;
                    }
                    if (HistoriaClinica.CheckOncologicos == "on")
                    {
                        Historia.CheckOncologicos = true;
                    }
                    else
                    {
                        Historia.CheckOncologicos = false;
                    }
                    if (HistoriaClinica.CheckSaludMental == "on")
                    {
                        Historia.CheckSaludMental = true;
                    }
                    else
                    {
                        Historia.CheckSaludMental = false;
                    }
                    if (HistoriaClinica.CheckNeurologicos == "on")
                    {
                        Historia.CheckNeurologicos = true;
                    }
                    else
                    {
                        Historia.CheckNeurologicos = false;
                    }
                    if (HistoriaClinica.CheckInfectoContagiosos == "on")
                    {
                        Historia.CheckInfectoContagiosos = true;
                    }
                    else
                    {
                        Historia.CheckInfectoContagiosos = false;
                    }
                    if (HistoriaClinica.CheckProblemasAparatoR == "on")
                    {
                        Historia.CheckProblemasAparatoR = true;
                    }
                    else
                    {
                        Historia.CheckProblemasAparatoR = false;
                    }
                    if (HistoriaClinica.CheckProblemasGastro == "on")
                    {
                        Historia.CheckProblemasGastro = true;
                    }
                    else
                    {
                        Historia.CheckProblemasGastro = false;
                    }
                    if (HistoriaClinica.CheckReumatologicos == "on")
                    {
                        Historia.CheckReumatologicos = true;
                    }
                    else
                    {
                        Historia.CheckReumatologicos = false;
                    }
                    if (HistoriaClinica.CheckNinguna == "on")
                    {
                        Historia.CheckNinguna = true;
                    }
                    else
                    {
                        Historia.CheckNinguna = false;
                    }
                    #endregion
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    Historia.DetallarRespiratorios = HistoriaClinica.DetallarRespiratorios;
                    Historia.DetallarEndocrinologicos = HistoriaClinica.DetallarEndocrinologicos;
                    Historia.DetallarCardiovasculares = HistoriaClinica.DetallarCardiovasculares;
                    Historia.DetallarOncologicos = HistoriaClinica.DetallarOncologicos;
                    Historia.DetallarSaludMental = HistoriaClinica.DetallarSaludMental;
                    Historia.DetallarNeurologicos = HistoriaClinica.DetallarNeurologicos;
                    Historia.DetallarInfectoContagiosos = HistoriaClinica.DetallarInfectoContagiosos;
                    Historia.DetallarProblemasAparatoR = HistoriaClinica.DetallarProblemasAparatoR;
                    Historia.DetallarProblemasGastro = HistoriaClinica.DetallarProblemasGastro;
                    Historia.DetallarReumatologicos = HistoriaClinica.DetallarReumatologicos;
                    db.hc_antecedentes_patologicos.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult consumo(hc_historial_consumo HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_historial_consumo Historia = new hc_historial_consumo();
                    Historia.TomaAlcohol = HistoriaClinica.TomaAlcohol;
                    Historia.TomaAlcoholFrecuencia = HistoriaClinica.TomaAlcoholFrecuencia;
                    Historia.CantidadAlcohol = HistoriaClinica.CantidadAlcohol;
                    Historia.Fuma = HistoriaClinica.Fuma;
                    Historia.EdadInicio = HistoriaClinica.EdadInicio;
                    HistoriaClinica.ActualmenteFuma = HistoriaClinica.ActualmenteFuma;
                    Historia.TiempoInactividadFuma = HistoriaClinica.TiempoInactividadFuma;
                    Historia.TipoFuma = HistoriaClinica.TipoFuma;
                    Historia.FrecuenciaFuma = HistoriaClinica.FrecuenciaFuma;
                    Historia.CantidadFuma = HistoriaClinica.CantidadFuma;
                    Historia.ConsumeDroga = HistoriaClinica.ConsumeDroga;
                    Historia.TipoDrogaConsume = HistoriaClinica.TipoDrogaConsume;
                    Historia.FrecuenciaDroga = HistoriaClinica.FrecuenciaDroga;
                    Historia.CantidadDrogaConsume = HistoriaClinica.CantidadDrogaConsume;
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_historial_consumo.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class _interrogatorio
        {
            public string Check_SintomasG_Si { get; set; }
            public string Check_SintomasG_No { get; set; }
            public string Describir_SintomasG { get; set; }
            public string Check_Respiratorio_Si { get; set; }
            public string Check_Respiratorio_No { get; set; }
            public string Describir_Respiratorio { get; set; }
            public string Check_Neurologico_Si { get; set; }
            public string Check_Neurologico_No { get; set; }
            public string Describir_Neurologico { get; set; }
            public string Check_pielYanexos_Si { get; set; }
            public string Check_pielYanexos_No { get; set; }
            public string Describir_pielYanexos { get; set; }
            public string Check_Hematologico_Si { get; set; }
            public string Check_Hematologico_No { get; set; }
            public string Describir_Hematologico { get; set; }
            public string Check_Digestivo_Si { get; set; }
            public string Check_Digestivo_No { get; set; }
            public string Describir_Digestivo { get; set; }
            public string Check_Genitourinario_Si { get; set; }
            public string Check_Genitourinario_No { get; set; }
            public string Describir_Genitourinario { get; set; }
            public string Check_Cardiovascular_Si { get; set; }
            public string Check_Cardiovascular_No { get; set; }
            public string Describir_Cardiovascular { get; set; }
            public string Check_Musculoesqueletico_Si { get; set; }
            public string Check_Musculoesqueletico_No { get; set; }
            public string Describir_Musculoesqueletico { get; set; }
            public string Check_Renal_Si { get; set; }
            public string Check_Renal_No { get; set; }
            public string Describir_Renal { get; set; }
            public string Check_Inmune_Si { get; set; }
            public string Check_Inmune_No { get; set; }
            public string Describir_Inmune { get; set; }
        }

        [HttpPost]
        public ActionResult interrogatorio(_interrogatorio HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_interrogatorio Historia = new hc_interrogatorio();
                    #region CHECKS
                    if (HistoriaClinica.Check_SintomasG_Si == "on")
                    {
                        Historia.Check_SintomasG_Si = true;
                    }
                    else
                    {
                        Historia.Check_SintomasG_Si = false;
                    }
                    if (HistoriaClinica.Check_SintomasG_No == "on")
                    {
                        Historia.Check_SintomasG_No = true;
                    }
                    else
                    {
                        Historia.Check_SintomasG_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Respiratorio_Si == "on")
                    {
                        Historia.Check_Respiratorio_Si = true;
                    }
                    else
                    {
                        Historia.Check_Respiratorio_Si = false;
                    }
                    if (HistoriaClinica.Check_Respiratorio_No == "on")
                    {
                        Historia.Check_Respiratorio_No = true;
                    }
                    else
                    {
                        Historia.Check_Respiratorio_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Neurologico_Si == "on")
                    {
                        Historia.Check_Neurologico_Si = true;
                    }
                    else
                    {
                        Historia.Check_Neurologico_Si = false;
                    }
                    if (HistoriaClinica.Check_Neurologico_No == "on")
                    {
                        Historia.Check_Neurologico_No = true;
                    }
                    else
                    {
                        Historia.Check_Neurologico_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_pielYanexos_Si == "on")
                    {
                        Historia.Check_pielYanexos_Si = true;
                    }
                    else
                    {
                        Historia.Check_pielYanexos_Si = false;
                    }
                    if (HistoriaClinica.Check_pielYanexos_No == "on")
                    {
                        Historia.Check_pielYanexos_No = true;
                    }
                    else
                    {
                        Historia.Check_pielYanexos_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Hematologico_Si == "on")
                    {
                        Historia.Check_Hematologico_Si = true;
                    }
                    else
                    {
                        Historia.Check_Hematologico_Si = false;
                    }
                    if (HistoriaClinica.Check_Hematologico_No == "on")
                    {
                        Historia.Check_Hematologico_No = true;
                    }
                    else
                    {
                        Historia.Check_Hematologico_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Digestivo_Si == "on")
                    {
                        Historia.Check_Digestivo_Si = true;
                    }
                    else
                    {
                        Historia.Check_Digestivo_Si = false;
                    }
                    if (HistoriaClinica.Check_Digestivo_No == "on")
                    {
                        Historia.Check_Digestivo_No = true;
                    }
                    else
                    {
                        Historia.Check_Digestivo_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Genitourinario_Si == "on")
                    {
                        Historia.Check_Genitourinario_Si = true;
                    }
                    else
                    {
                        Historia.Check_Genitourinario_Si = false;
                    }
                    if (HistoriaClinica.Check_Genitourinario_No == "on")
                    {
                        Historia.Check_Genitourinario_No = true;
                    }
                    else
                    {
                        Historia.Check_Genitourinario_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Cardiovascular_Si == "on")
                    {
                        Historia.Check_Cardiovascular_Si = true;
                    }
                    else
                    {
                        Historia.Check_Cardiovascular_Si = false;
                    }
                    if (HistoriaClinica.Check_Cardiovascular_No == "on")
                    {
                        Historia.Check_Cardiovascular_No = true;
                    }
                    else
                    {
                        Historia.Check_Cardiovascular_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Musculoesqueletico_Si == "on")
                    {
                        Historia.Check_Musculoesqueletico_Si = true;
                    }
                    else
                    {
                        Historia.Check_Musculoesqueletico_Si = false;
                    }
                    if (HistoriaClinica.Check_Musculoesqueletico_No == "on")
                    {
                        Historia.Check_Musculoesqueletico_No = true;
                    }
                    else
                    {
                        Historia.Check_Musculoesqueletico_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Renal_Si == "on")
                    {
                        Historia.Check_Renal_Si = true;
                    }
                    else
                    {
                        Historia.Check_Renal_Si = false;
                    }
                    if (HistoriaClinica.Check_Renal_No == "on")
                    {
                        Historia.Check_Renal_No = true;
                    }
                    else
                    {
                        Historia.Check_Renal_No = false;
                    }
                    //---
                    if (HistoriaClinica.Check_Inmune_Si == "on")
                    {
                        Historia.Check_Inmune_Si = true;
                    }
                    else
                    {
                        Historia.Check_Inmune_Si = false;
                    }
                    if (HistoriaClinica.Check_Inmune_No == "on")
                    {
                        Historia.Check_Inmune_No = true;
                    }
                    else
                    {
                        Historia.Check_Inmune_No = false;
                    }
                    #endregion
                    Historia.Describir_SintomasG = HistoriaClinica.Describir_SintomasG;
                    Historia.Describir_Respiratorio = HistoriaClinica.Describir_Respiratorio;
                    Historia.Describir_Neurologico = HistoriaClinica.Describir_Neurologico;
                    Historia.Describir_pielYanexos = HistoriaClinica.Describir_pielYanexos;
                    Historia.Describir_Hematologico = HistoriaClinica.Describir_Hematologico;
                    Historia.Describir_Digestivo = HistoriaClinica.Describir_Digestivo;
                    Historia.Describir_Genitourinario = HistoriaClinica.Describir_Genitourinario;
                    Historia.Describir_Cardiovascular = HistoriaClinica.Describir_Cardiovascular;
                    Historia.Describir_Musculoesqueletico = HistoriaClinica.Describir_Musculoesqueletico;
                    Historia.Describir_Renal = HistoriaClinica.Describir_Renal;
                    Historia.Describir_Inmune = HistoriaClinica.Describir_Inmune;
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_interrogatorio.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SignosVitales(SignosVitales signosVitales, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var ip_realiza = Request.UserHostAddress;
                var id_hc = 0;

                //Buscamos al px del que se le quiere hacer la Nota de Evolucion.
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
                    var userName = User.Identity.GetUserName();

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales."); //r.TipoHistoria == "Común" || r.TipoHistoria == "Datos Grales."
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_SignosVitales signos = new hc_SignosVitales();
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
                    signos.Id_Paciente = paciente.Id;
                    signos.Clave_hc_px = Id_claveHC;
                    db.hc_SignosVitales.Add(signos);
                    db.SaveChanges();

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
                TempData["message_err"] = "Error, vuelve a intentar";
                return Redirect(Request.UrlReferrer.ToString());
            }
            ;
        }

        //Consultar los signos vitales que le tomaron al recién llegar al cus (estos no son de la historia)
        public JsonResult ConsultarSignosVitales(string expediente)
        {
            var ultimoRegistro = (from a in db.SignosVitales
                                  where a.expediente == expediente
                                  select a).
                              OrderByDescending(r => r.fecha)
                              .FirstOrDefault();

            if (ultimoRegistro != null)
            {
                return new JsonResult { Data = ultimoRegistro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            else
            {
                return new JsonResult { Data = "", JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        [HttpPost]
        public ActionResult riesgoCaidas(hc_riesgo_caidas HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_riesgo_caidas Historia = new hc_riesgo_caidas();
                    Historia.PuntuajeEscala = HistoriaClinica.PuntuajeEscala;
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_riesgo_caidas.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class _exploracion
        {
            public string Check_Craneo_Si { get; set; }
            public string Check_Craneo_No { get; set; }
            public string Check_Craneo_Diferido { get; set; }
            public string Describir_Hallazgos_Craneo { get; set; }
            public string Check_Cara_Si { get; set; }
            public string Check_Cara_No { get; set; }
            public string Check_Cara_Diferido { get; set; }
            public string Describir_Hallazgos_Cara { get; set; }
            public string Check_Ojos_Si { get; set; }
            public string Check_Ojos_No { get; set; }
            public string Check_Ojos_Diferido { get; set; }
            public string Describir_Hallazgos_Ojos { get; set; }
            public string Check_Nariz_Si { get; set; }
            public string Check_Nariz_No { get; set; }
            public string Check_Nariz_Diferido { get; set; }
            public string Describir_Hallazgos_Nariz { get; set; }
            public string Check_Oidos_Si { get; set; }
            public string Check_Oidos_No { get; set; }
            public string Check_Oidos_Diferido { get; set; }
            public string Describir_Hallazgos_Oidos { get; set; }
            public string Check_Boca_Si { get; set; }
            public string Check_Boca_No { get; set; }
            public string Check_Boca_Diferido { get; set; }
            public string Describir_Hallazgos_Boca { get; set; }
            public string Check_Faringe_Si { get; set; }
            public string Check_Faringe_No { get; set; }
            public string Check_Faringe_Diferido { get; set; }
            public string Describir_Hallazgos_Faringe { get; set; }
            public string Check_Cuello_Si { get; set; }
            public string Check_Cuello_No { get; set; }
            public string Check_Cuello_Diferido { get; set; }
            public string Describir_Hallazgos_Cuello { get; set; }
            public string Check_Torax_Si { get; set; }
            public string Check_Torax_No { get; set; }
            public string Check_Torax_Diferido { get; set; }
            public string Describir_Hallazgos_Torax { get; set; }
            public string Check_AparatoR_Si { get; set; }
            public string Check_AparatoR_No { get; set; }
            public string Check_AparatoR_Diferido { get; set; }
            public string Describir_Hallazgos_AparatoR { get; set; }
            public string Check_AparatoC_Si { get; set; }
            public string Check_AparatoC_No { get; set; }
            public string Check_AparatoC_Diferido { get; set; }
            public string Describir_Hallazgos_AparatoC { get; set; }
            public string Check_Abdomen_Si { get; set; }
            public string Check_Abdomen_No { get; set; }
            public string Check_Abdomen_Diferido { get; set; }
            public string Describir_Hallazgos_Abdomen { get; set; }
            public string Check_Genitales_Si { get; set; }
            public string Check_Genitales_No { get; set; }
            public string Check_Genitales_Diferido { get; set; }
            public string Describir_Hallazgos_Genitales { get; set; }
            public string Check_Extremidades_Si { get; set; }
            public string Check_Extremidades_No { get; set; }
            public string Check_Extremidades_Diferido { get; set; }
            public string Describir_Hallazgos_Extremidades { get; set; }
            public string Check_SistemaN_Si { get; set; }
            public string Check_SistemaN_No { get; set; }
            public string Check_SistemaN_Diferido { get; set; }
            public string Describir_Hallazgos_SistemaN { get; set; }
            public string Check_pielanexos_Si { get; set; }
            public string Check_pielanexos_No { get; set; }
            public string Check_pielYanexos_Diferido { get; set; }
            public string Describir_Hallazgos_pielYanexos { get; set; }
        }

        [HttpPost]
        public ActionResult exploracionFisica(_exploracion HistoriaClinica, string expediente)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var fechaDT = DateTime.Parse(fecha);
                var id_hc = 0;

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

                    //si NO existe registro en la bd en la tbl HistoriaClinica por default pacienteTieneRegistroEnUltimas3Horas será null, quiere decir que se creará un registro nuevo
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
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Datos Grales.");
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
                        string claveHC = buscaHisotriaClinica(id_hc, expediente);
                        Id_claveHC = claveHC;
                    }

                    //Se crea la HC de esta sección/pestaña
                    hc_exploracion_fisica Historia = new hc_exploracion_fisica();
                    
                    if (HistoriaClinica.Check_Craneo_Si == "on")
                    {
                        Historia.Check_Craneo_Si = true;
                    }
                    else
                    {
                        Historia.Check_Craneo_Si = false;
                    }
                    if (HistoriaClinica.Check_Craneo_No == "on")
                    {
                        Historia.Check_Craneo_No = true;
                    }
                    else
                    {
                        Historia.Check_Craneo_No = false;
                    }
                    if (HistoriaClinica.Check_Craneo_Diferido == "on")
                    {
                        Historia.Check_Craneo_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Craneo_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Cara_Si == "on")
                    {
                        Historia.Check_Cara_Si = true;
                    }
                    else
                    {
                        Historia.Check_Cara_Si = false;
                    }
                    if (HistoriaClinica.Check_Cara_No == "on")
                    {
                        Historia.Check_Cara_No = true;
                    }
                    else
                    {
                        Historia.Check_Cara_No = false;
                    }
                    if (HistoriaClinica.Check_Cara_Diferido == "on")
                    {
                        Historia.Check_Cara_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Cara_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Ojos_Si == "on")
                    {
                        Historia.Check_Ojos_Si = true;
                    }
                    else
                    {
                        Historia.Check_Ojos_Si = false;
                    }
                    if (HistoriaClinica.Check_Ojos_No == "on")
                    {
                        Historia.Check_Ojos_No = true;
                    }
                    else
                    {
                        Historia.Check_Ojos_No = false;
                    }
                    if (HistoriaClinica.Check_Ojos_Diferido == "on")
                    {
                        Historia.Check_Ojos_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Ojos_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Nariz_Si == "on")
                    {
                        Historia.Check_Nariz_Si = true;
                    }
                    else
                    {
                        Historia.Check_Nariz_Si = false;
                    }
                    if (HistoriaClinica.Check_Nariz_No == "on")
                    {
                        Historia.Check_Nariz_No = true;
                    }
                    else
                    {
                        Historia.Check_Nariz_No = false;
                    }
                    if (HistoriaClinica.Check_Nariz_Diferido == "on")
                    {
                        Historia.Check_Nariz_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Nariz_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Oidos_Si == "on")
                    {
                        Historia.Check_Oidos_Si = true;
                    }
                    else
                    {
                        Historia.Check_Oidos_Si = false;
                    }
                    if (HistoriaClinica.Check_Oidos_No == "on")
                    {
                        Historia.Check_Oidos_No = true;
                    }
                    else
                    {
                        Historia.Check_Oidos_No = false;
                    }
                    if (HistoriaClinica.Check_Oidos_Diferido == "on")
                    {
                        Historia.Check_Oidos_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Oidos_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Boca_Si == "on")
                    {
                        Historia.Check_Boca_Si = true;
                    }
                    else
                    {
                        Historia.Check_Boca_Si = false;
                    }
                    if (HistoriaClinica.Check_Boca_No == "on")
                    {
                        Historia.Check_Boca_No = true;
                    }
                    else
                    {
                        Historia.Check_Boca_No = false;
                    }
                    if (HistoriaClinica.Check_Boca_Diferido == "on")
                    {
                        Historia.Check_Boca_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Boca_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Faringe_Si == "on")
                    {
                        Historia.Check_Faringe_Si = true;
                    }
                    else
                    {
                        Historia.Check_Faringe_Si = false;
                    }
                    if (HistoriaClinica.Check_Faringe_No == "on")
                    {
                        Historia.Check_Faringe_No = true;
                    }
                    else
                    {
                        Historia.Check_Faringe_No = false;
                    }
                    if (HistoriaClinica.Check_Faringe_Diferido == "on")
                    {
                        Historia.Check_Faringe_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Faringe_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Cuello_Si == "on")
                    {
                        Historia.Check_Cuello_Si = true;
                    }
                    else
                    {
                        Historia.Check_Cuello_Si = false;
                    }
                    if (HistoriaClinica.Check_Cuello_No == "on")
                    {
                        Historia.Check_Cuello_No = true;
                    }
                    else
                    {
                        Historia.Check_Cuello_No = false;
                    }
                    if (HistoriaClinica.Check_Cuello_Diferido == "on")
                    {
                        Historia.Check_Cuello_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Cuello_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Torax_Si == "on")
                    {
                        Historia.Check_Torax_Si = true;
                    }
                    else
                    {
                        Historia.Check_Torax_Si = false;
                    }
                    if (HistoriaClinica.Check_Torax_No == "on")
                    {
                        Historia.Check_Torax_No = true;
                    }
                    else
                    {
                        Historia.Check_Torax_No = false;
                    }
                    if (HistoriaClinica.Check_Torax_Diferido == "on")
                    {
                        Historia.Check_Torax_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Torax_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_AparatoR_Si == "on")
                    {
                        Historia.Check_AparatoR_Si = true;
                    }
                    else
                    {
                        Historia.Check_AparatoR_Si = false;
                    }
                    if (HistoriaClinica.Check_AparatoR_No == "on")
                    {
                        Historia.Check_AparatoR_No = true;
                    }
                    else
                    {
                        Historia.Check_AparatoR_No = false;
                    }
                    if (HistoriaClinica.Check_AparatoR_Diferido == "on")
                    {
                        Historia.Check_AparatoR_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_AparatoR_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_AparatoC_Si == "on")
                    {
                        Historia.Check_AparatoC_Si = true;
                    }
                    else
                    {
                        Historia.Check_AparatoC_Si = false;
                    }
                    if (HistoriaClinica.Check_AparatoC_No == "on")
                    {
                        Historia.Check_AparatoC_No = true;
                    }
                    else
                    {
                        Historia.Check_AparatoC_No = false;
                    }
                    if (HistoriaClinica.Check_AparatoC_Diferido == "on")
                    {
                        Historia.Check_AparatoC_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_AparatoC_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Abdomen_Si == "on")
                    {
                        Historia.Check_Abdomen_Si = true;
                    }
                    else
                    {
                        Historia.Check_Abdomen_Si = false;
                    }
                    if (HistoriaClinica.Check_Abdomen_No == "on")
                    {
                        Historia.Check_Abdomen_No = true;
                    }
                    else
                    {
                        Historia.Check_Abdomen_No = false;
                    }
                    if (HistoriaClinica.Check_Abdomen_Diferido == "on")
                    {
                        Historia.Check_Abdomen_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Abdomen_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Genitales_Si == "on")
                    {
                        Historia.Check_Genitales_Si = true;
                    }
                    else
                    {
                        Historia.Check_Genitales_Si = false;
                    }
                    if (HistoriaClinica.Check_Genitales_No == "on")
                    {
                        Historia.Check_Genitales_No = true;
                    }
                    else
                    {
                        Historia.Check_Genitales_No = false;
                    }
                    if (HistoriaClinica.Check_Genitales_Diferido == "on")
                    {
                        Historia.Check_Genitales_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Genitales_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_Extremidades_Si == "on")
                    {
                        Historia.Check_Extremidades_Si = true;
                    }
                    else
                    {
                        Historia.Check_Extremidades_Si = false;
                    }
                    if (HistoriaClinica.Check_Extremidades_No == "on")
                    {
                        Historia.Check_Extremidades_No = true;
                    }
                    else
                    {
                        Historia.Check_Extremidades_No = false;
                    }
                    if (HistoriaClinica.Check_Extremidades_Diferido == "on")
                    {
                        Historia.Check_Extremidades_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_Extremidades_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_SistemaN_Si == "on")
                    {
                        Historia.Check_SistemaN_Si = true;
                    }
                    else
                    {
                        Historia.Check_SistemaN_Si = false;
                    }
                    if (HistoriaClinica.Check_SistemaN_No == "on")
                    {
                        Historia.Check_SistemaN_No = true;
                    }
                    else
                    {
                        Historia.Check_SistemaN_No = false;
                    }
                    if (HistoriaClinica.Check_SistemaN_Diferido == "on")
                    {
                        Historia.Check_SistemaN_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_SistemaN_Diferido = false;
                    }
                    //-
                    if (HistoriaClinica.Check_pielanexos_Si == "on")
                    {
                        Historia.Check_pielanexos_Si = true;
                    }
                    else
                    {
                        Historia.Check_pielanexos_Si = false;
                    }
                    if (HistoriaClinica.Check_pielanexos_No == "on")
                    {
                        Historia.Check_pielanexos_No = true;
                    }
                    else
                    {
                        Historia.Check_pielanexos_No = false;
                    }
                    if (HistoriaClinica.Check_pielYanexos_Diferido == "on")
                    {
                        Historia.Check_pielYanexos_Diferido = true;
                    }
                    else
                    {
                        Historia.Check_pielYanexos_Diferido = false;
                    }

                    Historia.Describir_Hallazgos_Craneo = HistoriaClinica.Describir_Hallazgos_Craneo;
                    Historia.Describir_Hallazgos_Cara = HistoriaClinica.Describir_Hallazgos_Cara;
                    Historia.Describir_Hallazgos_Ojos = HistoriaClinica.Describir_Hallazgos_Ojos;
                    Historia.Describir_Hallazgos_Nariz = HistoriaClinica.Describir_Hallazgos_Nariz;
                    Historia.Describir_Hallazgos_Oidos = HistoriaClinica.Describir_Hallazgos_Oidos;
                    Historia.Describir_Hallazgos_Boca = HistoriaClinica.Describir_Hallazgos_Boca;
                    Historia.Describir_Hallazgos_Faringe = HistoriaClinica.Describir_Hallazgos_Faringe;
                    Historia.Describir_Hallazgos_Cuello = HistoriaClinica.Describir_Hallazgos_Cuello;
                    Historia.Describir_Hallazgos_Torax = HistoriaClinica.Describir_Hallazgos_Torax;
                    Historia.Describir_Hallazgos_AparatoR = HistoriaClinica.Describir_Hallazgos_AparatoR;
                    Historia.Describir_Hallazgos_AparatoC = HistoriaClinica.Describir_Hallazgos_AparatoC;
                    Historia.Describir_Hallazgos_Abdomen = HistoriaClinica.Describir_Hallazgos_Abdomen;
                    Historia.Describir_Hallazgos_Genitales = HistoriaClinica.Describir_Hallazgos_Genitales;
                    Historia.Describir_Hallazgos_Extremidades = HistoriaClinica.Describir_Hallazgos_Extremidades;
                    Historia.Describir_Hallazgos_SistemaN = HistoriaClinica.Describir_Hallazgos_SistemaN;
                    Historia.Describir_Hallazgos_pielYanexos = HistoriaClinica.Describir_Hallazgos_pielYanexos;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    db.hc_exploracion_fisica.Add(Historia);
                    db.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        public ActionResult Historia(string expediente)
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

        public class Propiedades
        {
            public int Id { get; set; }
            public int UnidadAfiliacion { get; set; }
            public string CURP { get; set; }
            public string Nombre { get; set; }
            public string PrimerApellido { get; set; }
            public string SegundoApellido { get; set; }
            public string Sexo { get; set; }
            public DateTime FechaNacimiento { get; set; }
            public int Edad { get; set; }
            public string Expediente { get; set; }
            public string Curp_Calculado { get; set; }
            public DateTime FechaRegistro { get; set; }
            public string Clave { get; set; }
            public string Medico { get; set; }
            public int IdPX { get; set; }
            public string FechaReg { get; set; }
            public string Tipo { get; set; }
            public string Ident_HCcomun { get; set; }
        }

        //********      Lista de las H.C. en base a un px que el usuario ingresó
        [HttpPost]
        public JsonResult BuscarHistoria(string ExpedientePX)
        {
            try
            {
                //Buscar PX por el Expediente que enviamos desde la vista
                var PX = (from a in db.Paciente
                          where a.Expediente == ExpedientePX
                          select a).FirstOrDefault();

                var results1 = new List<Propiedades>();

                if (PX != null)
                {
                    //Mostrar todas las HC que tenga ese px
                    var consulta = (from hc in db.HistoriaClinica
                                    join px in db.Paciente on hc.Id_Paciente equals px.Id
                                    where hc.Id_Paciente == PX.Id
                                    where hc.TipoHistoria != "Común" && hc.TipoHistoria != "Datos Grales."
                                    select new
                                    {
                                        IdHC = hc.Id,
                                        ClaveHC = hc.Clave_hc_px,
                                        MedicoHC = hc.Medico,
                                        FechaRegHC = hc.FechaRegistroHC,
                                        PxHC = hc.Id_Paciente,
                                        NombrePX = px.Nombre,
                                        PApellidoPX = px.PrimerApellido,
                                        ExpPX = px.Expediente,
                                        TipoH = hc.TipoHistoria,
                                        Ident = hc.Ident_HCcomun
                                    }).ToList().OrderByDescending(n=>n.IdHC);

                    foreach (var q in consulta)
                    {
                        var resultado = new Propiedades
                        {
                            Id = q.IdHC,
                            Clave = q.ClaveHC, //Este es el Identificador armado que se inserta en cada tbl de la H.C.
                            Medico = q.MedicoHC,
                            FechaReg = string.Format("{0:dd/MM/yyyy hh:mm tt}", q.FechaRegHC),
                            IdPX = q.PxHC,
                            Nombre = q.NombrePX,
                            PrimerApellido = q.PApellidoPX,
                            Expediente = q.ExpPX,
                            Tipo = q.TipoH,
                            Ident_HCcomun = q.Ident
                        };
                        results1.Add(resultado);
                    }
                }
                return Json(new { MENSAJE = "Succe: ", HISTORIAS = results1 }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class Propiedades_HC
        {
            //hc_ficha_identificacion
            public string acompanante { get; set; }
            public string NombreAcompa { get; set; }
            public string alergia { get; set; }
            public string NombreAlergia { get; set; }
            public string MotivoCons { get; set; }
            //hc_evaluacion_social
            public string estadocivil { get; set; }
            public string numeropersonas { get; set; }
            public int? numerohabitaciones { get; set; }
            public string hacinamiento { get; set; }
            public string escolaridad { get; set; }
            public string GradoEsc { get; set; }
            public string PromedioEsc { get; set; }
            public string reprobado { get; set; }
            public string EspecifiqueReprobado { get; set; }
            public string Desercion { get; set; }
            public string Guarderia { get; set; }
            public string Cuidador { get; set; }
            public string Problemas { get; set; }
            public string ProblemasMaestros { get; set; }
            public string Ocupacio { get; set; }
            public string TipoTrab { get; set; }
            public string RiesgoSos { get; set; }
            public string ServicioSal { get; set; }
            //hc_valoracion_familiar
            public string mamaPaciente { get; set; }
            public string EdadMama { get; set; }
            public string GradoMama { get; set; }
            public string OcupacionMama { get; set; }
            public string TipoTrabajoMama { get; set; }
            public string papaPaciente { get; set; }
            public string EdadPapa { get; set; }
            public string GradoPapa { get; set; }
            public string OcupacionPapa { get; set; }
            public string TipoTrabajoPapa { get; set; }
            public string ViveCon { get; set; }
            public string tieneHermanos { get; set; }
            public int? CuantosHermanos { get; set; }
            public string EspecifiqueEnf { get; set; }
            public string involucraTratamiento { get; set; }
            public string EspecifiqueInvol { get; set; }
            public string riesgoSocial { get; set; }
            //hc_evaliacion_economica
            public string beneficiarioPrograma { get; set; }
            public string EspecifiqueBeneficiario { get; set; }
            public string rolEconimico { get; set; }
            public string EspecifiqueRol { get; set; }
            public string riesgoEconomico { get; set; }
            //hc_valores_creencias
            public string perteneceReligion { get; set; }
            public string religion { get; set; }
            public string EspecifiqueReligion { get; set; }
            public string creenciaReligiosa { get; set; }
            public string EspecifiqueCreencia { get; set; }
            public string costumbreValores { get; set; }
            public string EspecifiqueCostumbres { get; set; }
            //hc_factores_riesgo_psicologicos
            public string CambiosSueño { get; set; }
            public string CambiosEnergia { get; set; }
            public string CambiosApetito { get; set; }
            public string Pesimismo { get; set; }
            public string Irritabilidad { get; set; }
            public string PerdidaPlacer { get; set; }
            public string PalpitacionesFuertes { get; set; }
            public string SensacionAahogo { get; set; }
            public string MiedoPreocupacion { get; set; }
            public string IdeacionSuicida { get; set; }
            //hc_habitos
            public bool? Check_Adicciones_Padre { get; set; }
            public bool? Check_Adicciones_Madre { get; set; }
            public bool? Check_Adicciones_Ambas { get; set; }
            public bool? Check_Adicciones_NA { get; set; }
            public bool? Check_Cardiopatia_Padre { get; set; }
            public bool? Check_Cardiopatia_Madre { get; set; }
            public bool? Check_Cardiopatia_Ambas { get; set; }
            public bool? Check_Cardiopatia_NA { get; set; }
            public bool? Check_Diabetes_Padre { get; set; }
            public bool? Check_Diabetes_Madre { get; set; }
            public bool? Check_Diabetes_Ambas { get; set; }
            public bool? Check_Diabetes_NA { get; set; }
            public bool? Check_Dislipidemias_Padre { get; set; }
            public bool? Check_Dislipidemias_Madre { get; set; }
            public bool? Check_Dislipidemias_Ambas { get; set; }
            public bool? Check_Dislipidemias_NA { get; set; }
            public bool? Check_Epilepsia_Padre { get; set; }
            public bool? Check_Epilepsia_Madre { get; set; }
            public bool? Check_Epilepsia_Ambas { get; set; }
            public bool? Check_Epilepsia_NA { get; set; }
            public bool? Check_Hipertension_Padre { get; set; }
            public bool? Check_Hipertension_Madre { get; set; }
            public bool? Check_Hipertension_Ambas { get; set; }
            public bool? Check_Hipertension_NA { get; set; }
            public bool? Check_Infectocontagiosas_Padre { get; set; }
            public bool? Check_Infectocontagiosas_Madre { get; set; }
            public bool? Check_Infectocontagiosas_Ambas { get; set; }
            public bool? Check_Infectocontagiosas_NA { get; set; }
            public bool? Check_Malformaciones_Padre { get; set; }
            public bool? Check_Malformaciones_Madre { get; set; }
            public bool? Check_Malformaciones_Ambas { get; set; }
            public bool? Check_Malformaciones_NA { get; set; }
            public bool? Check_Nefropatias_Padre { get; set; }
            public bool? Check_Nefropatias_Madre { get; set; }
            public bool? Check_Nefropatias_Ambas { get; set; }
            public bool? Check_Nefropatias_NA { get; set; }
            public bool? Check_Obesidad_Padre { get; set; }
            public bool? Check_Obesidad_Madre { get; set; }
            public bool? Check_Obesidad_Ambas { get; set; }
            public bool? Check_Obesidad_NA { get; set; }
            public bool? Check_Oncologicos_Padre { get; set; }
            public bool? Check_Oncologicos_Madre { get; set; }
            public bool? Check_Oncologicos_Ambas { get; set; }
            public bool? Check_Oncologicos_NA { get; set; }
            //hc_antecedentes_patologicos
            public string Antecedentes { get; set; }
            public string PxHospitalizado { get; set; }
            public string EspecifiqueHospitalizado { get; set; }
            public string RealizadoCirugia { get; set; }
            public string EspecifiqueCirugia { get; set; }
            public bool? CheckRespiratorios { get; set; }
            public bool? CheckEndocrinologicos { get; set; }
            public bool? CheckCardiovasculares { get; set; }
            public bool? CheckOncologicos { get; set; }
            public bool? CheckSaludMental { get; set; }
            public bool? CheckNeurologicos { get; set; }
            public bool? CheckInfectoContagiosos { get; set; }
            public bool? CheckProblemasAparatoR { get; set; }
            public bool? CheckProblemasGastro { get; set; }
            public bool? CheckReumatologicos { get; set; }
            public bool? CheckNinguna { get; set; }
            public string DetallarRespiratorios { get; set; }
            public string DetallarEndocrinologicos { get; set; }
            public string DetallarCardiovasculares { get; set; }
            public string DetallarOncologicos { get; set; }
            public string DetallarSaludMental { get; set; }
            public string DetallarNeurologicos { get; set; }
            public string DetallarInfectoContagiosos { get; set; }
            public string DetallarProblemasAparatoR { get; set; }
            public string DetallarProblemasGastro { get; set; }
            public string DetallarReumatologicos { get; set; }
            //hc_historial_consumo
            public string TomaAlcohol { get; set; }
            public string TomaAlcoholFrecuencia { get; set; }
            public string CantidadAlcohol { get; set; }
            public string Fuma { get; set; }
            public string EdadInicio { get; set; }
            public string ActualmenteFuma { get; set; }
            public string TiempoInactividadFuma { get; set; }
            public string TipoFuma { get; set; }
            public string FrecuenciaFuma { get; set; }
            public string CantidadFuma { get; set; }
            public string ConsumeDroga { get; set; }
            public string TipoDrogaConsume { get; set; }
            public string FrecuenciaDroga { get; set; }
            public string CantidadDrogaConsume { get; set; }
            //hc_interrogatorio
            public bool? Check_SintomasG_Si { get; set; }
            public bool? Check_SintomasG_No { get; set; }
            public string Describir_SintomasG { get; set; }
            public bool? Check_Respiratorio_Si { get; set; }
            public bool? Check_Respiratorio_No { get; set; }
            public string Describir_Respiratorio { get; set; }
            public bool? Check_Neurologico_Si { get; set; }
            public bool? Check_Neurologico_No { get; set; }
            public string Describir_Neurologico { get; set; }
            public bool? Check_pielYanexos_Si { get; set; }
            public bool? Check_pielYanexos_No { get; set; }
            public string Describir_pielYanexos { get; set; }
            public bool? Check_Hematologico_Si { get; set; }
            public bool? Check_Hematologico_No { get; set; }
            public string Describir_Hematologico { get; set; }
            public bool? Check_Digestivo_Si { get; set; }
            public bool? Check_Digestivo_No { get; set; }
            public string Describir_Digestivo { get; set; }
            public bool? Check_Genitourinario_Si { get; set; }
            public bool? Check_Genitourinario_No { get; set; }
            public string Describir_Genitourinario { get; set; }
            public bool? Check_Cardiovascular_Si { get; set; }
            public bool? Check_Cardiovascular_No { get; set; }
            public string Describir_Cardiovascular { get; set; }
            public bool? Check_Musculoesqueletico_Si { get; set; }
            public bool? Check_Musculoesqueletico_No { get; set; }
            public string Describir_Musculoesqueletico { get; set; }
            public bool? Check_Renal_Si { get; set; }
            public bool? Check_Renal_No { get; set; }
            public string Describir_Renal { get; set; }
            public bool? Check_Inmune_Si { get; set; }
            public bool? Check_Inmune_No { get; set; }
            public string Describir_Inmune { get; set; }
            //RiesgoCaidas
            public string PuntuajeEscala { get; set; }
            //ExploracionFisica
            public bool? Check_Craneo_Si { get; set; }
            public bool? Check_Craneo_No { get; set; }
            public bool? Check_Craneo_Diferido { get; set; }
            public string Describir_Hallazgos_Craneo { get; set; }
            public bool? Check_Cara_Si { get; set; }
            public bool? Check_Cara_No { get; set; }
            public bool? Check_Cara_Diferido { get; set; }
            public string Describir_Hallazgos_Cara { get; set; }
            public bool? Check_Ojos_Si { get; set; }
            public bool? Check_Ojos_No { get; set; }
            public bool? Check_Ojos_Diferido { get; set; }
            public string Describir_Hallazgos_Ojos { get; set; }
            public bool? Check_Nariz_Si { get; set; }
            public bool? Check_Nariz_No { get; set; }
            public bool? Check_Nariz_Diferido { get; set; }
            public string Describir_Hallazgos_Nariz { get; set; }
            public bool? Check_Oidos_Si { get; set; }
            public bool? Check_Oidos_No { get; set; }
            public bool? Check_Oidos_Diferido { get; set; }
            public string Describir_Hallazgos_Oidos { get; set; }
            public bool? Check_Boca_Si { get; set; }
            public bool? Check_Boca_No { get; set; }
            public bool? Check_Boca_Diferido { get; set; }
            public string Describir_Hallazgos_Boca { get; set; }
            public bool? Check_Faringe_Si { get; set; }
            public bool? Check_Faringe_No { get; set; }
            public bool? Check_Faringe_Diferido { get; set; }
            public string Describir_Hallazgos_Faringe { get; set; }
            public bool? Check_Cuello_Si { get; set; }
            public bool? Check_Cuello_No { get; set; }
            public bool? Check_Cuello_Diferido { get; set; }
            public string Describir_Hallazgos_Cuello { get; set; }
            public bool? Check_Torax_Si { get; set; }
            public bool? Check_Torax_No { get; set; }
            public bool? Check_Torax_Diferido { get; set; }
            public string Describir_Hallazgos_Torax { get; set; }
            public bool? Check_AparatoR_Si { get; set; }
            public bool? Check_AparatoR_No { get; set; }
            public bool? Check_AparatoR_Diferido { get; set; }
            public string Describir_Hallazgos_AparatoR { get; set; }
            public bool? Check_AparatoC_Si { get; set; }
            public bool? Check_AparatoC_No { get; set; }
            public bool? Check_AparatoC_Diferido { get; set; }
            public string Describir_Hallazgos_AparatoC { get; set; }
            public bool? Check_Abdomen_Si { get; set; }
            public bool? Check_Abdomen_No { get; set; }
            public bool? Check_Abdomen_Diferido { get; set; }
            public string Describir_Hallazgos_Abdomen { get; set; }
            public bool? Check_Genitales_Si { get; set; }
            public bool? Check_Genitales_No { get; set; }
            public bool? Check_Genitales_Diferido { get; set; }
            public string Describir_Hallazgos_Genitales { get; set; }
            public bool? Check_Extremidades_Si { get; set; }
            public bool? Check_Extremidades_No { get; set; }
            public bool? Check_Extremidades_Diferido { get; set; }
            public string Describir_Hallazgos_Extremidades { get; set; }
            public bool? Check_SistemaN_Si { get; set; }
            public bool? Check_SistemaN_No { get; set; }
            public bool? Check_SistemaN_Diferido { get; set; }
            public string Describir_Hallazgos_SistemaN { get; set; }
            public bool? Check_pielanexos_Si { get; set; }
            public bool? Check_pielanexos_No { get; set; }
            public bool? Check_pielYanexos_Diferido { get; set; }
            public string Describir_Hallazgos_pielYanexos { get; set; }
        }

        //********      Función para buscar el detalle de la H.C. en el MODAL
        [HttpPost]
        public ActionResult ConsultarHC(string Clave_hc_px, string TipoHistoria)//Este parametro lo recivimos de la vista, "Clave_hc_px" viene siendo el Identificador armado de la HC que se desea ver
        {
            try
            {
                Propiedades_HC HC = new Propiedades_HC();

                //Hacemos el Select de las primeras 3 tablas de la HC
                //Como "Clave_hc_px" está en cada tabla (en caso que si se haya hecho esa tabla al crear la HC), los Joins se hicieron en base a esa columna en cada tabla, entonces el Where será en base a esa tabla solo de la primera tabla
                //Recordemos que al crear la HC, puede que alguna(s) tablas no se llenen , por eso ágregué Left Join, según yo eso puede funcionar para que muestre Null cuando no encuentre registro en la tabla 
                string query =
                    "SELECT FI.acompanante, FI.NombreAcompa, FI.alergia, FI.NombreAlergia, FI.MotivoCons, " +
                    "VS.estadocivil, VS.numeropersonas, VS.numerohabitaciones, VS.hacinamiento, VS.escolaridad, VS.GradoEsc, VS.PromedioEsc, VS.reprobado, VS.EspecifiqueReprobado, VS.Desercion, VS.Guarderia, VS.Cuidador, VS.Problemas, VS.ProblemasMaestros, VS.Ocupacio, VS.TipoTrab, VS.RiesgoSos, VS.ServicioSal, " +
                    "VF.mamaPaciente, VF.EdadMama, VF.GradoMama, VF.OcupacionMama, VF.TipoTrabajoMama, VF.papaPaciente, VF.EdadPapa, VF.GradoPapa, VF.OcupacionPapa, VF.TipoTrabajoPapa, VF.ViveCon, VF.tieneHermanos, VF.CuantosHermanos, VF.EspecifiqueEnf, VF.involucraTratamiento, VF.EspecifiqueInvol, VF.riesgoSocial, " +
                    "EE.beneficiarioPrograma, EE.EspecifiqueBeneficiario, EE.rolEconimico, EE.EspecifiqueRol, EE.riesgoEconomico, " +
                    "VC.perteneceReligion, VC.religion, VC.EspecifiqueReligion, VC.creenciaReligiosa, VC.EspecifiqueCreencia, VC.costumbreValores, VC.EspecifiqueCostumbres, " +
                    "FRP.CambiosSueño, FRP.CambiosEnergia, FRP.CambiosApetito, FRP.Pesimismo, FRP.Irritabilidad, FRP.PerdidaPlacer, FRP.PalpitacionesFuertes, FRP.SensacionAahogo, FRP.MiedoPreocupacion, FRP.IdeacionSuicida, " +
                    "H.Check_Adicciones_Padre, H.Check_Adicciones_Madre, H.Check_Adicciones_Ambas, H.Check_Adicciones_NA, H.Check_Cardiopatia_Padre, H.Check_Cardiopatia_Madre, H.Check_Cardiopatia_Ambas, H.Check_Cardiopatia_NA, H.Check_Diabetes_Padre, H.Check_Diabetes_Madre, H.Check_Diabetes_Ambas, H.Check_Diabetes_NA, H.Check_Dislipidemias_Padre, H.Check_Dislipidemias_Madre, H.Check_Dislipidemias_Ambas, H.Check_Dislipidemias_NA, H.Check_Epilepsia_Padre, H.Check_Epilepsia_Madre, H.Check_Epilepsia_Ambas, H.Check_Epilepsia_NA, H.Check_Hipertension_Padre, H.Check_Hipertension_Madre, H.Check_Hipertension_Ambas, H.Check_Hipertension_NA, H.Check_Infectocontagiosas_Padre, H.Check_Infectocontagiosas_Madre, H.Check_Infectocontagiosas_Ambas, H.Check_Infectocontagiosas_NA, H.Check_Malformaciones_Padre, H.Check_Malformaciones_Madre, H.Check_Malformaciones_Ambas, H.Check_Malformaciones_NA, H.Check_Nefropatias_Padre, H.Check_Nefropatias_Madre, H.Check_Nefropatias_Ambas, H.Check_Nefropatias_NA, H.Check_Obesidad_Padre, H.Check_Obesidad_Madre, H.Check_Obesidad_Ambas, H.Check_Obesidad_NA, H.Check_Oncologicos_Padre, H.Check_Oncologicos_Madre, H.Check_Oncologicos_Ambas, H.Check_Oncologicos_NA, " +
                    "APA.Antecedentes, APA.PxHospitalizado, APA.EspecifiqueHospitalizado, APA.RealizadoCirugia, APA.EspecifiqueCirugia, APA.CheckRespiratorios, APA.CheckEndocrinologicos, APA.CheckCardiovasculares, APA.CheckOncologicos, APA.CheckSaludMental, APA.CheckNeurologicos, APA.CheckInfectoContagiosos, APA.CheckProblemasAparatoR, APA.CheckProblemasGastro, APA.CheckReumatologicos, APA.CheckNinguna, APA.DetallarRespiratorios, APA.DetallarEndocrinologicos, APA.DetallarCardiovasculares, APA.DetallarOncologicos, APA.DetallarSaludMental, APA.DetallarNeurologicos, APA.DetallarInfectoContagiosos, APA.DetallarProblemasAparatoR, APA.DetallarProblemasGastro, APA.DetallarReumatologicos, " +
                    "HC.TomaAlcohol, HC.TomaAlcoholFrecuencia, HC.CantidadAlcohol, HC.Fuma, HC.EdadInicio, HC.ActualmenteFuma, HC.TiempoInactividadFuma, HC.TipoFuma, HC.FrecuenciaFuma, HC.CantidadFuma, HC.ConsumeDroga, HC.TipoDrogaConsume, HC.FrecuenciaDroga, HC.CantidadDrogaConsume, " +
                    "INT.Check_SintomasG_Si, INT.Check_SintomasG_No, INT.Describir_SintomasG, INT.Check_Respiratorio_Si, INT.Check_Respiratorio_No, INT.Describir_Respiratorio, INT.Check_Neurologico_Si, INT.Check_Neurologico_No, INT.Describir_Neurologico, INT.Check_pielYanexos_Si, INT.Check_pielYanexos_No, INT.Describir_pielYanexos, INT.Check_Hematologico_Si, INT.Check_Hematologico_No, INT.Describir_Hematologico, INT.Check_Digestivo_Si, INT.Check_Digestivo_No, INT.Describir_Digestivo, INT.Check_Genitourinario_Si, INT.Check_Genitourinario_No, INT.Describir_Genitourinario, INT.Check_Cardiovascular_Si, INT.Check_Cardiovascular_No, INT.Describir_Cardiovascular, INT.Check_Musculoesqueletico_Si, INT.Check_Musculoesqueletico_No, INT.Describir_Musculoesqueletico, INT.Check_Renal_Si, INT.Check_Renal_No, INT.Describir_Renal, INT.Check_Inmune_Si, INT.Check_Inmune_No, INT.Describir_Inmune, " +
                    "RC.PuntuajeEscala, " +
                    "EF.Check_Craneo_Si, EF.Check_Craneo_No, EF.Check_Craneo_Diferido, EF.Describir_Hallazgos_Craneo, EF.Check_Cara_Si, EF.Check_Cara_No, EF.Check_Cara_Diferido, EF.Describir_Hallazgos_Cara, EF.Check_Ojos_Si, EF.Check_Ojos_No, EF.Check_Ojos_Diferido, EF.Describir_Hallazgos_Ojos, EF.Check_Nariz_Si, EF.Check_Nariz_No, EF.Check_Nariz_Diferido, EF.Describir_Hallazgos_Nariz, EF.Check_Oidos_Si, EF.Check_Oidos_No, EF.Check_Oidos_Diferido, EF.Describir_Hallazgos_Oidos, EF.Check_Boca_Si, EF.Check_Boca_No, EF.Check_Boca_Diferido, EF.Describir_Hallazgos_Boca, EF.Check_Faringe_Si, EF.Check_Faringe_No, EF.Check_Faringe_Diferido, EF.Describir_Hallazgos_Faringe, EF.Check_Cuello_Si, EF.Check_Cuello_No, EF.Check_Cuello_Diferido, EF.Describir_Hallazgos_Cuello, EF.Check_Torax_Si, EF.Check_Torax_No, EF.Check_Torax_Diferido, EF.Describir_Hallazgos_Torax, EF.Check_AparatoR_Si, EF.Check_AparatoR_No, EF.Check_AparatoR_Diferido, EF.Describir_Hallazgos_AparatoR, EF.Check_AparatoC_Si, EF.Check_AparatoC_No, EF.Check_AparatoC_Diferido, EF.Describir_Hallazgos_AparatoC, EF.Check_Abdomen_Si, EF.Check_Abdomen_No, EF.Check_Abdomen_Diferido, EF.Describir_Hallazgos_Abdomen, EF.Check_Genitales_Si, EF.Check_Genitales_No, EF.Check_Genitales_Diferido, EF.Describir_Hallazgos_Genitales, EF.Check_Extremidades_Si, EF.Check_Extremidades_No, EF.Check_Extremidades_Diferido, EF.Describir_Hallazgos_Extremidades, EF.Check_SistemaN_Si, EF.Check_SistemaN_No, EF.Check_SistemaN_Diferido, EF.Describir_Hallazgos_SistemaN, EF.Check_pielanexos_Si, EF.Check_pielanexos_No, EF.Check_pielYanexos_Diferido, EF.Describir_Hallazgos_pielYanexos " +
                                    "FROM HistoriaClinica HCli " +
                                    "LEFT JOIN hc_ficha_identificacion FI ON FI.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_evaluacion_social VS ON HCli.Clave_hc_px = VS.Clave_hc_px " +
                                    "LEFT JOIN hc_valoracion_familiar VF ON HCli.Clave_hc_px = VF.Clave_hc_px " +
                                    "LEFT JOIN hc_evaluacion_economica EE ON HCli.Clave_hc_px = EE.Clave_hc_px " +
                                    "LEFT JOIN hc_valores_creencias VC ON HCli.Clave_hc_px = VC.Clave_hc_px " +
                                    "LEFT JOIN hc_factores_riesgo_psicologicos FRP ON HCli.Clave_hc_px = FRP.Clave_hc_px " +
                                    "LEFT JOIN hc_habitos H ON HCli.Clave_hc_px = H.Clave_hc_px " +
                                    "LEFT JOIN hc_antecedentes_patologicos APA ON HCli.Clave_hc_px = APA.Clave_hc_px " +
                                    "LEFT JOIN hc_historial_consumo HC ON HCli.Clave_hc_px = HC.Clave_hc_px " +
                                    "LEFT JOIN hc_interrogatorio INT ON HCli.Clave_hc_px = INT.Clave_hc_px " +
                                    "LEFT JOIN hc_riesgo_caidas RC ON HCli.Clave_hc_px = RC.Clave_hc_px " +
                                    "LEFT JOIN hc_exploracion_fisica EF ON HCli.Clave_hc_px = EF.Clave_hc_px " +
                                    "WHERE HCli.Clave_hc_px = '" + Clave_hc_px + "' ";

                var result = db.Database.SqlQuery<Propiedades_HC>(query);
                HC = result.FirstOrDefault();

                return new JsonResult { Data = HC, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //Estos son los signos vitales que guardaron al estar dentro de la historia (entran a la secciones, se cargan los ultimos, y despues los guardan)
        public JsonResult ConsultarSignosVitales_Guardados(string expediente, string Clave_hc_px)
        {
            var ultimoRegistro = (from a in db.hc_SignosVitales
                                  where a.expediente == expediente
                                  where a.Clave_hc_px == Clave_hc_px
                                  select a).
                              OrderByDescending(r => r.fecha)
                              .FirstOrDefault();

            if (ultimoRegistro != null)
            {
                return new JsonResult { Data = ultimoRegistro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            else
            {
                return new JsonResult { Data = "", JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        public ActionResult ValidarPxParaMostrarHistorias(string expediente)
        {
            try
            {
                //Buscamos el px en la tabla 'Paciente'
                var PX = (from a in db.Paciente
                          where a.Expediente == expediente
                          select a).FirstOrDefault();

                //Buscamos si el px ya tiene registro de 'Datos Grales.'
                var HC = (from a in db.HistoriaClinica
                          where a.Id_Paciente == PX.Id
                          where a.TipoHistoria == "Datos Grales."
                          select a).FirstOrDefault();

                var TieneHistoria = 0;

                //Si tiene registro de Datos Grales. establecemos como '1' y si no como '0'
                if (HC != null)
                {
                    TieneHistoria = 1;
                }
                else
                {
                    TieneHistoria = 0;
                }
                return Json(new { MENSAJE = "Succe: ", Data = TieneHistoria }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}