
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


    }
}
