using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    public class HistoriaPsicologiaController : Controller
    {
        Models.CUS db = new Models.CUS();
        Models.HC_Medicina hcMed = new Models.HC_Medicina();

        // GET: Admin/HistoriaPsicologia
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

        //********      Función para Guarda nueva HC PSICOLOGÍA
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
                hc.TipoHistoria = "Psicología";
                hc.Ident_HCcomun = Ultima_HCcomun.Clave_hc_px;//Este es el identificador de la ultima HC Común, que hará matcha con la HC Psicología
                db.HistoriaClinica.Add(hc);
                db.SaveChanges();

                claveHC = paciente.Expediente + "HC" + idConsecutivo;
            }
            return claveHC;
        }

        #region Guardar Pestañas de la H.C. Psicología

        [HttpPost]
        public ActionResult Familia(Models.hc_PS_Familia HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_Familia Historia = new Models.hc_PS_Familia();
                    Historia.Parentesco = HistoriaClinica.Parentesco;
                    Historia.NombreCompleto = HistoriaClinica.NombreCompleto;
                    Historia.Edad = HistoriaClinica.Edad;
                    Historia.Ocupacion = HistoriaClinica.Ocupacion;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_Familia.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class AtencionPsicologicaPropiedades
        {
            public string Presencia_Voluntaria { get; set; }
            public string Presencia_Sugerida { get; set; }
            public string Presencia_Obligatoria { get; set; }
            public string AnteriormenteAtencion { get; set; }
        }

        [HttpPost]
        public ActionResult AtencionPsi(AtencionPsicologicaPropiedades HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_AtencionPsicologica Historia = new Models.hc_PS_AtencionPsicologica();
                    if (HistoriaClinica.Presencia_Voluntaria == "on")
                    {
                        Historia.Presencia_Voluntaria = true;
                    }
                    else
                    {
                        Historia.Presencia_Voluntaria = false;
                    }
                    if (HistoriaClinica.Presencia_Sugerida == "on")
                    {
                        Historia.Presencia_Sugerida = true;
                    }
                    else
                    {
                        Historia.Presencia_Sugerida = false;
                    }
                    if (HistoriaClinica.Presencia_Obligatoria == "on")
                    {
                        Historia.Presencia_Obligatoria = true;
                    }
                    else
                    {
                        Historia.Presencia_Obligatoria = false;
                    }

                    Historia.AnteriormenteAtencion = HistoriaClinica.AnteriormenteAtencion;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_AtencionPsicologica.Add(Historia);
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
        public ActionResult Motivo(Models.hc_PS_MotivoConsulta HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_MotivoConsulta Historia = new Models.hc_PS_MotivoConsulta();
                    Historia.Motivo = HistoriaClinica.Motivo;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_MotivoConsulta.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class SintomatologiaActualPropiedades
        {
            public string Sintomatologia { get; set; }
            public string DificultadRespirar_Si { get; set; }
            public string DificultadRespirar_No { get; set; }
            public string DificultadRespirar_Esp { get; set; }
            public string SensacionMorir_Si { get; set; }
            public string SensacionMorir_No { get; set; }
            public string SensacionMorir_Esp { get; set; }
            public string TemblorCuerpo_Si { get; set; }
            public string TemblorCuerpo_No { get; set; }
            public string TemblorCuerpo_Esp { get; set; }
            public string Desmayo_Si { get; set; }
            public string Desmayo_No { get; set; }
            public string Desmayo_Esp { get; set; }
            public string Lloras_Si { get; set; }
            public string Lloras_No { get; set; }
            public string Lloras_Esp { get; set; }
            public string DespiertasAnimo_Si { get; set; }
            public string DespiertasAnimo_No { get; set; }
            public string DespiertasAnimo_Esp { get; set; }
            public string Culpable_Si { get; set; }
            public string Culpable_No { get; set; }
            public string Culpable_Esp { get; set; }
            public string PerdidoMotivacion_Si { get; set; }
            public string PerdidoMotivacion_No { get; set; }
            public string PerdidoMotivacion_Esp { get; set; }
            public string QuererMorir_Si { get; set; }
            public string QuererMorir_No { get; set; }
            public string QuererMorir_Esp { get; set; }
            public string Consultado_Si { get; set; }
            public string Consultado_No { get; set; }
            public string Consultado_Esp { get; set; }
            public string ProblemasAtencion_Si { get; set; }
            public string ProblemasAtencion_No { get; set; }
            public string ProblemasAtencion_Esp { get; set; }
            public string ProblemasDormir_Si { get; set; }
            public string ProblemasDormir_No { get; set; }
            public string ProblemasDormir_Esp { get; set; }
            public string CambioApetito_Si { get; set; }
            public string CambioApetito_No { get; set; }
            public string CambioApetito_Esp { get; set; }
            public string ParteTiempo_Triste { get; set; }
            public string ParteTiempo_Enojado { get; set; }
            public string ParteTiempo_Miedo { get; set; }
            public string ParteTiempo_Angustiado { get; set; }
            public string ParteTiempo_Estresado { get; set; }
            public string ParteTiempo_Maniaco { get; set; }
            public string ParteTiempo_Otra { get; set; }
            public string ParteTiempo_OtraEsp { get; set; }
        }

        [HttpPost]
        public ActionResult Sintomatologia(SintomatologiaActualPropiedades HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_SintomatologiaActual Historia = new Models.hc_PS_SintomatologiaActual();
                    Historia.Sintomatologia = HistoriaClinica.Sintomatologia;
                    if (HistoriaClinica.DificultadRespirar_Si == "on")
                    {
                        Historia.DificultadRespirar_Si = true;
                    }
                    else
                    {
                        Historia.DificultadRespirar_Si = false;
                    }
                    if (HistoriaClinica.DificultadRespirar_No == "on")
                    {
                        Historia.DificultadRespirar_No = true;
                    }
                    else
                    {
                        Historia.DificultadRespirar_No = false;
                    }
                    Historia.DificultadRespirar_Esp = HistoriaClinica.DificultadRespirar_Esp;
                    if (HistoriaClinica.SensacionMorir_Si == "on")
                    {
                        Historia.SensacionMorir_Si = true;
                    }
                    else
                    {
                        Historia.SensacionMorir_Si = false;
                    }
                    if (HistoriaClinica.SensacionMorir_No == "on")
                    {
                        Historia.SensacionMorir_No = true;
                    }
                    else
                    {
                        Historia.SensacionMorir_No = false;
                    }
                    Historia.SensacionMorir_Esp = HistoriaClinica.SensacionMorir_Esp;
                    if (HistoriaClinica.TemblorCuerpo_Si == "on")
                    {
                        Historia.TemblorCuerpo_Si = true;
                    }
                    else
                    {
                        Historia.TemblorCuerpo_Si = false;
                    }
                    if (HistoriaClinica.TemblorCuerpo_No == "on")
                    {
                        Historia.TemblorCuerpo_No = true;
                    }
                    else
                    {
                        Historia.TemblorCuerpo_No = false;
                    }
                    Historia.TemblorCuerpo_Esp = HistoriaClinica.TemblorCuerpo_Esp;
                    if (HistoriaClinica.Desmayo_Si == "on")
                    {
                        Historia.Desmayo_Si = true;
                    }
                    else
                    {
                        Historia.Desmayo_Si = false;
                    }
                    if (HistoriaClinica.Desmayo_No == "on")
                    {
                        Historia.Desmayo_No = true;
                    }
                    else
                    {
                        Historia.Desmayo_No = false;
                    }
                    Historia.Desmayo_Esp = HistoriaClinica.Desmayo_Esp;
                    if (HistoriaClinica.Lloras_Si == "on")
                    {
                        Historia.Lloras_Si = true;
                    }
                    else
                    {
                        Historia.Lloras_Si = false;
                    }
                    if (HistoriaClinica.Lloras_No == "on")
                    {
                        Historia.Lloras_No = true;
                    }
                    else
                    {
                        Historia.Lloras_No = false;
                    }
                    Historia.Lloras_Esp = HistoriaClinica.Lloras_Esp;
                    if (HistoriaClinica.DespiertasAnimo_Si == "on")
                    {
                        Historia.DespiertasAnimo_Si = true;
                    }
                    else
                    {
                        Historia.DespiertasAnimo_Si = false;
                    }
                    if (HistoriaClinica.DespiertasAnimo_No == "on")
                    {
                        Historia.DespiertasAnimo_No = true;
                    }
                    else
                    {
                        Historia.DespiertasAnimo_No = false;
                    }
                    Historia.DespiertasAnimo_Esp = HistoriaClinica.DespiertasAnimo_Esp;
                    if (HistoriaClinica.Culpable_Si == "on")
                    {
                        Historia.Culpable_Si = true;
                    }
                    else
                    {
                        Historia.Culpable_Si = false;
                    }
                    if (HistoriaClinica.Culpable_No == "on")
                    {
                        Historia.Culpable_No = true;
                    }
                    else
                    {
                        Historia.Culpable_No = false;
                    }
                    Historia.Culpable_Esp = HistoriaClinica.Culpable_Esp;
                    if (HistoriaClinica.PerdidoMotivacion_Si == "on")
                    {
                        Historia.PerdidoMotivacion_Si = true;
                    }
                    else
                    {
                        Historia.PerdidoMotivacion_Si = false;
                    }
                    if (HistoriaClinica.PerdidoMotivacion_No == "on")
                    {
                        Historia.PerdidoMotivacion_No = true;
                    }
                    else
                    {
                        Historia.PerdidoMotivacion_No = false;
                    }
                    Historia.PerdidoMotivacion_Esp = HistoriaClinica.PerdidoMotivacion_Esp;
                    if (HistoriaClinica.QuererMorir_Si == "on")
                    {
                        Historia.QuererMorir_Si = true;
                    }
                    else
                    {
                        Historia.QuererMorir_Si = false;
                    }
                    if (HistoriaClinica.QuererMorir_No == "on")
                    {
                        Historia.QuererMorir_No = true;
                    }
                    else
                    {
                        Historia.QuererMorir_No = false;
                    }
                    Historia.QuererMorir_Esp = HistoriaClinica.QuererMorir_Esp;
                    if (HistoriaClinica.Consultado_Si == "on")
                    {
                        Historia.Consultado_Si = true;
                    }
                    else
                    {
                        Historia.Consultado_Si = false;
                    }
                    if (HistoriaClinica.Consultado_No == "on")
                    {
                        Historia.Consultado_No = true;
                    }
                    else
                    {
                        Historia.Consultado_No = false;
                    }
                    Historia.Consultado_Esp = HistoriaClinica.Consultado_Esp;
                    if (HistoriaClinica.ProblemasAtencion_Si == "on")
                    {
                        Historia.ProblemasAtencion_Si = true;
                    }
                    else
                    {
                        Historia.ProblemasAtencion_Si = false;
                    }
                    if (HistoriaClinica.ProblemasAtencion_No == "on")
                    {
                        Historia.ProblemasAtencion_No = true;
                    }
                    else
                    {
                        Historia.ProblemasAtencion_No = false;
                    }
                    Historia.ProblemasAtencion_Esp = HistoriaClinica.ProblemasAtencion_Esp;
                    if (HistoriaClinica.ProblemasDormir_Si == "on")
                    {
                        Historia.ProblemasDormir_Si = true;
                    }
                    else
                    {
                        Historia.ProblemasDormir_Si = false;
                    }
                    if (HistoriaClinica.ProblemasDormir_No == "on")
                    {
                        Historia.ProblemasDormir_No = true;
                    }
                    else
                    {
                        Historia.ProblemasDormir_No = false;
                    }
                    Historia.ProblemasDormir_Esp = HistoriaClinica.ProblemasDormir_Esp;
                    if (HistoriaClinica.CambioApetito_Si == "on")
                    {
                        Historia.CambioApetito_Si = true;
                    }
                    else
                    {
                        Historia.CambioApetito_Si = false;
                    }
                    if (HistoriaClinica.CambioApetito_No == "on")
                    {
                        Historia.CambioApetito_No = true;
                    }
                    else
                    {
                        Historia.CambioApetito_No = false;
                    }
                    Historia.CambioApetito_Esp = HistoriaClinica.CambioApetito_Esp;
                    if (HistoriaClinica.ParteTiempo_Triste == "on")
                    {
                        Historia.ParteTiempo_Triste = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Triste = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Enojado == "on")
                    {
                        Historia.ParteTiempo_Enojado = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Enojado = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Miedo == "on")
                    {
                        Historia.ParteTiempo_Miedo = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Miedo = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Angustiado == "on")
                    {
                        Historia.ParteTiempo_Angustiado = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Angustiado = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Estresado == "on")
                    {
                        Historia.ParteTiempo_Estresado = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Estresado = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Maniaco == "on")
                    {
                        Historia.ParteTiempo_Maniaco = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Maniaco = false;
                    }
                    if (HistoriaClinica.ParteTiempo_Otra == "on")
                    {
                        Historia.ParteTiempo_Otra = true;
                    }
                    else
                    {
                        Historia.ParteTiempo_Otra = false;
                    }
                    Historia.ParteTiempo_OtraEsp = HistoriaClinica.ParteTiempo_OtraEsp;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_SintomatologiaActual.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class AreasAtencionPropiedades
        {
            public string Conductual { get; set; }
            public string Emocional { get; set; }
            public string RelacionesIn { get; set; }
            public string Neurologico { get; set; }
            public string Academico { get; set; }
        }

        [HttpPost]
        public ActionResult AreasAtencion(AreasAtencionPropiedades HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_AreasAtencion Historia = new Models.hc_PS_AreasAtencion();
                    if (HistoriaClinica.Conductual == "on")
                    {
                        Historia.Conductual = true;
                    }
                    else
                    {
                        Historia.Conductual = false;
                    }
                    if (HistoriaClinica.Emocional == "on")
                    {
                        Historia.Emocional = true;
                    }
                    else
                    {
                        Historia.Emocional = false;
                    }
                    if (HistoriaClinica.RelacionesIn == "on")
                    {
                        Historia.RelacionesIn = true;
                    }
                    else
                    {
                        Historia.RelacionesIn = false;
                    }
                    if (HistoriaClinica.Neurologico == "on")
                    {
                        Historia.Neurologico = true;
                    }
                    else
                    {
                        Historia.Neurologico = false;
                    }
                    if (HistoriaClinica.Academico == "on")
                    {
                        Historia.Academico = true;
                    }
                    else
                    {
                        Historia.Academico = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_AreasAtencion.Add(Historia);
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
        public ActionResult SignosAlarma(Models.hc_PS_SignosAlarma HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_SignosAlarma Historia = new Models.hc_PS_SignosAlarma();
                    Historia.Signo = HistoriaClinica.Signo;
                    Historia.Esp_Signo = HistoriaClinica.Esp_Signo;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_SignosAlarma.Add(Historia);
                    hcMed.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class DiagnosticoPresPropiedades
        {
            public string Diagnostico1 { get; set; }
            public string Diagnostico2 { get; set; }
            public string Diagnostico3 { get; set; }
            public string Diagnostico4 { get; set; }
            public string Diagnostico5 { get; set; }
            public string D_LigadoEvol { get; set; }
            public string Diag_Favorable { get; set; }
            public string D_Desfavorable { get; set; }
        }

        [HttpPost]
        public ActionResult DiagnosticoPres(DiagnosticoPresPropiedades HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_DiagnosticoPres Historia = new Models.hc_PS_DiagnosticoPres();
                    Historia.diagnostico1 = HistoriaClinica.Diagnostico1;
                    Historia.diagnostico2 = HistoriaClinica.Diagnostico2;
                    Historia.diagnostico3 = HistoriaClinica.Diagnostico3;
                    Historia.diagnostico4 = HistoriaClinica.Diagnostico4;
                    Historia.diagnostico5 = HistoriaClinica.Diagnostico5;

                    if (HistoriaClinica.D_LigadoEvol == "on")
                    {
                        Historia.LigadoEvolucion = true;
                    }
                    else
                    {
                        Historia.LigadoEvolucion = false;
                    }
                    if (HistoriaClinica.Diag_Favorable == "on")
                    {
                        Historia.Favorable = true;
                    }
                    else
                    {
                        Historia.Favorable = false;
                    }
                    if (HistoriaClinica.D_Desfavorable == "on")
                    {
                        Historia.Desfavorable = true;
                    }
                    else
                    {
                        Historia.Desfavorable = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_DiagnosticoPres.Add(Historia);
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
        public ActionResult IndicacionTera(Models.hc_PS_IndicacionTerapeutica HistoriaClinica, string expediente)
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
                        DateTime fechaL = fechaActual.AddHours(-3);
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 3 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Psicología");
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
                    Models.hc_PS_IndicacionTerapeutica Historia = new Models.hc_PS_IndicacionTerapeutica();
                    Historia.Plan = HistoriaClinica.Plan;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcMed.hc_PS_IndicacionTerapeutica.Add(Historia);
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
            //Familia
            public string Parentesco { get; set; }
            public string NombreCompleto { get; set; }
            public string Edad { get; set; }
            public string Ocupacion { get; set; }
            //AtencionPsicologica
            public bool? Presencia_Voluntaria { get; set; }
            public bool? Presencia_Sugerida { get; set; }
            public bool? Presencia_Obligatoria { get; set; }
            public string AnteriormenteAtencion { get; set; }
            //MotivoConsulta
            public string Motivo { get; set; }
            //SintomatologiaActual
            public string Sintomatologia { get; set; }
            public bool? DificultadRespirar_Si { get; set; }
            public bool? DificultadRespirar_No { get; set; }
            public string DificultadRespirar_Esp { get; set; }
            public bool? SensacionMorir_Si { get; set; }
            public bool? SensacionMorir_No { get; set; }
            public string SensacionMorir_Esp { get; set; }
            public bool? TemblorCuerpo_Si { get; set; }
            public bool? TemblorCuerpo_No { get; set; }
            public string TemblorCuerpo_Esp { get; set; }
            public bool? Desmayo_Si { get; set; }
            public bool? Desmayo_No { get; set; }
            public string Desmayo_Esp { get; set; }
            public bool? Lloras_Si { get; set; }
            public bool? Lloras_No { get; set; }
            public string Lloras_Esp { get; set; }
            public bool? DespiertasAnimo_Si { get; set; }
            public bool? DespiertasAnimo_No { get; set; }
            public string DespiertasAnimo_Esp { get; set; }
            public bool? Culpable_Si { get; set; }
            public bool? Culpable_No { get; set; }
            public string Culpable_Esp { get; set; }
            public bool? PerdidoMotivacion_Si { get; set; }
            public bool? PerdidoMotivacion_No { get; set; }
            public string PerdidoMotivacion_Esp { get; set; }
            public bool? QuererMorir_Si { get; set; }
            public bool? QuererMorir_No { get; set; }
            public string QuererMorir_Esp { get; set; }
            public bool? Consultado_Si { get; set; }
            public bool? Consultado_No { get; set; }
            public string Consultado_Esp { get; set; }
            public bool? ProblemasAtencion_Si { get; set; }
            public bool? ProblemasAtencion_No { get; set; }
            public string ProblemasAtencion_Esp { get; set; }
            public bool? ProblemasDormir_Si { get; set; }
            public bool? ProblemasDormir_No { get; set; }
            public string ProblemasDormir_Esp { get; set; }
            public bool? CambioApetito_Si { get; set; }
            public bool? CambioApetito_No { get; set; }
            public string CambioApetito_Esp { get; set; }
            public bool? ParteTiempo_Triste { get; set; }
            public bool? ParteTiempo_Enojado { get; set; }
            public bool? ParteTiempo_Miedo { get; set; }
            public bool? ParteTiempo_Angustiado { get; set; }
            public bool? ParteTiempo_Estresado { get; set; }
            public bool? ParteTiempo_Maniaco { get; set; }
            public bool? ParteTiempo_Otra { get; set; }
            public string ParteTiempo_OtraEsp { get; set; }
            //AreasAtencion
            public bool? Conductual { get; set; }
            public bool? Emocional { get; set; }
            public bool? RelacionesIn { get; set; }
            public bool? Neurologico { get; set; }
            public bool? Academico { get; set; }
            //SignosAlarma
            public string Signo { get; set; }
            public string Esp_Signo { get; set; }
            //DiagnosticoPresuntivo
            public string Diagnostico1 { get; set; }
            public string Diagnostico2 { get; set; }
            public string Diagnostico3 { get; set; }
            public string Diagnostico4 { get; set; }
            public string Diagnostico5 { get; set; }
            public bool? LigadoEvolucion { get; set; }
            public bool? Favorable { get; set; }
            public bool? Desfavorable { get; set; }
            //IndicacionTerapeutica
            public string Plan { get; set; }
        }

        //********      Función para buscar el detalle de la H.C. en el MODAL
        [HttpPost]
        public ActionResult ConsultarHC_Psic(string Clave_hc_px, string TipoHistoria)//Este parametro lo recivimos de la vista, "Clave_hc_px" viene siendo el Identificador armado de la HC que se desea ver
        {
            try
            {
                Propiedades_HC HC = new Propiedades_HC();

                string query =
                    "SELECT F.Parentesco, F.NombreCompleto, F.Edad, F.Ocupacion, " +
                    "AP.Presencia_Voluntaria, AP.Presencia_Sugerida, AP.Presencia_Obligatoria, AP.AnteriormenteAtencion, " +
                    "MC.Motivo, " +
                    "SA.Sintomatologia, SA.DificultadRespirar_Si, SA.DificultadRespirar_No, SA.DificultadRespirar_Esp, SA.SensacionMorir_Si, SA.SensacionMorir_No, SA.SensacionMorir_Esp, SA.TemblorCuerpo_Si, SA.TemblorCuerpo_No, SA.TemblorCuerpo_Esp, SA.Desmayo_Si, SA.Desmayo_No, SA.Desmayo_Esp, SA.Lloras_Si, SA.Lloras_No, SA.Lloras_Esp, SA.DespiertasAnimo_Si, SA.DespiertasAnimo_No, SA.DespiertasAnimo_Esp, " +
                    "SA.Culpable_Si, SA.Culpable_No, SA.Culpable_Esp, SA.PerdidoMotivacion_Si, SA.PerdidoMotivacion_No, SA.PerdidoMotivacion_Esp, SA.QuererMorir_Si, SA.QuererMorir_No, SA.QuererMorir_Esp, SA.Consultado_Si, SA.Consultado_No, SA.Consultado_Esp, SA.ProblemasAtencion_Si, SA.ProblemasAtencion_No, SA.ProblemasAtencion_Esp, SA.ProblemasDormir_Si, SA.ProblemasDormir_No, SA.ProblemasDormir_Esp, SA.CambioApetito_Si, " +
                    "SA.CambioApetito_No, SA.CambioApetito_Esp, SA.ParteTiempo_Triste, SA.ParteTiempo_Enojado, SA.ParteTiempo_Miedo, SA.ParteTiempo_Angustiado, SA.ParteTiempo_Estresado, SA.ParteTiempo_Maniaco, SA.ParteTiempo_Otra, SA.ParteTiempo_OtraEsp, " +
                    "AA.Conductual, AA.Emocional, AA.RelacionesIn, AA.Neurologico, AA.Academico, " +
                    "SIG.Signo, SIG.Esp_Signo, " +
                    "DP.Diagnostico1, DP.Diagnostico2, DP.Diagnostico3, DP.Diagnostico4, DP.Diagnostico5, DP.LigadoEvolucion, DP.Favorable, DP.Desfavorable, " +
                    "IT.[Plan] " +
                                    "FROM HistoriaClinica HCli " +
                                    "LEFT JOIN hc_PS_Familia F ON F.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_AtencionPsicologica AP ON AP.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_MotivoConsulta MC ON MC.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_SintomatologiaActual SA ON SA.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_AreasAtencion AA ON AA.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_SignosAlarma SIG ON SIG.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_DiagnosticoPres DP ON DP.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_PS_IndicacionTerapeutica IT ON IT.Clave_hc_px = HCli.Clave_hc_px " +
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
