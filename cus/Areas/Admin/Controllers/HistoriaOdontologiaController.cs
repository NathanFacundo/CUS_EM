
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace CUS.Areas.Admin.Controllers
{
    public class HistoriaOdontologiaController : Controller
    {
        Models.CUS db = new Models.CUS();
        Models.HC_Nutricion hcNut = new Models.HC_Nutricion();

        // GET: Admin/HistoriaOdontologia
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

        //********      Función para Guarda nueva HC NUTRICION
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
                hc.TipoHistoria = "Odontología";
                hc.Ident_HCcomun = Ultima_HCcomun.Clave_hc_px;//Este es el identificador de la ultima HC Común, que hará matcha con la HC Medicina
                db.HistoriaClinica.Add(hc);
                db.SaveChanges();

                claveHC = paciente.Expediente + "HC" + idConsecutivo;
            }
            return claveHC;
        }

        #region Guardar Pestañas de la H.C. Odontologia

        [HttpPost]
        public ActionResult HistoriaDen(Models.hc_ODO_HistoriaDental HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
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
                    Models.hc_ODO_HistoriaDental Historia = new Models.hc_ODO_HistoriaDental();
                    Historia.PrimeraVisitaODO = HistoriaClinica.PrimeraVisitaODO;
                    Historia.TratamientosRealizadoaODO = HistoriaClinica.TratamientosRealizadoaODO;
                    Historia.FamiliarPadecidoODO = HistoriaClinica.FamiliarPadecidoODO;
                    Historia.GolpeadoDientesODO = HistoriaClinica.GolpeadoDientesODO;
                    Historia.Especifica_GolpeadoDienODO = HistoriaClinica.Especifica_GolpeadoDienODO;
                    Historia.PacienteCooperoODO = HistoriaClinica.PacienteCooperoODO;
                    Historia.InteresConservarDienODO = HistoriaClinica.InteresConservarDienODO;
                    Historia.SatisfechoODO = HistoriaClinica.SatisfechoODO;
                    Historia.Especifica_SatisfechoODO = HistoriaClinica.Especifica_SatisfechoODO;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_HistoriaDental.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult InfoPerinata(Models.hc_ODO_InformacionPeri HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_InformacionPeri Historia = new Models.hc_ODO_InformacionPeri();
                    Historia.EmbarazoTerminoODO = HistoriaClinica.EmbarazoTerminoODO;
                    Historia.Motivo_EmbarazoTerODO = HistoriaClinica.Motivo_EmbarazoTerODO;
                    Historia.CompEmbarazoODO = HistoriaClinica.CompEmbarazoODO;
                    Historia.PesoNacerODO = HistoriaClinica.PesoNacerODO;
                    Historia.EstaturaNacerODO = HistoriaClinica.EstaturaNacerODO;
                    Historia.TipoLactanciaODO = HistoriaClinica.TipoLactanciaODO;
                    Historia.TiempoLactanciaODO = HistoriaClinica.TiempoLactanciaODO;
                    Historia.MostroCartillaODO = HistoriaClinica.MostroCartillaODO;
                    Historia.EsquemaCompletoODO = HistoriaClinica.EsquemaCompletoODO;
                    Historia.Especifica_EsquemaComODO = HistoriaClinica.Especifica_EsquemaComODO;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_InformacionPeri.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class HabitosPropiedades
        {
            public string ConsCarbohidratosODO { get; set; }
            public string AlimEntreComidasODO { get; set; }
            public string Especifica_AlimEntreComidasODO { get; set; }
            public string HigieneOralODO { get; set; }
            public string Frecuencia_HigieneOralODO { get; set; }
            public string Solo_HigieneOralODO { get; set; }
            public string ConAyuda_HigieneOralODO { get; set; }
            public string TipoCepillo_HigieneOralODO { get; set; }
            public string TipoPasta_HigieneOralODO { get; set; }
            public string SuccionLabialODO { get; set; }
            public string MorderObjetosODO { get; set; }
            public string RespiracionOralODO { get; set; }
            public string SuccionDigitalODO { get; set; }
            public string OnicofagiaODO { get; set; }
            public string BruxismoODO { get; set; }
            public string BiberonODO { get; set; }
            public string ChuponODO { get; set; }
        }

        [HttpPost]
        public ActionResult Habito(HabitosPropiedades HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Habitos Historia = new Models.hc_ODO_Habitos();
                    Historia.ConsCarbohidratosODO = HistoriaClinica.ConsCarbohidratosODO;
                    Historia.AlimEntreComidasODO = HistoriaClinica.AlimEntreComidasODO;
                    Historia.Especifica_AlimEntreComidasODO = HistoriaClinica.Especifica_AlimEntreComidasODO;
                    Historia.HigieneOralODO = HistoriaClinica.HigieneOralODO;
                    Historia.Frecuencia_HigieneOralODO = HistoriaClinica.Frecuencia_HigieneOralODO;
                    if (HistoriaClinica.Solo_HigieneOralODO == "on")
                    {
                        Historia.Solo_HigieneOralODO = true;
                    }
                    else
                    {
                        Historia.Solo_HigieneOralODO = false;
                    }
                    if (HistoriaClinica.ConAyuda_HigieneOralODO == "on")
                    {
                        Historia.ConAyuda_HigieneOralODO = true;
                    }
                    else
                    {
                        Historia.ConAyuda_HigieneOralODO = false;
                    }
                    Historia.TipoCepillo_HigieneOralODO = HistoriaClinica.TipoCepillo_HigieneOralODO;
                    Historia.TipoPasta_HigieneOralODO = HistoriaClinica.TipoPasta_HigieneOralODO;
                    if (HistoriaClinica.SuccionLabialODO == "on")
                    {
                        Historia.SuccionLabialODO = true;
                    }
                    else
                    {
                        Historia.SuccionLabialODO = false;
                    }
                    if (HistoriaClinica.MorderObjetosODO == "on")
                    {
                        Historia.MorderObjetosODO = true;
                    }
                    else
                    {
                        Historia.MorderObjetosODO = false;
                    }
                    if (HistoriaClinica.RespiracionOralODO == "on")
                    {
                        Historia.RespiracionOralODO = true;
                    }
                    else
                    {
                        Historia.RespiracionOralODO = false;
                    }
                    if (HistoriaClinica.SuccionDigitalODO == "on")
                    {
                        Historia.SuccionDigitalODO = true;
                    }
                    else
                    {
                        Historia.SuccionDigitalODO = false;
                    }
                    if (HistoriaClinica.OnicofagiaODO == "on")
                    {
                        Historia.OnicofagiaODO = true;
                    }
                    else
                    {
                        Historia.OnicofagiaODO = false;
                    }
                    if (HistoriaClinica.BruxismoODO == "on")
                    {
                        Historia.BruxismoODO = true;
                    }
                    else
                    {
                        Historia.BruxismoODO = false;
                    }
                    if (HistoriaClinica.BiberonODO == "on")
                    {
                        Historia.BiberonODO = true;
                    }
                    else
                    {
                        Historia.BiberonODO = false;
                    }
                    if (HistoriaClinica.ChuponODO == "on")
                    {
                        Historia.ChuponODO = true;
                    }
                    else
                    {
                        Historia.ChuponODO = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Habitos.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class EvaluacionFuncionalPropiedades
        {
            public string FA_Sup_Redondo { get; set; }
            public string FA_Sup_Triangular { get; set; }
            public string FA_Sup_Cuadrado { get; set; }
            public string FA_Inf_Redondo { get; set; }
            public string FA_Inf_Triangular { get; set; }
            public string FA_Inf_Cuadrado { get; set; }
            public string FA_MA_Anterior { get; set; }
            public string FA_MA_Posterior { get; set; }
            public string FA_MA_NoAplica { get; set; }
            public string FA_MC_Anterior { get; set; }
            public string FA_MC_Posterior { get; set; }
            public string FA_MC_NoAplica { get; set; }
            public string SMI_Der_EscalonM { get; set; }
            public string SMI_Der_EscalonD { get; set; }
            public string SMI_Der_PlanoRecto { get; set; }
            public string SMI_Der_NoAplica { get; set; }
            public string SMI_Izq_EscalonM { get; set; }
            public string SMI_Izq_EscalonD { get; set; }
            public string SMI_Izq_PlanoRecto { get; set; }
            public string SMI_Izq_NoAplica { get; set; }
            public string CPri_Der_ClaseI { get; set; }
            public string CPri_Der_ClaseII { get; set; }
            public string CPri_Der_ClaseIII { get; set; }
            public string CPri_Der_NoAplica { get; set; }
            public string CPri_Izq_ClaseI { get; set; }
            public string CPri_Izq_ClaseII { get; set; }
            public string CPri_Izq_ClaseIII { get; set; }
            public string CPri_Izq_NoAplica { get; set; }
            public string PMP_Der_ClaseI { get; set; }
            public string PMP_Der_ClaseII { get; set; }
            public string PMP_Der_ClaseIII { get; set; }
            public string PMP_Der_NoAplica { get; set; }
            public string PMP_Izq_ClaseI { get; set; }
            public string PMP_Izq_ClaseII { get; set; }
            public string PMP_Izq_ClaseIII { get; set; }
            public string PMP_Izq_NoAplica { get; set; }
            public string CPer_Der_ClaseI { get; set; }
            public string CPer_Der_ClaseII { get; set; }
            public string CPer_Der_ClaseIII { get; set; }
            public string CPer_Der_NoAplica { get; set; }
            public string CPer_Izq_ClaseI { get; set; }
            public string CPer_Izq_ClaseII { get; set; }
            public string CPer_Izq_ClaseIII { get; set; }
            public string CPer_Izq_NoAplica { get; set; }
        }

        [HttpPost]
        public ActionResult EvaluacionFunciona(EvaluacionFuncionalPropiedades HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_EvaluacionFuncional Historia = new Models.hc_ODO_EvaluacionFuncional();
                    
                    if (HistoriaClinica.FA_Sup_Redondo == "on")
                    {
                        Historia.FA_Sup_Redondo = true;
                    }
                    else
                    {
                        Historia.FA_Sup_Redondo = false;
                    }
                    if (HistoriaClinica.FA_Sup_Triangular == "on")
                    {
                        Historia.FA_Sup_Triangular = true;
                    }
                    else
                    {
                        Historia.FA_Sup_Triangular = false;
                    }
                    if (HistoriaClinica.FA_Sup_Cuadrado == "on")
                    {
                        Historia.FA_Sup_Cuadrado = true;
                    }
                    else
                    {
                        Historia.FA_Sup_Cuadrado = false;
                    }
                    if (HistoriaClinica.FA_Inf_Redondo == "on")
                    {
                        Historia.FA_Inf_Redondo = true;
                    }
                    else
                    {
                        Historia.FA_Inf_Redondo = false;
                    }
                    if (HistoriaClinica.FA_Inf_Triangular == "on")
                    {
                        Historia.FA_Inf_Triangular = true;
                    }
                    else
                    {
                        Historia.FA_Inf_Triangular = false;
                    }
                    if (HistoriaClinica.FA_Inf_Cuadrado == "on")
                    {
                        Historia.FA_Inf_Cuadrado = true;
                    }
                    else
                    {
                        Historia.FA_Inf_Cuadrado = false;
                    }
                    if (HistoriaClinica.FA_MA_Anterior == "on")
                    {
                        Historia.FA_MA_Anterior = true;
                    }
                    else
                    {
                        Historia.FA_MA_Anterior = false;
                    }
                    if (HistoriaClinica.FA_MA_Posterior == "on")
                    {
                        Historia.FA_MA_Posterior = true;
                    }
                    else
                    {
                        Historia.FA_MA_Posterior = false;
                    }
                    if (HistoriaClinica.FA_MA_NoAplica == "on")
                    {
                        Historia.FA_MA_NoAplica = true;
                    }
                    else
                    {
                        Historia.FA_MA_NoAplica = false;
                    }
                    if (HistoriaClinica.FA_MC_Anterior == "on")
                    {
                        Historia.FA_MC_Anterior = true;
                    }
                    else
                    {
                        Historia.FA_MC_Anterior = false;
                    }
                    if (HistoriaClinica.FA_MC_Posterior == "on")
                    {
                        Historia.FA_MC_Posterior = true;
                    }
                    else
                    {
                        Historia.FA_MC_Posterior = false;
                    }
                    if (HistoriaClinica.FA_MC_NoAplica == "on")
                    {
                        Historia.FA_MC_NoAplica = true;
                    }
                    else
                    {
                        Historia.FA_MC_NoAplica = false;
                    }
                    if (HistoriaClinica.SMI_Der_EscalonM == "on")
                    {
                        Historia.SMI_Der_EscalonM = true;
                    }
                    else
                    {
                        Historia.SMI_Der_EscalonM = false;
                    }
                    if (HistoriaClinica.SMI_Der_EscalonD == "on")
                    {
                        Historia.SMI_Der_EscalonD = true;
                    }
                    else
                    {
                        Historia.SMI_Der_EscalonD = false;
                    }
                    if (HistoriaClinica.SMI_Der_PlanoRecto == "on")
                    {
                        Historia.SMI_Der_PlanoRecto = true;
                    }
                    else
                    {
                        Historia.SMI_Der_PlanoRecto = false;
                    }
                    if (HistoriaClinica.SMI_Der_NoAplica == "on")
                    {
                        Historia.SMI_Der_NoAplica = true;
                    }
                    else
                    {
                        Historia.SMI_Der_NoAplica = false;
                    }
                    if (HistoriaClinica.SMI_Izq_EscalonM == "on")
                    {
                        Historia.SMI_Izq_EscalonM = true;
                    }
                    else
                    {
                        Historia.SMI_Izq_EscalonM = false;
                    }
                    if (HistoriaClinica.SMI_Izq_EscalonD == "on")
                    {
                        Historia.SMI_Izq_EscalonD = true;
                    }
                    else
                    {
                        Historia.SMI_Izq_EscalonD = false;
                    }
                    if (HistoriaClinica.SMI_Izq_PlanoRecto == "on")
                    {
                        Historia.SMI_Izq_PlanoRecto = true;
                    }
                    else
                    {
                        Historia.SMI_Izq_PlanoRecto = false;
                    }
                    if (HistoriaClinica.SMI_Izq_NoAplica == "on")
                    {
                        Historia.SMI_Izq_NoAplica = true;
                    }
                    else
                    {
                        Historia.SMI_Izq_NoAplica = false;
                    }
                    if (HistoriaClinica.CPri_Der_ClaseI == "on")
                    {
                        Historia.CPri_Der_ClaseI = true;
                    }
                    else
                    {
                        Historia.CPri_Der_ClaseI = false;
                    }
                    if (HistoriaClinica.CPri_Der_ClaseII == "on")
                    {
                        Historia.CPri_Der_ClaseII = true;
                    }
                    else
                    {
                        Historia.CPri_Der_ClaseII = false;
                    }
                    if (HistoriaClinica.CPri_Der_ClaseIII == "on")
                    {
                        Historia.CPri_Der_ClaseIII = true;
                    }
                    else
                    {
                        Historia.CPri_Der_ClaseIII = false;
                    }
                    if (HistoriaClinica.CPri_Der_NoAplica == "on")
                    {
                        Historia.CPri_Der_NoAplica = true;
                    }
                    else
                    {
                        Historia.CPri_Der_NoAplica = false;
                    }
                    if (HistoriaClinica.CPri_Izq_ClaseI == "on")
                    {
                        Historia.CPri_Izq_ClaseI = true;
                    }
                    else
                    {
                        Historia.CPri_Izq_ClaseI = false;
                    }
                    if (HistoriaClinica.CPri_Izq_ClaseII == "on")
                    {
                        Historia.CPri_Izq_ClaseII = true;
                    }
                    else
                    {
                        Historia.CPri_Izq_ClaseII = false;
                    }
                    if (HistoriaClinica.CPri_Izq_ClaseIII == "on")
                    {
                        Historia.CPri_Izq_ClaseIII = true;
                    }
                    else
                    {
                        Historia.CPri_Izq_ClaseIII = false;
                    }
                    if (HistoriaClinica.CPri_Izq_NoAplica == "on")
                    {
                        Historia.CPri_Izq_NoAplica = true;
                    }
                    else
                    {
                        Historia.CPri_Izq_NoAplica = false;
                    }
                    if (HistoriaClinica.PMP_Der_ClaseI == "on")
                    {
                        Historia.PMP_Der_ClaseI = true;
                    }
                    else
                    {
                        Historia.PMP_Der_ClaseI = false;
                    }
                    if (HistoriaClinica.PMP_Der_ClaseII == "on")
                    {
                        Historia.PMP_Der_ClaseII = true;
                    }
                    else
                    {
                        Historia.PMP_Der_ClaseII = false;
                    }
                    if (HistoriaClinica.PMP_Der_ClaseIII == "on")
                    {
                        Historia.PMP_Der_ClaseIII = true;
                    }
                    else
                    {
                        Historia.PMP_Der_ClaseIII = false;
                    }
                    if (HistoriaClinica.PMP_Der_NoAplica == "on")
                    {
                        Historia.PMP_Der_NoAplica = true;
                    }
                    else
                    {
                        Historia.PMP_Der_NoAplica = false;
                    }
                    if (HistoriaClinica.PMP_Izq_ClaseI == "on")
                    {
                        Historia.PMP_Izq_ClaseI = true;
                    }
                    else
                    {
                        Historia.PMP_Izq_ClaseI = false;
                    }
                    if (HistoriaClinica.PMP_Izq_ClaseII == "on")
                    {
                        Historia.PMP_Izq_ClaseII = true;
                    }
                    else
                    {
                        Historia.PMP_Izq_ClaseII = false;
                    }
                    if (HistoriaClinica.PMP_Izq_ClaseIII == "on")
                    {
                        Historia.PMP_Izq_ClaseIII = true;
                    }
                    else
                    {
                        Historia.PMP_Izq_ClaseIII = false;
                    }
                    if (HistoriaClinica.PMP_Izq_NoAplica == "on")
                    {
                        Historia.PMP_Izq_NoAplica = true;
                    }
                    else
                    {
                        Historia.PMP_Izq_NoAplica = false;
                    }
                    if (HistoriaClinica.CPer_Der_ClaseI == "on")
                    {
                        Historia.CPer_Der_ClaseI = true;
                    }
                    else
                    {
                        Historia.CPer_Der_ClaseI = false;
                    }
                    if (HistoriaClinica.CPer_Der_ClaseII == "on")
                    {
                        Historia.CPer_Der_ClaseII = true;
                    }
                    else
                    {
                        Historia.CPer_Der_ClaseII = false;
                    }
                    if (HistoriaClinica.CPer_Der_ClaseIII == "on")
                    {
                        Historia.CPer_Der_ClaseIII = true;
                    }
                    else
                    {
                        Historia.CPer_Der_ClaseIII = false;
                    }
                    if (HistoriaClinica.CPer_Der_NoAplica == "on")
                    {
                        Historia.CPer_Der_NoAplica = true;
                    }
                    else
                    {
                        Historia.CPer_Der_NoAplica = false;
                    }
                    if (HistoriaClinica.CPer_Izq_ClaseI == "on")
                    {
                        Historia.CPer_Izq_ClaseI = true;
                    }
                    else
                    {
                        Historia.CPer_Izq_ClaseI = false;
                    }
                    if (HistoriaClinica.CPer_Izq_ClaseII == "on")
                    {
                        Historia.CPer_Izq_ClaseII = true;
                    }
                    else
                    {
                        Historia.CPer_Izq_ClaseII = false;
                    }
                    if (HistoriaClinica.CPer_Izq_ClaseIII == "on")
                    {
                        Historia.CPer_Izq_ClaseIII = true;
                    }
                    else
                    {
                        Historia.CPer_Izq_ClaseIII = false;
                    }
                    if (HistoriaClinica.CPer_Izq_NoAplica == "on")
                    {
                        Historia.CPer_Izq_NoAplica = true;
                    }
                    else
                    {
                        Historia.CPer_Izq_NoAplica = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_EvaluacionFuncional.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AntecedentesOb(Models.hc_ODO_AntecedentesObs HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_AntecedentesObs Historia = new Models.hc_ODO_AntecedentesObs();
                    Historia.EmbarazadaODO = HistoriaClinica.EmbarazadaODO;
                    Historia.SemanaGestaODO = HistoriaClinica.SemanaGestaODO;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_AntecedentesObs.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Riesg(Models.hc_ODO_Riesgo HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Riesgo Historia = new Models.hc_ODO_Riesgo();
                    Historia.HemodialisisODO = HistoriaClinica.HemodialisisODO;
                    Historia.TatuajesPerforacionesODO = HistoriaClinica.TatuajesPerforacionesODO;
                    Historia.Tiempo_TatuajesPerfoODO = HistoriaClinica.Tiempo_TatuajesPerfoODO;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Riesgo.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class OdontogramaPropiedades
        {
            public string MasticaDientesODO { get; set; }

            public string CheckBox18 { get; set; }
            public string Input18 { get; set; }
            public string CheckBox17 { get; set; }
            public string Input17 { get; set; }
            public string CheckBox16 { get; set; }
            public string Input16 { get; set; }
            public string CheckBox15 { get; set; }
            public string Input15 { get; set; }
            public string CheckBox14 { get; set; }
            public string Input14 { get; set; }
            public string CheckBox13 { get; set; }
            public string Input13 { get; set; }
            public string CheckBox12 { get; set; }
            public string Input12 { get; set; }
            public string CheckBox11 { get; set; }
            public string Input11 { get; set; }
            public string CheckBox21 { get; set; }
            public string Input21 { get; set; }
            public string CheckBox22 { get; set; }
            public string Input22 { get; set; }
            public string CheckBox23 { get; set; }
            public string Input23 { get; set; }
            public string CheckBox24 { get; set; }
            public string Input24 { get; set; }
            public string CheckBox25 { get; set; }
            public string Input25 { get; set; }
            public string CheckBox26 { get; set; }
            public string Input26 { get; set; }
            public string CheckBox27 { get; set; }
            public string Input27 { get; set; }
            public string CheckBox28 { get; set; }
            public string Input28 { get; set; }
            public string CheckBox55 { get; set; }
            public string Input55 { get; set; }
            public string CheckBox54 { get; set; }
            public string Input54 { get; set; }
            public string CheckBox53 { get; set; }
            public string Input53 { get; set; }
            public string CheckBox52 { get; set; }
            public string Input52 { get; set; }
            public string CheckBox51 { get; set; }
            public string Input51 { get; set; }
            public string CheckBox61 { get; set; }
            public string Input61 { get; set; }
            public string CheckBox62 { get; set; }
            public string Input62 { get; set; }
            public string CheckBox63 { get; set; }
            public string Input63 { get; set; }
            public string CheckBox64 { get; set; }
            public string Input64 { get; set; }
            public string CheckBox65 { get; set; }
            public string Input65 { get; set; }
            public string CheckBox85 { get; set; }
            public string Input85 { get; set; }
            public string CheckBox84 { get; set; }
            public string Input84 { get; set; }
            public string CheckBox83 { get; set; }
            public string Input83 { get; set; }
            public string CheckBox82 { get; set; }
            public string Input82 { get; set; }
            public string CheckBox81 { get; set; }
            public string Input81 { get; set; }
            public string CheckBox71 { get; set; }
            public string Input71 { get; set; }
            public string CheckBox72 { get; set; }
            public string Input72 { get; set; }
            public string CheckBox73 { get; set; }
            public string Input73 { get; set; }
            public string CheckBox74 { get; set; }
            public string Input74 { get; set; }
            public string CheckBox75 { get; set; }
            public string Input75 { get; set; }
            public string CheckBox48 { get; set; }
            public string Input48 { get; set; }
            public string CheckBox47 { get; set; }
            public string Input47 { get; set; }
            public string CheckBox46 { get; set; }
            public string Input46 { get; set; }
            public string CheckBox45 { get; set; }
            public string Input45 { get; set; }
            public string CheckBox44 { get; set; }
            public string Input44 { get; set; }
            public string CheckBox43 { get; set; }
            public string Input43 { get; set; }
            public string CheckBox42 { get; set; }
            public string Input42 { get; set; }
            public string CheckBox41 { get; set; }
            public string Input41 { get; set; }
            public string CheckBox31 { get; set; }
            public string Input31 { get; set; }
            public string CheckBox32 { get; set; }
            public string Input32 { get; set; }
            public string CheckBox33 { get; set; }
            public string Input33 { get; set; }
            public string CheckBox34 { get; set; }
            public string Input34 { get; set; }
            public string CheckBox35 { get; set; }
            public string Input35 { get; set; }
            public string CheckBox36 { get; set; }
            public string Input36 { get; set; }
            public string CheckBox37 { get; set; }
            public string Input37 { get; set; }
            public string CheckBox38 { get; set; }
            public string Input38 { get; set; }

            public string AltaODO { get; set; }
            public string MediaODO { get; set; }
            public string BajaODO { get; set; }
        }

        [HttpPost]
        public ActionResult Odontogra(OdontogramaPropiedades HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Odontograma Historia = new Models.hc_ODO_Odontograma();
                    Historia.MasticaDientesODO = HistoriaClinica.MasticaDientesODO;

                    if (HistoriaClinica.CheckBox18 == "on")
                    {
                        Historia.CheckBox18 = true;
                    }
                    else
                    {
                        Historia.CheckBox18 = false;
                    }
                    Historia.Input18 = HistoriaClinica.Input18;
                    //
                    if (HistoriaClinica.CheckBox17 == "on")
                    {
                        Historia.CheckBox17 = true;
                    }
                    else
                    {
                        Historia.CheckBox17 = false;
                    }
                    Historia.Input17 = HistoriaClinica.Input17;
                    //
                    if (HistoriaClinica.CheckBox16 == "on")
                    {
                        Historia.CheckBox16 = true;
                    }
                    else
                    {
                        Historia.CheckBox16 = false;
                    }
                    Historia.Input16 = HistoriaClinica.Input16;
                    //
                    if (HistoriaClinica.CheckBox15 == "on")
                    {
                        Historia.CheckBox15 = true;
                    }
                    else
                    {
                        Historia.CheckBox15 = false;
                    }
                    Historia.Input15 = HistoriaClinica.Input15;
                    //
                    if (HistoriaClinica.CheckBox14 == "on")
                    {
                        Historia.CheckBox14 = true;
                    }
                    else
                    {
                        Historia.CheckBox14 = false;
                    }
                    Historia.Input14 = HistoriaClinica.Input14;
                    //
                    if (HistoriaClinica.CheckBox13 == "on")
                    {
                        Historia.CheckBox13 = true;
                    }
                    else
                    {
                        Historia.CheckBox13 = false;
                    }
                    Historia.Input13 = HistoriaClinica.Input13;
                    //
                    if (HistoriaClinica.CheckBox12 == "on")
                    {
                        Historia.CheckBox12 = true;
                    }
                    else
                    {
                        Historia.CheckBox12 = false;
                    }
                    Historia.Input12 = HistoriaClinica.Input12;
                    //
                    if (HistoriaClinica.CheckBox11 == "on")
                    {
                        Historia.CheckBox11 = true;
                    }
                    else
                    {
                        Historia.CheckBox11 = false;
                    }
                    Historia.Input11 = HistoriaClinica.Input11;
                    //
                    if (HistoriaClinica.CheckBox21 == "on")
                    {
                        Historia.CheckBox21 = true;
                    }
                    else
                    {
                        Historia.CheckBox21 = false;
                    }
                    Historia.Input21 = HistoriaClinica.Input21;
                    //
                    if (HistoriaClinica.CheckBox22 == "on")
                    {
                        Historia.CheckBox22 = true;
                    }
                    else
                    {
                        Historia.CheckBox22 = false;
                    }
                    Historia.Input22 = HistoriaClinica.Input22;
                    //
                    if (HistoriaClinica.CheckBox23 == "on")
                    {
                        Historia.CheckBox23 = true;
                    }
                    else
                    {
                        Historia.CheckBox23 = false;
                    }
                    Historia.Input23 = HistoriaClinica.Input23;
                    //
                    if (HistoriaClinica.CheckBox24 == "on")
                    {
                        Historia.CheckBox24 = true;
                    }
                    else
                    {
                        Historia.CheckBox24 = false;
                    }
                    Historia.Input24 = HistoriaClinica.Input24;
                    //
                    if (HistoriaClinica.CheckBox25 == "on")
                    {
                        Historia.CheckBox25 = true;
                    }
                    else
                    {
                        Historia.CheckBox25 = false;
                    }
                    Historia.Input25 = HistoriaClinica.Input25;
                    //
                    if (HistoriaClinica.CheckBox26 == "on")
                    {
                        Historia.CheckBox26 = true;
                    }
                    else
                    {
                        Historia.CheckBox26 = false;
                    }
                    Historia.Input26 = HistoriaClinica.Input26;
                    //
                    if (HistoriaClinica.CheckBox27 == "on")
                    {
                        Historia.CheckBox27 = true;
                    }
                    else
                    {
                        Historia.CheckBox27 = false;
                    }
                    Historia.Input27 = HistoriaClinica.Input27;
                    //
                    if (HistoriaClinica.CheckBox28 == "on")
                    {
                        Historia.CheckBox28 = true;
                    }
                    else
                    {
                        Historia.CheckBox28 = false;
                    }
                    Historia.Input28 = HistoriaClinica.Input28;
                    //
                    if (HistoriaClinica.CheckBox55 == "on")
                    {
                        Historia.CheckBox55 = true;
                    }
                    else
                    {
                        Historia.CheckBox55 = false;
                    }
                    Historia.Input55 = HistoriaClinica.Input55;
                    //
                    if (HistoriaClinica.CheckBox54 == "on")
                    {
                        Historia.CheckBox54 = true;
                    }
                    else
                    {
                        Historia.CheckBox54 = false;
                    }
                    Historia.Input54 = HistoriaClinica.Input54;
                    //
                    if (HistoriaClinica.CheckBox53 == "on")
                    {
                        Historia.CheckBox53 = true;
                    }
                    else
                    {
                        Historia.CheckBox53 = false;
                    }
                    Historia.Input53 = HistoriaClinica.Input53;
                    //
                    if (HistoriaClinica.CheckBox52 == "on")
                    {
                        Historia.CheckBox52 = true;
                    }
                    else
                    {
                        Historia.CheckBox52 = false;
                    }
                    Historia.Input52 = HistoriaClinica.Input52;
                    //
                    if (HistoriaClinica.CheckBox51 == "on")
                    {
                        Historia.CheckBox51 = true;
                    }
                    else
                    {
                        Historia.CheckBox51 = false;
                    }
                    Historia.Input51 = HistoriaClinica.Input51;
                    //
                    if (HistoriaClinica.CheckBox61 == "on")
                    {
                        Historia.CheckBox61 = true;
                    }
                    else
                    {
                        Historia.CheckBox61 = false;
                    }
                    Historia.Input61 = HistoriaClinica.Input61;
                    //
                    if (HistoriaClinica.CheckBox62 == "on")
                    {
                        Historia.CheckBox62 = true;
                    }
                    else
                    {
                        Historia.CheckBox62 = false;
                    }
                    Historia.Input62 = HistoriaClinica.Input62;
                    //
                    if (HistoriaClinica.CheckBox63 == "on")
                    {
                        Historia.CheckBox63 = true;
                    }
                    else
                    {
                        Historia.CheckBox63 = false;
                    }
                    Historia.Input63 = HistoriaClinica.Input63;
                    //
                    if (HistoriaClinica.CheckBox64 == "on")
                    {
                        Historia.CheckBox64 = true;
                    }
                    else
                    {
                        Historia.CheckBox64 = false;
                    }
                    Historia.Input64 = HistoriaClinica.Input64;
                    //
                    if (HistoriaClinica.CheckBox65 == "on")
                    {
                        Historia.CheckBox65 = true;
                    }
                    else
                    {
                        Historia.CheckBox65 = false;
                    }
                    Historia.Input65 = HistoriaClinica.Input65;
                    //
                    if (HistoriaClinica.CheckBox85 == "on")
                    {
                        Historia.CheckBox85 = true;
                    }
                    else
                    {
                        Historia.CheckBox85 = false;
                    }
                    Historia.Input85 = HistoriaClinica.Input85;
                    //
                    if (HistoriaClinica.CheckBox84 == "on")
                    {
                        Historia.CheckBox84 = true;
                    }
                    else
                    {
                        Historia.CheckBox84 = false;
                    }
                    Historia.Input84 = HistoriaClinica.Input84;
                    //
                    if (HistoriaClinica.CheckBox83 == "on")
                    {
                        Historia.CheckBox83 = true;
                    }
                    else
                    {
                        Historia.CheckBox83 = false;
                    }
                    Historia.Input83 = HistoriaClinica.Input83;
                    //
                    if (HistoriaClinica.CheckBox82 == "on")
                    {
                        Historia.CheckBox82 = true;
                    }
                    else
                    {
                        Historia.CheckBox82 = false;
                    }
                    Historia.Input82 = HistoriaClinica.Input82;
                    //
                    if (HistoriaClinica.CheckBox81 == "on")
                    {
                        Historia.CheckBox81 = true;
                    }
                    else
                    {
                        Historia.CheckBox81 = false;
                    }
                    Historia.Input81 = HistoriaClinica.Input81;
                    //
                    if (HistoriaClinica.CheckBox71 == "on")
                    {
                        Historia.CheckBox71 = true;
                    }
                    else
                    {
                        Historia.CheckBox71 = false;
                    }
                    Historia.Input71 = HistoriaClinica.Input71;
                    //
                    if (HistoriaClinica.CheckBox72 == "on")
                    {
                        Historia.CheckBox72 = true;
                    }
                    else
                    {
                        Historia.CheckBox72 = false;
                    }
                    Historia.Input72 = HistoriaClinica.Input72;
                    //
                    if (HistoriaClinica.CheckBox73 == "on")
                    {
                        Historia.CheckBox73 = true;
                    }
                    else
                    {
                        Historia.CheckBox73 = false;
                    }
                    Historia.Input73 = HistoriaClinica.Input73;
                    //
                    if (HistoriaClinica.CheckBox74 == "on")
                    {
                        Historia.CheckBox74 = true;
                    }
                    else
                    {
                        Historia.CheckBox74 = false;
                    }
                    Historia.Input74 = HistoriaClinica.Input74;
                    //
                    if (HistoriaClinica.CheckBox75 == "on")
                    {
                        Historia.CheckBox75 = true;
                    }
                    else
                    {
                        Historia.CheckBox75 = false;
                    }
                    Historia.Input75 = HistoriaClinica.Input75;
                    //
                    if (HistoriaClinica.CheckBox48 == "on")
                    {
                        Historia.CheckBox48 = true;
                    }
                    else
                    {
                        Historia.CheckBox48 = false;
                    }
                    Historia.Input48 = HistoriaClinica.Input48;
                    //
                    if (HistoriaClinica.CheckBox47 == "on")
                    {
                        Historia.CheckBox47 = true;
                    }
                    else
                    {
                        Historia.CheckBox47 = false;
                    }
                    Historia.Input47 = HistoriaClinica.Input47;
                    //
                    if (HistoriaClinica.CheckBox46 == "on")
                    {
                        Historia.CheckBox46 = true;
                    }
                    else
                    {
                        Historia.CheckBox46 = false;
                    }
                    Historia.Input46 = HistoriaClinica.Input46;
                    //
                    if (HistoriaClinica.CheckBox45 == "on")
                    {
                        Historia.CheckBox45 = true;
                    }
                    else
                    {
                        Historia.CheckBox45 = false;
                    }
                    Historia.Input45 = HistoriaClinica.Input45;
                    //
                    if (HistoriaClinica.CheckBox44 == "on")
                    {
                        Historia.CheckBox44 = true;
                    }
                    else
                    {
                        Historia.CheckBox44 = false;
                    }
                    Historia.Input44 = HistoriaClinica.Input44;
                    //
                    if (HistoriaClinica.CheckBox43 == "on")
                    {
                        Historia.CheckBox43 = true;
                    }
                    else
                    {
                        Historia.CheckBox43 = false;
                    }
                    Historia.Input43 = HistoriaClinica.Input43;
                    //
                    if (HistoriaClinica.CheckBox42 == "on")
                    {
                        Historia.CheckBox42 = true;
                    }
                    else
                    {
                        Historia.CheckBox42 = false;
                    }
                    Historia.Input42 = HistoriaClinica.Input42;
                    //
                    if (HistoriaClinica.CheckBox41 == "on")
                    {
                        Historia.CheckBox41 = true;
                    }
                    else
                    {
                        Historia.CheckBox41 = false;
                    }
                    Historia.Input41 = HistoriaClinica.Input41;
                    //
                    if (HistoriaClinica.CheckBox31 == "on")
                    {
                        Historia.CheckBox31 = true;
                    }
                    else
                    {
                        Historia.CheckBox31 = false;
                    }
                    Historia.Input31 = HistoriaClinica.Input31;
                    //
                    if (HistoriaClinica.CheckBox32 == "on")
                    {
                        Historia.CheckBox32 = true;
                    }
                    else
                    {
                        Historia.CheckBox32 = false;
                    }
                    Historia.Input32 = HistoriaClinica.Input32;
                    //
                    if (HistoriaClinica.CheckBox33 == "on")
                    {
                        Historia.CheckBox33 = true;
                    }
                    else
                    {
                        Historia.CheckBox33 = false;
                    }
                    Historia.Input33 = HistoriaClinica.Input33;
                    //
                    if (HistoriaClinica.CheckBox34 == "on")
                    {
                        Historia.CheckBox34 = true;
                    }
                    else
                    {
                        Historia.CheckBox34 = false;
                    }
                    Historia.Input34 = HistoriaClinica.Input34;
                    //
                    if (HistoriaClinica.CheckBox35 == "on")
                    {
                        Historia.CheckBox35 = true;
                    }
                    else
                    {
                        Historia.CheckBox35 = false;
                    }
                    Historia.Input35 = HistoriaClinica.Input35;
                    //
                    if (HistoriaClinica.CheckBox36 == "on")
                    {
                        Historia.CheckBox36 = true;
                    }
                    else
                    {
                        Historia.CheckBox36 = false;
                    }
                    Historia.Input36 = HistoriaClinica.Input36;
                    //
                    if (HistoriaClinica.CheckBox37 == "on")
                    {
                        Historia.CheckBox37 = true;
                    }
                    else
                    {
                        Historia.CheckBox37 = false;
                    }
                    Historia.Input37 = HistoriaClinica.Input37;
                    //
                    if (HistoriaClinica.CheckBox38 == "on")
                    {
                        Historia.CheckBox38 = true;
                    }
                    else
                    {
                        Historia.CheckBox38 = false;
                    }
                    Historia.Input38 = HistoriaClinica.Input38;
                    //

                    if (HistoriaClinica.AltaODO == "on")
                    {
                        Historia.AltaODO = true;
                    }
                    else
                    {
                        Historia.AltaODO = false;
                    }
                    if (HistoriaClinica.MediaODO == "on")
                    {
                        Historia.MediaODO = true;
                    }
                    else
                    {
                        Historia.MediaODO = false;
                    }
                    if (HistoriaClinica.BajaODO == "on")
                    {
                        Historia.BajaODO = true;
                    }
                    else
                    {
                        Historia.BajaODO = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Odontograma.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ResultadosLab(Models.hc_ODO_ResultadoLaboratorio HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_ResultadoLaboratorio Historia = new Models.hc_ODO_ResultadoLaboratorio();
                    Historia.ResultadoODO = HistoriaClinica.ResultadoODO;
                    Historia.Especifica_ResultadoODO = HistoriaClinica.Especifica_ResultadoODO;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_ResultadoLaboratorio.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ImpresionDia(Models.hc_ODO_ImpresionDiag HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_ImpresionDiag Historia = new Models.hc_ODO_ImpresionDiag();
                    Historia.diagnostico1ODO = HistoriaClinica.diagnostico1ODO;
                    Historia.diagnostico2ODO = HistoriaClinica.diagnostico2ODO;
                    Historia.diagnostico3ODO = HistoriaClinica.diagnostico3ODO;
                    Historia.diagnostico4ODO = HistoriaClinica.diagnostico4ODO;
                    Historia.diagnostico5ODO = HistoriaClinica.diagnostico5ODO;
                    Historia.tipo_diagnostico1 = HistoriaClinica.tipo_diagnostico1;
                    Historia.tipo_diagnostico2 = HistoriaClinica.tipo_diagnostico2;
                    Historia.tipo_diagnostico3 = HistoriaClinica.tipo_diagnostico3;
                    Historia.tipo_diagnostico4 = HistoriaClinica.tipo_diagnostico4;
                    Historia.tipo_diagnostico5 = HistoriaClinica.tipo_diagnostico5;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_ImpresionDiag.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult plan(Models.hc_ODO_Plan HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Plan Historia = new Models.hc_ODO_Plan();
                    Historia.PlanODO = HistoriaClinica.PlanODO;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Plan.Add(Historia);
                    hcNut.SaveChanges();
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
            public string LigadoEvolucionODO { get; set; }
            public string FavorableODO { get; set; }
            public string DesfavorableODO { get; set; }
        }

        [HttpPost]
        public ActionResult pronostico(PronosticoPropiedades HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Pronostico Historia = new Models.hc_ODO_Pronostico();
                    if (HistoriaClinica.LigadoEvolucionODO == "on")
                    {
                        Historia.LigadoEvolucionODO = true;
                    }
                    else
                    {
                        Historia.LigadoEvolucionODO = false;
                    }
                    if (HistoriaClinica.FavorableODO == "on")
                    {
                        Historia.FavorableODO = true;
                    }
                    else
                    {
                        Historia.FavorableODO = false;
                    }
                    if (HistoriaClinica.DesfavorableODO == "on")
                    {
                        Historia.DesfavorableODO = true;
                    }
                    else
                    {
                        Historia.DesfavorableODO = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Pronostico.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Otros(Models.hc_ODO_Otros HistoriaClinica, string expediente)
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
                        //utilizar fechaLimite para verificar si el paciente tiene un registro dentro de las últimas 1.5 horas y tambien validar el TIPO DE HISTORIA
                        pacienteTieneRegistroEnUltimas3Horas = db.HistoriaClinica
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Odontología");
                    }

                    var Id_claveHC = "";
                    if (pacienteTieneRegistroEnUltimas3Horas)// El paciente ya tiene un registro en las últimas 1.5 horas
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
                    Models.hc_ODO_Otros Historia = new Models.hc_ODO_Otros();
                    Historia.InterconsultaODO = HistoriaClinica.InterconsultaODO;
                    Historia.PadecimientoActualODO = HistoriaClinica.PadecimientoActualODO;
                    Historia.Especifica_PadecimientoActualODO = HistoriaClinica.Especifica_PadecimientoActualODO;
                    Historia.ProximaCitaODO = HistoriaClinica.ProximaCitaODO;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_ODO_Otros.Add(Historia);
                    hcNut.SaveChanges();
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
            //HISTORIA DENTAL
            public string PrimeraVisitaODO { get; set; }
            public string TratamientosRealizadoaODO { get; set; }
            public string FamiliarPadecidoODO { get; set; }
            public string GolpeadoDientesODO { get; set; }
            public string Especifica_GolpeadoDienODO { get; set; }
            public string PacienteCooperoODO { get; set; }
            public string InteresConservarDienODO { get; set; }
            public string SatisfechoODO { get; set; }
            public string Especifica_SatisfechoODO { get; set; }
            //INFORMACION PERI
            public string EmbarazoTerminoODO { get; set; }
            public string Motivo_EmbarazoTerODO { get; set; }
            public string CompEmbarazoODO { get; set; }
            public string PesoNacerODO { get; set; }
            public string EstaturaNacerODO { get; set; }
            public string TipoLactanciaODO { get; set; }
            public string TiempoLactanciaODO { get; set; }
            public string MostroCartillaODO { get; set; }
            public string EsquemaCompletoODO { get; set; }
            public string Especifica_EsquemaComODO { get; set; }
            //HABITOS
            public string ConsCarbohidratosODO { get; set; }
            public string AlimEntreComidasODO { get; set; }
            public string Especifica_AlimEntreComidasODO { get; set; }
            public string HigieneOralODO { get; set; }
            public string Frecuencia_HigieneOralODO { get; set; }
            public bool? Solo_HigieneOralODO { get; set; }
            public bool? ConAyuda_HigieneOralODO { get; set; }
            public string TipoCepillo_HigieneOralODO { get; set; }
            public string TipoPasta_HigieneOralODO { get; set; }
            public bool? SuccionLabialODO { get; set; }
            public bool? MorderObjetosODO { get; set; }
            public bool? RespiracionOralODO { get; set; }
            public bool? SuccionDigitalODO { get; set; }
            public bool? OnicofagiaODO { get; set; }
            public bool? BruxismoODO { get; set; }
            public bool? BiberonODO { get; set; }
            public bool? ChuponODO { get; set; }
            //EVALUACION FUNCIONAL
            public bool? FA_Sup_Redondo { get; set; }
            public bool? FA_Sup_Triangular { get; set; }
            public bool? FA_Sup_Cuadrado { get; set; }
            public bool? FA_Inf_Redondo { get; set; }
            public bool? FA_Inf_Triangular { get; set; }
            public bool? FA_Inf_Cuadrado { get; set; }
            public bool? FA_MA_Anterior { get; set; }
            public bool? FA_MA_Posterior { get; set; }
            public bool? FA_MA_NoAplica { get; set; }
            public bool? FA_MC_Anterior { get; set; }
            public bool? FA_MC_Posterior { get; set; }
            public bool? FA_MC_NoAplica { get; set; }
            public bool? SMI_Der_EscalonM { get; set; }
            public bool? SMI_Der_EscalonD { get; set; }
            public bool? SMI_Der_PlanoRecto { get; set; }
            public bool? SMI_Der_NoAplica { get; set; }
            public bool? SMI_Izq_EscalonM { get; set; }
            public bool? SMI_Izq_EscalonD { get; set; }
            public bool? SMI_Izq_PlanoRecto { get; set; }
            public bool? SMI_Izq_NoAplica { get; set; }
            public bool? CPri_Der_ClaseI { get; set; }
            public bool? CPri_Der_ClaseII { get; set; }
            public bool? CPri_Der_ClaseIII { get; set; }
            public bool? CPri_Der_NoAplica { get; set; }
            public bool? CPri_Izq_ClaseI { get; set; }
            public bool? CPri_Izq_ClaseII { get; set; }
            public bool? CPri_Izq_ClaseIII { get; set; }
            public bool? CPri_Izq_NoAplica { get; set; }
            public bool? PMP_Der_ClaseI { get; set; }
            public bool? PMP_Der_ClaseII { get; set; }
            public bool? PMP_Der_ClaseIII { get; set; }
            public bool? PMP_Der_NoAplica { get; set; }
            public bool? PMP_Izq_ClaseI { get; set; }
            public bool? PMP_Izq_ClaseII { get; set; }
            public bool? PMP_Izq_ClaseIII { get; set; }
            public bool? PMP_Izq_NoAplica { get; set; }
            public bool? CPer_Der_ClaseI { get; set; }
            public bool? CPer_Der_ClaseII { get; set; }
            public bool? CPer_Der_ClaseIII { get; set; }
            public bool? CPer_Der_NoAplica { get; set; }
            public bool? CPer_Izq_ClaseI { get; set; }
            public bool? CPer_Izq_ClaseII { get; set; }
            public bool? CPer_Izq_ClaseIII { get; set; }
            public bool? CPer_Izq_NoAplica { get; set; }
            //ANTECEDENTES OBS
            public string EmbarazadaODO { get; set; }
            public string SemanaGestaODO { get; set; }
            //RIESGO
            public string HemodialisisODO { get; set; }
            public string TatuajesPerforacionesODO { get; set; }
            public string Tiempo_TatuajesPerfoODO { get; set; }
            //ODONTOGRAMA
            public string MasticaDientesODO { get; set; }
            public bool? CheckBox18 { get; set; }
            public string Input18 { get; set; }
            public bool? CheckBox17 { get; set; }
            public string Input17 { get; set; }
            public bool? CheckBox16 { get; set; }
            public string Input16 { get; set; }
            public bool? CheckBox15 { get; set; }
            public string Input15 { get; set; }
            public bool? CheckBox14 { get; set; }
            public string Input14 { get; set; }
            public bool? CheckBox13 { get; set; }
            public string Input13 { get; set; }
            public bool? CheckBox12 { get; set; }
            public string Input12 { get; set; }
            public bool? CheckBox11 { get; set; }
            public string Input11 { get; set; }
            public bool? CheckBox21 { get; set; }
            public string Input21 { get; set; }
            public bool? CheckBox22 { get; set; }
            public string Input22 { get; set; }
            public bool? CheckBox23 { get; set; }
            public string Input23 { get; set; }
            public bool? CheckBox24 { get; set; }
            public string Input24 { get; set; }
            public bool? CheckBox25 { get; set; }
            public string Input25 { get; set; }
            public bool? CheckBox26 { get; set; }
            public string Input26 { get; set; }
            public bool? CheckBox27 { get; set; }
            public string Input27 { get; set; }
            public bool? CheckBox28 { get; set; }
            public string Input28 { get; set; }
            public bool? CheckBox55 { get; set; }
            public string Input55 { get; set; }
            public bool? CheckBox54 { get; set; }
            public string Input54 { get; set; }
            public bool? CheckBox53 { get; set; }
            public string Input53 { get; set; }
            public bool? CheckBox52 { get; set; }
            public string Input52 { get; set; }
            public bool? CheckBox51 { get; set; }
            public string Input51 { get; set; }
            public bool? CheckBox61 { get; set; }
            public string Input61 { get; set; }
            public bool? CheckBox62 { get; set; }
            public string Input62 { get; set; }
            public bool? CheckBox63 { get; set; }
            public string Input63 { get; set; }
            public bool? CheckBox64 { get; set; }
            public string Input64 { get; set; }
            public bool? CheckBox65 { get; set; }
            public string Input65 { get; set; }
            public bool? CheckBox85 { get; set; }
            public string Input85 { get; set; }
            public bool? CheckBox84 { get; set; }
            public string Input84 { get; set; }
            public bool? CheckBox83 { get; set; }
            public string Input83 { get; set; }
            public bool? CheckBox82 { get; set; }
            public string Input82 { get; set; }
            public bool? CheckBox81 { get; set; }
            public string Input81 { get; set; }
            public bool? CheckBox71 { get; set; }
            public string Input71 { get; set; }
            public bool? CheckBox72 { get; set; }
            public string Input72 { get; set; }
            public bool? CheckBox73 { get; set; }
            public string Input73 { get; set; }
            public bool? CheckBox74 { get; set; }
            public string Input74 { get; set; }
            public bool? CheckBox75 { get; set; }
            public string Input75 { get; set; }
            public bool? CheckBox48 { get; set; }
            public string Input48 { get; set; }
            public bool? CheckBox47 { get; set; }
            public string Input47 { get; set; }
            public bool? CheckBox46 { get; set; }
            public string Input46 { get; set; }
            public bool? CheckBox45 { get; set; }
            public string Input45 { get; set; }
            public bool? CheckBox44 { get; set; }
            public string Input44 { get; set; }
            public bool? CheckBox43 { get; set; }
            public string Input43 { get; set; }
            public bool? CheckBox42 { get; set; }
            public string Input42 { get; set; }
            public bool? CheckBox41 { get; set; }
            public string Input41 { get; set; }
            public bool? CheckBox31 { get; set; }
            public string Input31 { get; set; }
            public bool? CheckBox32 { get; set; }
            public string Input32 { get; set; }
            public bool? CheckBox33 { get; set; }
            public string Input33 { get; set; }
            public bool? CheckBox34 { get; set; }
            public string Input34 { get; set; }
            public bool? CheckBox35 { get; set; }
            public string Input35 { get; set; }
            public bool? CheckBox36 { get; set; }
            public string Input36 { get; set; }
            public bool? CheckBox37 { get; set; }
            public string Input37 { get; set; }
            public bool? CheckBox38 { get; set; }
            public string Input38 { get; set; }
            public bool? AltaODO { get; set; }
            public bool? MediaODO { get; set; }
            public bool? BajaODO { get; set; }
            //RESULTADOS LAB
            public string ResultadoODO { get; set; }
            public string Especifica_ResultadoODO { get; set; }
            //IMPRESIÓN DIAGNOSTICA
            public string diagnostico1ODO { get; set; }
            public string diagnostico2ODO { get; set; }
            public string diagnostico3ODO { get; set; }
            public string diagnostico4ODO { get; set; }
            public string diagnostico5ODO { get; set; }
            public string tipo_diagnostico1 { get; set; }
            public string tipo_diagnostico2 { get; set; }
            public string tipo_diagnostico3 { get; set; }
            public string tipo_diagnostico4 { get; set; }
            public string tipo_diagnostico5 { get; set; }
            //Plan
            public string PlanODO { get; set; }
            //Pronostico
            public bool? LigadoEvolucionODO { get; set; }
            public bool? FavorableODO { get; set; }
            public bool? DesfavorableODO { get; set; }
            //Otros
            public string InterconsultaODO { get; set; }
            public string PadecimientoActualODO { get; set; }
            public string Especifica_PadecimientoActualODO { get; set; }
            public string ProximaCitaODO { get; set; }
        }

        //********      Función para buscar el detalle de la H.C. en el MODAL
        [HttpPost]
        public ActionResult ConsultarHC_Odo(string Clave_hc_px, string TipoHistoria)//Este parametro lo recivimos de la vista, "Clave_hc_px" viene siendo el Identificador armado de la HC que se desea ver
        {
            try
            {
                Propiedades_HC HC = new Propiedades_HC();

                string query =
                "SELECT HD.PrimeraVisitaODO, HD.TratamientosRealizadoaODO, HD.FamiliarPadecidoODO, HD.GolpeadoDientesODO, HD.Especifica_GolpeadoDienODO, HD.PacienteCooperoODO, HD.InteresConservarDienODO, HD.SatisfechoODO, HD.Especifica_SatisfechoODO, " +
                "InP.EmbarazoTerminoODO, InP.Motivo_EmbarazoTerODO, InP.CompEmbarazoODO, InP.PesoNacerODO, InP.EstaturaNacerODO, InP.TipoLactanciaODO, InP.TiempoLactanciaODO, InP.MostroCartillaODO, InP.EsquemaCompletoODO, InP.Especifica_EsquemaComODO, " +
                "H.ConsCarbohidratosODO, H.AlimEntreComidasODO, H.Especifica_AlimEntreComidasODO, H.HigieneOralODO, H.Frecuencia_HigieneOralODO, H.Solo_HigieneOralODO, H.ConAyuda_HigieneOralODO, H.TipoCepillo_HigieneOralODO, H.TipoPasta_HigieneOralODO, H.SuccionLabialODO, H.MorderObjetosODO, H.RespiracionOralODO, H.SuccionDigitalODO, H.OnicofagiaODO, H.BruxismoODO, H.BiberonODO, H.ChuponODO, " +
                "EF.FA_Sup_Redondo, EF.FA_Sup_Triangular, EF.FA_Sup_Cuadrado, EF.FA_Inf_Redondo, EF.FA_Inf_Triangular, EF.FA_Inf_Cuadrado, EF.FA_MA_Anterior, EF.FA_MA_Posterior, EF.FA_MA_NoAplica, EF.FA_MC_Anterior, EF.FA_MC_Posterior, EF.FA_MC_NoAplica, " +
                "EF.SMI_Der_EscalonM, EF.SMI_Der_EscalonD, EF.SMI_Der_PlanoRecto, EF.SMI_Der_NoAplica, EF.SMI_Izq_EscalonM, EF.SMI_Izq_EscalonD, EF.SMI_Izq_PlanoRecto, EF.SMI_Izq_NoAplica, " +
                "EF.CPri_Der_ClaseI, EF.CPri_Der_ClaseII, EF.CPri_Der_ClaseIII, EF.CPri_Der_NoAplica, EF.CPri_Izq_ClaseI, EF.CPri_Izq_ClaseII, EF.CPri_Izq_ClaseIII, EF.CPri_Izq_NoAplica, " +
                "EF.PMP_Der_ClaseI, EF.PMP_Der_ClaseII, EF.PMP_Der_ClaseIII, EF.PMP_Der_NoAplica, EF.PMP_Izq_ClaseI, EF.PMP_Izq_ClaseII, EF.PMP_Izq_ClaseIII, EF.PMP_Izq_NoAplica, " +
                "EF.CPer_Der_ClaseI, EF.CPer_Der_ClaseII, EF.CPer_Der_ClaseIII, EF.CPer_Der_NoAplica, EF.CPer_Izq_ClaseI, EF.CPer_Izq_ClaseII, EF.CPer_Izq_ClaseIII, EF.CPer_Izq_NoAplica, " +
                "AO.EmbarazadaODO, AO.SemanaGestaODO, " +
                "R.HemodialisisODO, R.TatuajesPerforacionesODO, R.Tiempo_TatuajesPerfoODO, " +
                "Od.MasticaDientesODO,Od.CheckBox18,Od.Input18,Od.CheckBox17,Od.Input17,Od.CheckBox16,Od.Input16,Od.CheckBox15,Od.Input15,Od.CheckBox14,Od.Input14,Od.CheckBox13,Od.Input13,Od.CheckBox12,Od.Input12,Od.CheckBox11,Od.Input11,Od.CheckBox21,Od.Input21,Od.CheckBox22,Od.Input22,Od.CheckBox23,Od.Input23,Od.CheckBox24,Od.Input24,Od.CheckBox25,Od.Input25,Od.CheckBox26,Od.Input26,Od.CheckBox27,Od.Input27,Od.CheckBox28,Od.Input28,Od.CheckBox55,Od.Input55, " +
                "Od.CheckBox54,Od.Input54,Od.CheckBox53,Od.Input53,Od.CheckBox52,Od.Input52,Od.CheckBox51,Od.Input51,Od.CheckBox61,Od.Input61,Od.CheckBox62,Od.Input62,Od.CheckBox63,Od.Input63,Od.CheckBox64,Od.Input64,Od.CheckBox65,Od.Input65,Od.CheckBox85,Od.Input85,Od.CheckBox84,Od.Input84,Od.CheckBox83,Od.Input83,Od.CheckBox82,Od.Input82,Od.CheckBox81,Od.Input81,Od.CheckBox71,Od.Input71,Od.CheckBox72,Od.Input72,Od.CheckBox73,Od.Input73,Od.CheckBox74,Od.Input74, " +
                "Od.CheckBox75,Od.Input75,Od.CheckBox48,Od.Input48,Od.CheckBox47,Od.Input47,Od.CheckBox46,Od.Input46,Od.CheckBox45,Od.Input45,Od.CheckBox44,Od.Input44,Od.CheckBox43,Od.Input43,Od.CheckBox42,Od.Input42,Od.CheckBox41,Od.Input41,Od.CheckBox31,Od.Input31,Od.CheckBox32,Od.Input32,Od.CheckBox33,Od.Input33,Od.CheckBox34,Od.Input34,Od.CheckBox35,Od.Input35,Od.CheckBox36,Od.Input36,Od.CheckBox37,Od.Input37,Od.CheckBox38,Od.Input38,Od.AltaODO,Od.MediaODO,Od.BajaODO, " +
                "RL.ResultadoODO, RL.Especifica_ResultadoODO, " +
                "ID.diagnostico1ODO,ID.diagnostico2ODO,ID.diagnostico3ODO,ID.diagnostico4ODO,ID.diagnostico5ODO, ID.tipo_diagnostico1, ID.tipo_diagnostico2, ID.tipo_diagnostico3, ID.tipo_diagnostico4, ID.tipo_diagnostico5, " +
                "PL.[PlanODO], " +
                "PR.LigadoEvolucionODO, PR.FavorableODO, PR.DesfavorableODO, " +
                "O.InterconsultaODO, O.PadecimientoActualODO, O.Especifica_PadecimientoActualODO, O.ProximaCitaODO " +
                        "FROM HistoriaClinica HCli " +
                                "LEFT JOIN hc_ODO_HistoriaDental      HD ON HD.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_InformacionPeri     InP ON InP.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Habitos             H ON H.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_EvaluacionFuncional EF ON EF.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_AntecedentesObs     AO ON AO.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Riesgo              R ON R.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Odontograma         Od ON Od.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_ResultadoLaboratorio RL ON RL.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_ImpresionDiag        ID ON ID.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Plan                 PL ON PL.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Pronostico           PR ON PR.Clave_hc_px = HCli.Clave_hc_px " +
                                "LEFT JOIN hc_ODO_Otros                O ON O.Clave_hc_px = HCli.Clave_hc_px " +
                                        "WHERE HCli.Clave_hc_px = '" + Clave_hc_px + "' ";

                var result = hcNut.Database.SqlQuery<Propiedades_HC>(query);
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
