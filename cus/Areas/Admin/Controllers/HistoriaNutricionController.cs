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
                hc.TipoHistoria = "Nutrición";
                hc.Ident_HCcomun = Ultima_HCcomun.Clave_hc_px;//Este es el identificador de la ultima HC Común, que hará matcha con la HC Medicina
                db.HistoriaClinica.Add(hc);
                db.SaveChanges();

                claveHC = paciente.Expediente + "HC" + idConsecutivo;
            }
            return claveHC;
        }

        #region Guardar Pestañas de la H.C. Medicina
        [HttpPost]
        public ActionResult ResultadoLab(Models.hc_NUT_ResultadoLab HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_ResultadoLab Historia = new Models.hc_NUT_ResultadoLab();
                    Historia.Resultado = HistoriaClinica.Resultado;
                    Historia.Especifica_Resultado = HistoriaClinica.Especifica_Resultado;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_ResultadoLab.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class EtapaCamPropiedades
        {
            public string Viene { get; set; }
            public string DeseaLograr { get; set; }
            public string Precontemplacion { get; set; }
            public string Contemplacion { get; set; }
            public string Preparacion { get; set; }
            public string Mantenimiento { get; set; }
            public string Terminacion { get; set; }
        }

        [HttpPost]
        public ActionResult EtapaCam(EtapaCamPropiedades HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_EtapaCambio Historia = new Models.hc_NUT_EtapaCambio();
                    Historia.Viene = HistoriaClinica.Viene;
                    Historia.DeseaLograr = HistoriaClinica.DeseaLograr;
                    if (HistoriaClinica.Precontemplacion == "on")
                    {
                        Historia.Precontemplacion = true;
                    }
                    else
                    {
                        Historia.Precontemplacion = false;
                    }
                    if (HistoriaClinica.Contemplacion == "on")
                    {
                        Historia.Contemplacion = true;
                    }
                    else
                    {
                        Historia.Contemplacion = false;
                    }
                    if (HistoriaClinica.Preparacion == "on")
                    {
                        Historia.Preparacion = true;
                    }
                    else
                    {
                        Historia.Preparacion = false;
                    }
                    if (HistoriaClinica.Mantenimiento == "on")
                    {
                        Historia.Mantenimiento = true;
                    }
                    else
                    {
                        Historia.Mantenimiento = false;
                    }
                    if (HistoriaClinica.Terminacion == "on")
                    {
                        Historia.Terminacion = true;
                    }
                    else
                    {
                        Historia.Terminacion = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_EtapaCambio.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class DatosDietPropiedades
        {
            public string Comidas { get; set; }
            public string Desayuno { get; set; }
            public string Comidas2 { get; set; }
            public string Cena { get; set; }
            public string ColacionMat { get; set; }
            public string ColacionVes { get; set; }
            public string TiempoConsumir { get; set; }
            public string Suplementos { get; set; }
            public string Especifica_Suplementos { get; set; }
            public string AlimentosDisgustan { get; set; }
            public string AlimentosFavoritos { get; set; }
            public string ConsumoAgua { get; set; }
            public string Especifique_ConsumoA { get; set; }
            public string HorasSuenio { get; set; }
            public string ActividadFisica { get; set; }
            public string Tipo_ActividadF { get; set; }
            public string Tiempo_ActividadF { get; set; }
            public string Frecuencia_ActividadF { get; set; }
        }

        [HttpPost]
        public ActionResult DatosDiet(DatosDietPropiedades HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_DatosDieteticos Historia = new Models.hc_NUT_DatosDieteticos();
                    Historia.Comidas = HistoriaClinica.Comidas;
                    if (HistoriaClinica.Desayuno == "on")
                    {
                        Historia.Desayuno = true;
                    }
                    else
                    {
                        Historia.Desayuno = false;
                    }
                    if (HistoriaClinica.Comidas2 == "on")
                    {
                        Historia.Comidas2 = true;
                    }
                    else
                    {
                        Historia.Comidas2 = false;
                    }
                    if (HistoriaClinica.Cena == "on")
                    {
                        Historia.Cena = true;
                    }
                    else
                    {
                        Historia.Cena = false;
                    }
                    if (HistoriaClinica.ColacionMat == "on")
                    {
                        Historia.ColacionMat = true;
                    }
                    else
                    {
                        Historia.ColacionMat = false;
                    }
                    if (HistoriaClinica.ColacionVes == "on")
                    {
                        Historia.ColacionVes = true;
                    }
                    else
                    {
                        Historia.ColacionVes = false;
                    }
                    Historia.TiempoConsumir = HistoriaClinica.TiempoConsumir;
                    Historia.Suplementos = HistoriaClinica.Suplementos;
                    Historia.Especifica_Supl = HistoriaClinica.Especifica_Suplementos;
                    Historia.AlimentosDisgustan = HistoriaClinica.AlimentosDisgustan;
                    Historia.AlimentosFavoritos = HistoriaClinica.AlimentosFavoritos;
                    Historia.ConsumoAgua = HistoriaClinica.ConsumoAgua;
                    Historia.Especifica_ConsumoAgua = HistoriaClinica.Especifique_ConsumoA;
                    Historia.HorasSuenio = HistoriaClinica.HorasSuenio;
                    Historia.ActividadFisica = HistoriaClinica.ActividadFisica;
                    Historia.Tipo_ActividadFisica = HistoriaClinica.Tipo_ActividadF;
                    Historia.Tiempo_ActividadFisica = HistoriaClinica.Tiempo_ActividadF;
                    Historia.Frec_ActividadFisica = HistoriaClinica.Frecuencia_ActividadF;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_DatosDieteticos.Add(Historia);
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
        public ActionResult AlimentacionIn(Models.hc_NUT_AlimentacionInicial HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_AlimentacionInicial Historia = new Models.hc_NUT_AlimentacionInicial();
                    Historia.LactanciaMaterna = HistoriaClinica.LactanciaMaterna;
                    Historia.Tiempo_LactanciaM = HistoriaClinica.Tiempo_LactanciaM;
                    Historia.Sucedaneo = HistoriaClinica.Sucedaneo;
                    Historia.Cual_Sucedaneo = HistoriaClinica.Cual_Sucedaneo;
                    Historia.AlimentacionComp = HistoriaClinica.AlimentacionComp;
                    Historia.Edad_AlimentacionC = HistoriaClinica.Edad_AlimentacionC;
                    Historia.Motivo_AlimentacionC = HistoriaClinica.Motivo_AlimentacionC;
                    
                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_AlimentacionInicial.Add(Historia);
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
        public ActionResult Recordatorio(Models.hc_NUT_Recordatorio HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_Recordatorio Historia = new Models.hc_NUT_Recordatorio();
                    Historia.Desayuno_Hora = HistoriaClinica.Desayuno_Hora;
                    Historia.Desayuno_Preparacion = HistoriaClinica.Desayuno_Preparacion;
                    Historia.Desayuno_Alimento = HistoriaClinica.Desayuno_Alimento;
                    Historia.Desayuno_Cantidad = HistoriaClinica.Desayuno_Cantidad;
                    Historia.ColacionDes_Hora = HistoriaClinica.ColacionDes_Hora;
                    Historia.ColacionDes_Preparacion = HistoriaClinica.ColacionDes_Preparacion;
                    Historia.ColacionDes_Alimento = HistoriaClinica.ColacionDes_Alimento;
                    Historia.ColacionDes_Cantidad = HistoriaClinica.ColacionDes_Cantidad;
                    Historia.Comida_Hora = HistoriaClinica.Comida_Hora;
                    Historia.Comida_Preparacion = HistoriaClinica.Comida_Preparacion;
                    Historia.Comida_Alimento = HistoriaClinica.Comida_Alimento;
                    Historia.Comida_Cantidad = HistoriaClinica.Comida_Cantidad;
                    Historia.ColacionCom_Hora = HistoriaClinica.ColacionCom_Hora;
                    Historia.ColacionCom_Preparacion = HistoriaClinica.ColacionCom_Preparacion;
                    Historia.ColacionCom_Alimento = HistoriaClinica.ColacionCom_Alimento;
                    Historia.ColacionCom_Cantidad = HistoriaClinica.ColacionCom_Cantidad;
                    Historia.Cena_Hora = HistoriaClinica.Cena_Hora;
                    Historia.Cena_Preparacion = HistoriaClinica.Cena_Preparacion;
                    Historia.Cena_Alimento = HistoriaClinica.Cena_Alimento;
                    Historia.Cena_Cantidad = HistoriaClinica.Cena_Cantidad;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_Recordatorio.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class FrecuenciaAlimenPropiedades
        {
            public string Leche { get; set; }
            public string Frecuencia_Leche { get; set; }
            public string Verdura { get; set; }
            public string Frecuencia_Verdura { get; set; }
            public string Fruta { get; set; }
            public string Frecuencia_Fruta { get; set; }
            public string Cereales { get; set; }
            public string Frecuencia_Cereales { get; set; }
            public string Leguminosas { get; set; }
            public string Frecuencia_Leguminosas { get; set; }
            public string Carne { get; set; }
            public string Frecuencia_Carne { get; set; }
            public string Grasa { get; set; }
            public string Frecuencia_Grasa { get; set; }
            public string Azucar { get; set; }
            public string Frecuencia_Azucar { get; set; }
            public string BebidasAzucar { get; set; }
            public string Frecuencia_BebidasAzuc { get; set; }
            public string BebidasDiet { get; set; }
            public string Frecuencia_BebidasDiet { get; set; }
            public string BebidasAlt { get; set; }
            public string Frecuencia_BebidasAlt { get; set; }
            public string Cafe { get; set; }
            public string Frecuencia_Cafe { get; set; }
            public string Te { get; set; }
            public string Frecuencia_Te { get; set; }
            public string Cerveza { get; set; }
            public string Frecuencia_Cerveza { get; set; }
            public string ProductosPan { get; set; }
            public string Frecuencia_ProductosPan { get; set; }
            public string Confiteria { get; set; }
            public string Frecuencia_Confiteria { get; set; }
            public string Embutidos { get; set; }
            public string Frecuencia_Embutidos { get; set; }
            public string AlimentosEnla { get; set; }
            public string Frecuencia_AlimentosEnla { get; set; }
            public string Sopas { get; set; }
            public string Frecuencia_Sopas { get; set; }
            public string Verudras { get; set; }
            public string Frecuencia_Verudras { get; set; }
            public string ComidaRap { get; set; }
            public string Frecuencia_ComidaRap { get; set; }
            public string ComidasGras { get; set; }
            public string Frecuencia_ComidasGras { get; set; }
            public string ProductosChat { get; set; }
            public string Frecuencia_ProductosChat { get; set; }
            public string Consome { get; set; }
            public string Frecuencia_Consome { get; set; }
            public string Sal { get; set; }
            public string Frecuencia_Sal { get; set; }
            public string Sucedaneo { get; set; }
            public string Frecuencia_Sucedaneo { get; set; }
            public string Otros { get; set; }
            public string Frecuencia_Otros { get; set; }
        }

        [HttpPost]
        public ActionResult FrecuenciaAlimen(FrecuenciaAlimenPropiedades HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_FrecuenciaAlimen Historia = new Models.hc_NUT_FrecuenciaAlimen();
                    
                    if (HistoriaClinica.Leche == "on")
                    {
                        Historia.Leche = true;
                    }
                    else
                    {
                        Historia.Leche = false;
                    }
                    Historia.Frecuencia_Leche = HistoriaClinica.Frecuencia_Leche;

                    if (HistoriaClinica.Verdura == "on")
                    {
                        Historia.Verdura = true;
                    }
                    else
                    {
                        Historia.Verdura = false;
                    }
                    Historia.Frecuencia_Verdura = HistoriaClinica.Frecuencia_Verdura;

                    if (HistoriaClinica.Fruta == "on")
                    {
                        Historia.Fruta = true;
                    }
                    else
                    {
                        Historia.Fruta = false;
                    }
                    Historia.Frecuencia_Fruta = HistoriaClinica.Frecuencia_Fruta;

                    if (HistoriaClinica.Cereales == "on")
                    {
                        Historia.Cereales = true;
                    }
                    else
                    {
                        Historia.Cereales = false;
                    }
                    Historia.Frecuencia_Cereales = HistoriaClinica.Frecuencia_Cereales;

                    if (HistoriaClinica.Leguminosas == "on")
                    {
                        Historia.Leguminosas = true;
                    }
                    else
                    {
                        Historia.Leguminosas = false;
                    }
                    Historia.Frecuencia_Leguminosas = HistoriaClinica.Frecuencia_Leguminosas;

                    if (HistoriaClinica.Carne == "on")
                    {
                        Historia.Carne = true;
                    }
                    else
                    {
                        Historia.Carne = false;
                    }
                    Historia.Frecuencia_Carne = HistoriaClinica.Frecuencia_Carne;

                    if (HistoriaClinica.Grasa == "on")
                    {
                        Historia.Grasa = true;
                    }
                    else
                    {
                        Historia.Grasa = false;
                    }
                    Historia.Frecuencia_Grasa = HistoriaClinica.Frecuencia_Grasa;

                    if (HistoriaClinica.Azucar == "on")
                    {
                        Historia.Azucar = true;
                    }
                    else
                    {
                        Historia.Azucar = false;
                    }
                    Historia.Frecuencia_Azucar = HistoriaClinica.Frecuencia_Azucar;

                    if (HistoriaClinica.BebidasAzucar == "on")
                    {
                        Historia.BebidasAzucar = true;
                    }
                    else
                    {
                        Historia.BebidasAzucar = false;
                    }
                    Historia.Frecuencia_BebidasAzucar = HistoriaClinica.Frecuencia_BebidasAzuc;

                    if (HistoriaClinica.BebidasDiet == "on")
                    {
                        Historia.BebidasDiet = true;
                    }
                    else
                    {
                        Historia.BebidasDiet = false;
                    }
                    Historia.Frecuencia_BebidasDiet = HistoriaClinica.Frecuencia_BebidasDiet;

                    if (HistoriaClinica.BebidasAlt == "on")
                    {
                        Historia.BebidasAlt = true;
                    }
                    else
                    {
                        Historia.BebidasAlt = false;
                    }
                    Historia.Frecuencia_BebidasAlt = HistoriaClinica.Frecuencia_BebidasAlt;

                    if (HistoriaClinica.Cafe == "on")
                    {
                        Historia.Cafe = true;
                    }
                    else
                    {
                        Historia.Cafe = false;
                    }
                    Historia.Frecuencia_Cafe = HistoriaClinica.Frecuencia_Cafe;

                    if (HistoriaClinica.Te == "on")
                    {
                        Historia.Te = true;
                    }
                    else
                    {
                        Historia.Te = false;
                    }
                    Historia.Frecuencia_Te = HistoriaClinica.Frecuencia_Te;

                    if (HistoriaClinica.Cerveza == "on")
                    {
                        Historia.Cerveza = true;
                    }
                    else
                    {
                        Historia.Cerveza = false;
                    }
                    Historia.Frecuencia_Cerveza = HistoriaClinica.Frecuencia_Cerveza;

                    if (HistoriaClinica.ProductosPan == "on")
                    {
                        Historia.ProductosPan = true;
                    }
                    else
                    {
                        Historia.ProductosPan = false;
                    }
                    Historia.Frecuencia_ProductosPan = HistoriaClinica.Frecuencia_ProductosPan;

                    if (HistoriaClinica.Confiteria == "on")
                    {
                        Historia.Confiteria = true;
                    }
                    else
                    {
                        Historia.Confiteria = false;
                    }
                    Historia.Frecuencia_Confiteria = HistoriaClinica.Frecuencia_Confiteria;

                    if (HistoriaClinica.Embutidos == "on")
                    {
                        Historia.Embutidos = true;
                    }
                    else
                    {
                        Historia.Embutidos = false;
                    }
                    Historia.Frecuencia_Embutidos = HistoriaClinica.Frecuencia_Embutidos;

                    if (HistoriaClinica.AlimentosEnla == "on")
                    {
                        Historia.AlimentosEnla = true;
                    }
                    else
                    {
                        Historia.AlimentosEnla = false;
                    }
                    Historia.Frecuencia_AlimentosEnla = HistoriaClinica.Frecuencia_AlimentosEnla;

                    if (HistoriaClinica.Sopas == "on")
                    {
                        Historia.Sopas = true;
                    }
                    else
                    {
                        Historia.Sopas = false;
                    }
                    Historia.Frecuencia_Sopas = HistoriaClinica.Frecuencia_Sopas;

                    if (HistoriaClinica.Verudras == "on")
                    {
                        Historia.Verduras = true;
                    }
                    else
                    {
                        Historia.Verduras = false;
                    }
                    Historia.Frecuencia_Verduras = HistoriaClinica.Frecuencia_Verudras;

                    if (HistoriaClinica.ComidaRap == "on")
                    {
                        Historia.ComidaRap = true;
                    }
                    else
                    {
                        Historia.ComidaRap = false;
                    }
                    Historia.Frecuencia_ComidaRap = HistoriaClinica.Frecuencia_ComidaRap;

                    if (HistoriaClinica.ComidasGras == "on")
                    {
                        Historia.ComidaGras = true;
                    }
                    else
                    {
                        Historia.ComidaGras = false;
                    }
                    Historia.Frecuencia_ComidaGras = HistoriaClinica.Frecuencia_ComidasGras;

                    if (HistoriaClinica.ProductosChat == "on")
                    {
                        Historia.ProductosChat = true;
                    }
                    else
                    {
                        Historia.ProductosChat = false;
                    }
                    Historia.Frecuencia_ProductosChat = HistoriaClinica.Frecuencia_ProductosChat;

                    if (HistoriaClinica.Consome == "on")
                    {
                        Historia.Consome = true;
                    }
                    else
                    {
                        Historia.Consome = false;
                    }
                    Historia.Frecuencia_Consome = HistoriaClinica.Frecuencia_Consome;

                    if (HistoriaClinica.Sal == "on")
                    {
                        Historia.Sal = true;
                    }
                    else
                    {
                        Historia.Sal = false;
                    }
                    Historia.Frecuencia_Sal = HistoriaClinica.Frecuencia_Sal;

                    if (HistoriaClinica.Sucedaneo == "on")
                    {
                        Historia.Sucedaneo = true;
                    }
                    else
                    {
                        Historia.Sucedaneo = false;
                    }
                    Historia.Frecuencia_Sucedaneo = HistoriaClinica.Frecuencia_Sucedaneo;

                    if (HistoriaClinica.Otros == "on")
                    {
                        Historia.Otros = true;
                    }
                    else
                    {
                        Historia.Otros = false;
                    }
                    Historia.Frecuencia_Otros = HistoriaClinica.Frecuencia_Otros;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_FrecuenciaAlimen.Add(Historia);
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
        public ActionResult EvaluacionAntro(Models.hc_NUT_EvaluacionAntro HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_EvaluacionAntro Historia = new Models.hc_NUT_EvaluacionAntro();
                    Historia.S4Anios_PesoN = HistoriaClinica.S4Anios_PesoN;
                    Historia.S4Anios_TallaN = HistoriaClinica.S4Anios_TallaN;
                    Historia.S4Anios_PesoA = HistoriaClinica.S4Anios_PesoA;
                    Historia.S4Anios_TallaA = HistoriaClinica.S4Anios_TallaA;
                    Historia.S4Tabla_MedPeso = HistoriaClinica.S4Tabla_MedPeso;
                    Historia.S4Tabla_MedTalla = HistoriaClinica.S4Tabla_MedTalla;
                    Historia.S4Tabla_MedPeTa = HistoriaClinica.S4Tabla_MedPeTa;
                    Historia.S4Tabla_MedPeId = HistoriaClinica.S4Tabla_MedPeId;
                    Historia.S4Tabla_DesPeso = HistoriaClinica.S4Tabla_DesPeso;
                    Historia.S4Tabla_DesTalla = HistoriaClinica.S4Tabla_DesTalla;
                    Historia.S4Tabla_DesPeTa = HistoriaClinica.S4Tabla_DesPeTa;
                    Historia.S4Tabla_DesPeId = HistoriaClinica.S4Tabla_DesPeId;
                    Historia.S4Tabla_RanPeso = HistoriaClinica.S4Tabla_RanPeso;
                    Historia.S4Tabla_RanTalla = HistoriaClinica.S4Tabla_RanTalla;
                    Historia.S4Tabla_RanPeTa = HistoriaClinica.S4Tabla_RanPeTa;
                    Historia.S4Tabla_RanPeId = HistoriaClinica.S4Tabla_RanPeId;
                    Historia.S4Tabla_ClaPeso = HistoriaClinica.S4Tabla_ClaPeso;
                    Historia.S4Tabla_ClaTalla = HistoriaClinica.S4Tabla_ClaTalla;
                    Historia.S4Tabla_ClaPeTa = HistoriaClinica.S4Tabla_ClaPeTa;
                    Historia.S4Tabla_ClaPeId = HistoriaClinica.S4Tabla_ClaPeId;
                    Historia.S4Anios_Interpretacion = HistoriaClinica.S4Anios_Interpretacion;
                    Historia.S4Anios_Observaciones = HistoriaClinica.S4Anios_Observaciones;
                    Historia.S5Anios_PesoA = HistoriaClinica.S5Anios_PesoA;
                    Historia.S5Anios_TallaA = HistoriaClinica.S5Anios_TallaA;
                    Historia.S5Anios_Indice = HistoriaClinica.S5Anios_Indice;
                    Historia.S5Anios_Desviacion = HistoriaClinica.S5Anios_Desviacion;
                    Historia.S5Anios_Diagnostico = HistoriaClinica.S5Anios_Diagnostico;
                    Historia.S5Anios_PesoId = HistoriaClinica.S5Anios_PesoId;
                    Historia.S5Anios_Circunf = HistoriaClinica.S5Anios_Circunf;
                    Historia.S5Anios_Percentil = HistoriaClinica.S5Anios_Percentil;
                    Historia.S5Anios_Interpretacion = HistoriaClinica.S5Anios_Interpretacion;
                    Historia.S5Anios_Observaciones = HistoriaClinica.S5Anios_Observaciones;
                    Historia.S10Anios_PesoA = HistoriaClinica.S10Anios_PesoA;
                    Historia.S10Anios_TallaA = HistoriaClinica.S10Anios_TallaA;
                    Historia.S10Anios_Indice = HistoriaClinica.S10Anios_Indice;
                    Historia.S10Anios_Desviacion = HistoriaClinica.S10Anios_Desviacion;
                    Historia.S10Anios_Diagnostico = HistoriaClinica.S10Anios_Diagnostico;
                    Historia.S10Anios_PesoId = HistoriaClinica.S10Anios_PesoId;
                    Historia.S10Anios_Circunf = HistoriaClinica.S10Anios_Circunf;
                    Historia.S10Anios_Percentil = HistoriaClinica.S10Anios_Percentil;
                    Historia.S10Anios_Interpretacion = HistoriaClinica.S10Anios_Interpretacion;
                    Historia.S10Anios_Observaciones = HistoriaClinica.S10Anios_Observaciones;
                    Historia.S20Anios_PesoA = HistoriaClinica.S20Anios_PesoA;
                    Historia.S20Anios_TallaA = HistoriaClinica.S20Anios_TallaA;
                    Historia.S20Anios_Indice = HistoriaClinica.S20Anios_Indice;
                    Historia.S20Anios_Diagnostico = HistoriaClinica.S20Anios_Diagnostico;
                    Historia.S20Anios_CircunfMu = HistoriaClinica.S20Anios_CircunfMu;
                    Historia.S20Anios_Complexion = HistoriaClinica.S20Anios_Complexion;
                    Historia.S20Anios_PesoId = HistoriaClinica.S20Anios_PesoId;
                    Historia.S20Anios_CircunfCin = HistoriaClinica.S20Anios_CircunfCin;
                    Historia.S20Anios_CircunfCad = HistoriaClinica.S20Anios_CircunfCad;
                    Historia.S20Anios_IndiceCin = HistoriaClinica.S20Anios_IndiceCin;
                    Historia.S20Anios_Riesgo = HistoriaClinica.S20Anios_Riesgo;
                    Historia.S20Anios_Observaciones = HistoriaClinica.S20Anios_Observaciones;
                    Historia.S60Anios_PesoA = HistoriaClinica.S60Anios_PesoA;
                    Historia.S60Anios_TallaA = HistoriaClinica.S60Anios_TallaA;
                    Historia.S60Anios_Indice = HistoriaClinica.S60Anios_Indice;
                    Historia.S60Anios_Diagnostico = HistoriaClinica.S60Anios_Diagnostico;
                    Historia.S60Anios_CircunfMu = HistoriaClinica.S60Anios_CircunfMu;
                    Historia.S60Anios_Complexion = HistoriaClinica.S60Anios_Complexion;
                    Historia.S60Anios_PesoId = HistoriaClinica.S60Anios_PesoId;
                    Historia.S60Anios_PesoAj = HistoriaClinica.S60Anios_PesoAj;
                    Historia.S60Anios_CircunfCin = HistoriaClinica.S60Anios_CircunfCin;
                    Historia.S60Anios_CircunfCad = HistoriaClinica.S60Anios_CircunfCad;
                    Historia.S60Anios_IndiceCin = HistoriaClinica.S60Anios_IndiceCin;
                    Historia.S60Anios_CircunfBra = HistoriaClinica.S60Anios_CircunfBra;
                    Historia.S60Anios_CircunfPan = HistoriaClinica.S60Anios_CircunfPan;
                    Historia.S60Anios_Observaciones = HistoriaClinica.S60Anios_Observaciones;
                    Historia.SEmbaraz_PesoA = HistoriaClinica.SEmbaraz_PesoA;
                    Historia.SEmbaraz_TallaA = HistoriaClinica.SEmbaraz_TallaA;
                    Historia.SEmbaraz_Gestacion = HistoriaClinica.SEmbaraz_Gestacion;
                    Historia.SEmbaraz_CircunfBra = HistoriaClinica.SEmbaraz_CircunfBra;
                    Historia.SEmbaraz_PesoPreges = HistoriaClinica.SEmbaraz_PesoPreges;
                    Historia.SEmbaraz_Indice = HistoriaClinica.SEmbaraz_Indice;
                    Historia.SEmbaraz_Diagnostico = HistoriaClinica.SEmbaraz_Diagnostico;
                    Historia.SEmbaraz_GanPesoDesd = HistoriaClinica.SEmbaraz_GanPesoDesd;
                    Historia.SEmbaraz_GanPesoAct = HistoriaClinica.SEmbaraz_GanPesoAct;
                    Historia.SEmbaraz_Observaciones = HistoriaClinica.SEmbaraz_Observaciones;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_EvaluacionAntro.Add(Historia);
                    hcNut.SaveChanges();
                }
                return Json(new { MENSAJE = "Succe: " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class EducacionInPropiedades
        {
            public string CriteriosGen { get; set; }
            public string PrevencionEnf { get; set; }
            public string EducacionMat { get; set; }
            public string PlanificacionDis { get; set; }
            public string AdopcionPat { get; set; }
            public string EducacionMej { get; set; }
            public string EjercicioAd { get; set; }
            public string EducacionRel { get; set; }
            public string AdherenciaTrat { get; set; }
            public string EdRelAlimentos { get; set; }
            public string DudasInq { get; set; }
            public string Especifica_Dudas { get; set; }
            public string Oral { get; set; }
            public string Escrito { get; set; }
            public string Video { get; set; }
            public string Demostracion { get; set; }
            public string MujerEmb { get; set; }
            public string MadreEt { get; set; }
            public string Postnatal { get; set; }
            public string Cuidador6meses { get; set; }
            public string Cuidador6a12meses { get; set; }
            public string Cuidador1a4anios { get; set; }
            public string Cuidador5a9anios { get; set; }
            public string C10a19anios { get; set; }
            public string C20a59anios { get; set; }
            public string C60anios { get; set; }
            public string SeCompleta { get; set; }
            public string SinCompletar { get; set; }
            public string ReforzarParte { get; set; }
            public string ReforzarToda { get; set; }
            public string Rechazo { get; set; }
        }

        [HttpPost]
        public ActionResult EducacionIn(EducacionInPropiedades HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_EducacionInicial Historia = new Models.hc_NUT_EducacionInicial();
                    if (HistoriaClinica.CriteriosGen == "on")
                    {
                        Historia.CriteriosGen = true;
                    }
                    else
                    {
                        Historia.CriteriosGen = false;
                    }
                    if (HistoriaClinica.PrevencionEnf == "on")
                    {
                        Historia.PrevencionEnf = true;
                    }
                    else
                    {
                        Historia.PrevencionEnf = false;
                    }
                    if (HistoriaClinica.EducacionMat == "on")
                    {
                        Historia.EducacionMat = true;
                    }
                    else
                    {
                        Historia.EducacionMat = false;
                    }
                    if (HistoriaClinica.PlanificacionDis == "on")
                    {
                        Historia.PlanificacionDis = true;
                    }
                    else
                    {
                        Historia.PlanificacionDis = false;
                    }
                    if (HistoriaClinica.AdopcionPat == "on")
                    {
                        Historia.AdopcionPat = true;
                    }
                    else
                    {
                        Historia.AdopcionPat = false;
                    }
                    if (HistoriaClinica.EducacionMej == "on")
                    {
                        Historia.EducacionMej = true;
                    }
                    else
                    {
                        Historia.EducacionMej = false;
                    }
                    if (HistoriaClinica.EjercicioAd == "on")
                    {
                        Historia.EjercicioAd = true;
                    }
                    else
                    {
                        Historia.EjercicioAd = false;
                    }
                    if (HistoriaClinica.EducacionRel == "on")
                    {
                        Historia.EducacionRel = true;
                    }
                    else
                    {
                        Historia.EducacionRel = false;
                    }
                    if (HistoriaClinica.AdherenciaTrat == "on")
                    {
                        Historia.AdherenciaTrat = true;
                    }
                    else
                    {
                        Historia.AdherenciaTrat = false;
                    }
                    if (HistoriaClinica.EdRelAlimentos == "on")
                    {
                        Historia.EdRelAlimentos = true;
                    }
                    else
                    {
                        Historia.EdRelAlimentos = false;
                    }
                    if (HistoriaClinica.DudasInq == "on")
                    {
                        Historia.DudasInq = true;
                    }
                    else
                    {
                        Historia.DudasInq = false;
                    }
                    Historia.Especifica_Dudas = HistoriaClinica.Especifica_Dudas;
                    if (HistoriaClinica.Oral == "on")
                    {
                        Historia.Oral = true;
                    }
                    else
                    {
                        Historia.Oral = false;
                    }
                    if (HistoriaClinica.Escrito == "on")
                    {
                        Historia.Escrito = true;
                    }
                    else
                    {
                        Historia.Escrito = false;
                    }
                    if (HistoriaClinica.Video == "on")
                    {
                        Historia.Video = true;
                    }
                    else
                    {
                        Historia.Video = false;
                    }
                    if (HistoriaClinica.Demostracion == "on")
                    {
                        Historia.Demostracion = true;
                    }
                    else
                    {
                        Historia.Demostracion = false;
                    }
                    if (HistoriaClinica.MujerEmb == "on")
                    {
                        Historia.Mujeremb = true;
                    }
                    else
                    {
                        Historia.Mujeremb = false;
                    }
                    if (HistoriaClinica.MadreEt == "on")
                    {
                        Historia.MadreEt = true;
                    }
                    else
                    {
                        Historia.MadreEt = false;
                    }
                    if (HistoriaClinica.Postnatal == "on")
                    {
                        Historia.Postnatal = true;
                    }
                    else
                    {
                        Historia.Postnatal = false;
                    }
                    if (HistoriaClinica.Cuidador6meses == "on")
                    {
                        Historia.Cuidador6meses = true;
                    }
                    else
                    {
                        Historia.Cuidador6meses = false;
                    }
                    if (HistoriaClinica.Cuidador6a12meses == "on")
                    {
                        Historia.Cuidador6a12meses = true;
                    }
                    else
                    {
                        Historia.Cuidador6a12meses = false;
                    }
                    if (HistoriaClinica.Cuidador1a4anios == "on")
                    {
                        Historia.Cuidador1a4anios = true;
                    }
                    else
                    {
                        Historia.Cuidador1a4anios = false;
                    }
                    if (HistoriaClinica.Cuidador5a9anios == "on")
                    {
                        Historia.Cuidador5a9anios = true;
                    }
                    else
                    {
                        Historia.Cuidador5a9anios = false;
                    }
                    if (HistoriaClinica.C10a19anios == "on")
                    {
                        Historia.C10a19anios = true;
                    }
                    else
                    {
                        Historia.C10a19anios = false;
                    }
                    if (HistoriaClinica.C20a59anios == "on")
                    {
                        Historia.C20a59anios = true;
                    }
                    else
                    {
                        Historia.C20a59anios = false;
                    }
                    if (HistoriaClinica.C60anios == "on")
                    {
                        Historia.C60anios = true;
                    }
                    else
                    {
                        Historia.C60anios = false;
                    }
                    if (HistoriaClinica.SeCompleta == "on")
                    {
                        Historia.SeCompleta = true;
                    }
                    else
                    {
                        Historia.SeCompleta = false;
                    }
                    if (HistoriaClinica.SinCompletar == "on")
                    {
                        Historia.SinCompletar = true;
                    }
                    else
                    {
                        Historia.SinCompletar = false;
                    }
                    if (HistoriaClinica.ReforzarParte == "on")
                    {
                        Historia.ReforzarParte = true;
                    }
                    else
                    {
                        Historia.ReforzarParte = false;
                    }
                    if (HistoriaClinica.ReforzarToda == "on")
                    {
                        Historia.ReforzarToda = true;
                    }
                    else
                    {
                        Historia.ReforzarToda = false;
                    }
                    if (HistoriaClinica.Rechazo == "on")
                    {
                        Historia.Rechazo = true;
                    }
                    else
                    {
                        Historia.Rechazo = false;
                    }

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_EducacionInicial.Add(Historia);
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
        public ActionResult ImpresionDiag(Models.hc_NUT_ImpresionDiag HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_ImpresionDiag Historia = new Models.hc_NUT_ImpresionDiag();
                    Historia.diagnostico1 = HistoriaClinica.diagnostico1;
                    Historia.diagnostico2 = HistoriaClinica.diagnostico2;
                    Historia.diagnostico3 = HistoriaClinica.diagnostico3;
                    Historia.diagnostico4 = HistoriaClinica.diagnostico4;
                    Historia.diagnostico5 = HistoriaClinica.diagnostico5;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_ImpresionDiag.Add(Historia);
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
        public ActionResult Plan(Models.hc_NUT_Plan HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_Plan Historia = new Models.hc_NUT_Plan();
                    Historia.Plan = HistoriaClinica.Plan;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_Plan.Add(Historia);
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_Pronostico Historia = new Models.hc_NUT_Pronostico();
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
                    hcNut.hc_NUT_Pronostico.Add(Historia);
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
        public ActionResult Otros(Models.hc_NUT_Otros HistoriaClinica, string expediente)
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
                        .Any(r => r.Id_Paciente == paciente.Id && r.FechaRegistroHC >= fechaL && r.FechaRegistroHC <= fechaActual && r.TipoHistoria == "Nutrición");
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
                    Models.hc_NUT_Otros Historia = new Models.hc_NUT_Otros();
                    Historia.Interconsulta = HistoriaClinica.Interconsulta;
                    Historia.PadecimientoActual = HistoriaClinica.PadecimientoActual;
                    Historia.Especifica_PadecimientoActual = HistoriaClinica.Especifica_PadecimientoActual;
                    Historia.ProximaCita = HistoriaClinica.ProximaCita;

                    Historia.Id_Paciente = paciente.Id;
                    Historia.Clave_hc_px = Id_claveHC;
                    hcNut.hc_NUT_Otros.Add(Historia);
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
            //ResultadoLab	
            public string Resultado { get; set; }
            public string Especifica_Resultado { get; set; }
            //EtapaCambio	
            public string Viene { get; set; }
            public string DeseaLograr { get; set; }
            public bool? Precontemplacion { get; set; }
            public bool? Contemplacion { get; set; }
            public bool? Preparacion { get; set; }
            public bool? Mantenimiento { get; set; }
            public bool? Terminacion { get; set; }
            //DatosDieteticos	
            public string Comidas { get; set; }
            public bool? Desayuno { get; set; }
            public bool? Comidas2 { get; set; }
            public bool? Cena { get; set; }
            public bool? ColacionMat { get; set; }
            public bool? ColacionVes { get; set; }
            public string TiempoConsumir { get; set; }
            public string Suplementos { get; set; }
            public string Especifica_Supl { get; set; }
            public string AlimentosDisgustan { get; set; }
            public string AlimentosFavoritos { get; set; }
            public string ConsumoAgua { get; set; }
            public string Especifica_ConsumoAgua { get; set; }
            public string HorasSuenio { get; set; }
            public string ActividadFisica { get; set; }
            public string Tipo_ActividadFisica { get; set; }
            public string Tiempo_ActividadFisica { get; set; }
            public string Frec_ActividadFisica { get; set; }
            //AlimentacionInicial	
            public string LactanciaMaterna { get; set; }
            public string Tiempo_LactanciaM { get; set; }
            public string Sucedaneo { get; set; }
            public string Cual_Sucedaneo { get; set; }
            public string AlimentacionComp { get; set; }
            public string Edad_AlimentacionC { get; set; }
            public string Motivo_AlimentacionC { get; set; }
            //Recordatorio	
            public string Desayuno_Hora { get; set; }
            public string Desayuno_Preparacion { get; set; }
            public string Desayuno_Alimento { get; set; }
            public string Desayuno_Cantidad { get; set; }
            public string ColacionDes_Hora { get; set; }
            public string ColacionDes_Preparacion { get; set; }
            public string ColacionDes_Alimento { get; set; }
            public string ColacionDes_Cantidad { get; set; }
            public string Comida_Hora { get; set; }
            public string Comida_Preparacion { get; set; }
            public string Comida_Alimento { get; set; }
            public string Comida_Cantidad { get; set; }
            public string ColacionCom_Hora { get; set; }
            public string ColacionCom_Preparacion { get; set; }
            public string ColacionCom_Alimento { get; set; }
            public string ColacionCom_Cantidad { get; set; }
            public string Cena_Hora { get; set; }
            public string Cena_Preparacion { get; set; }
            public string Cena_Alimento { get; set; }
            public string Cena_Cantidad { get; set; }
            //FrecuenciaAlimen	
            public bool? Leche { get; set; }
            public string Frecuencia_Leche { get; set; }
            public bool? Verdura { get; set; }
            public string Frecuencia_Verdura { get; set; }
            public bool? Fruta { get; set; }
            public string Frecuencia_Fruta { get; set; }
            public bool? Cereales { get; set; }
            public string Frecuencia_Cereales { get; set; }
            public bool? Leguminosas { get; set; }
            public string Frecuencia_Leguminosas { get; set; }
            public bool? Carne { get; set; }
            public string Frecuencia_Carne { get; set; }
            public bool? Grasa { get; set; }
            public string Frecuencia_Grasa { get; set; }
            public bool? Azucar { get; set; }
            public string Frecuencia_Azucar { get; set; }
            public bool? BebidasAzucar { get; set; }
            public string Frecuencia_BebidasAzucar { get; set; }
            public bool? BebidasDiet { get; set; }
            public string Frecuencia_BebidasDiet { get; set; }
            public bool? BebidasAlt { get; set; }
            public string Frecuencia_BebidasAlt { get; set; }
            public bool? Cafe { get; set; }
            public string Frecuencia_Cafe { get; set; }
            public bool? Te { get; set; }
            public string Frecuencia_Te { get; set; }
            public bool? Cerveza { get; set; }
            public string Frecuencia_Cerveza { get; set; }
            public bool? ProductosPan { get; set; }
            public string Frecuencia_ProductosPan { get; set; }
            public bool? Confiteria { get; set; }
            public string Frecuencia_Confiteria { get; set; }
            public bool? Embutidos { get; set; }
            public string Frecuencia_Embutidos { get; set; }
            public bool? AlimentosEnla { get; set; }
            public string Frecuencia_AlimentosEnla { get; set; }
            public bool? Sopas { get; set; }
            public string Frecuencia_Sopas { get; set; }
            public bool? Verduras { get; set; }
            public string Frecuencia_Verduras { get; set; }
            public bool? ComidaRap { get; set; }
            public string Frecuencia_ComidaRap { get; set; }
            public bool? ComidaGras { get; set; }
            public string Frecuencia_ComidaGras { get; set; }
            public bool? ProductosChat { get; set; }
            public string Frecuencia_ProductosChat { get; set; }
            public bool? Consome { get; set; }
            public string Frecuencia_Consome { get; set; }
            public bool? Sal { get; set; }
            public string Frecuencia_Sal { get; set; }
            public bool? sucedaneo { get; set; }
            public string Frecuencia_Sucedaneo { get; set; }
            public bool? Otros { get; set; }
            public string Frecuencia_Otros { get; set; }
            //EvaluacionAntro	
            public string S4Anios_PesoN { get; set; }
            public string S4Anios_TallaN { get; set; }
            public string S4Anios_PesoA { get; set; }
            public string S4Anios_TallaA { get; set; }
            public string S4Tabla_MedPeso { get; set; }
            public string S4Tabla_MedTalla { get; set; }
            public string S4Tabla_MedPeTa { get; set; }
            public string S4Tabla_MedPeId { get; set; }
            public string S4Tabla_DesPeso { get; set; }
            public string S4Tabla_DesTalla { get; set; }
            public string S4Tabla_DesPeTa { get; set; }
            public string S4Tabla_DesPeId { get; set; }
            public string S4Tabla_RanPeso { get; set; }
            public string S4Tabla_RanTalla { get; set; }
            public string S4Tabla_RanPeTa { get; set; }
            public string S4Tabla_RanPeId { get; set; }
            public string S4Tabla_ClaPeso { get; set; }
            public string S4Tabla_ClaTalla { get; set; }
            public string S4Tabla_ClaPeTa { get; set; }
            public string S4Tabla_ClaPeId { get; set; }
            public string S4Anios_Interpretacion { get; set; }
            public string S4Anios_Observaciones { get; set; }
            public string S5Anios_PesoA { get; set; }
            public string S5Anios_TallaA { get; set; }
            public string S5Anios_Indice { get; set; }
            public string S5Anios_Desviacion { get; set; }
            public string S5Anios_Diagnostico { get; set; }
            public string S5Anios_PesoId { get; set; }
            public string S5Anios_Circunf { get; set; }
            public string S5Anios_Percentil { get; set; }
            public string S5Anios_Interpretacion { get; set; }
            public string S5Anios_Observaciones { get; set; }
            public string S10Anios_PesoA { get; set; }
            public string S10Anios_TallaA { get; set; }
            public string S10Anios_Indice { get; set; }
            public string S10Anios_Desviacion { get; set; }
            public string S10Anios_Diagnostico { get; set; }
            public string S10Anios_PesoId { get; set; }
            public string S10Anios_Circunf { get; set; }
            public string S10Anios_Percentil { get; set; }
            public string S10Anios_Interpretacion { get; set; }
            public string S10Anios_Observaciones { get; set; }
            public string S20Anios_PesoA { get; set; }
            public string S20Anios_TallaA { get; set; }
            public string S20Anios_Indice { get; set; }
            public string S20Anios_Diagnostico { get; set; }
            public string S20Anios_CircunfMu { get; set; }
            public string S20Anios_Complexion { get; set; }
            public string S20Anios_PesoId { get; set; }
            public string S20Anios_CircunfCin { get; set; }
            public string S20Anios_CircunfCad { get; set; }
            public string S20Anios_IndiceCin { get; set; }
            public string S20Anios_Riesgo { get; set; }
            public string S20Anios_Observaciones { get; set; }
            public string S60Anios_PesoA { get; set; }
            public string S60Anios_TallaA { get; set; }
            public string S60Anios_Indice { get; set; }
            public string S60Anios_Diagnostico { get; set; }
            public string S60Anios_CircunfMu { get; set; }
            public string S60Anios_Complexion { get; set; }
            public string S60Anios_PesoId { get; set; }
            public string S60Anios_PesoAj { get; set; }
            public string S60Anios_CircunfCin { get; set; }
            public string S60Anios_CircunfCad { get; set; }
            public string S60Anios_IndiceCin { get; set; }
            public string S60Anios_CircunfBra { get; set; }
            public string S60Anios_CircunfPan { get; set; }
            public string S60Anios_Observaciones { get; set; }
            public string SEmbaraz_PesoA { get; set; }
            public string SEmbaraz_TallaA { get; set; }
            public string SEmbaraz_Gestacion { get; set; }
            public string SEmbaraz_CircunfBra { get; set; }
            public string SEmbaraz_PesoPreges { get; set; }
            public string SEmbaraz_Indice { get; set; }
            public string SEmbaraz_Diagnostico { get; set; }
            public string SEmbaraz_GanPesoDesd { get; set; }
            public string SEmbaraz_GanPesoAct { get; set; }
            public string SEmbaraz_Observaciones { get; set; }
            //EducacionInicial	
            public bool? CriteriosGen { get; set; }
            public bool? PrevencionEnf { get; set; }
            public bool? EducacionMat { get; set; }
            public bool? PlanificacionDis { get; set; }
            public bool? AdopcionPat { get; set; }
            public bool? EducacionMej { get; set; }
            public bool? EjercicioAd { get; set; }
            public bool? EducacionRel { get; set; }
            public bool? EdRelAlimentos { get; set; }
            public bool? AdherenciaTrat { get; set; }
            public bool? DudasInq { get; set; }
            public string Especifica_Dudas { get; set; }
            public bool? Oral { get; set; }
            public bool? Escrito { get; set; }
            public bool? Video { get; set; }
            public bool? Demostracion { get; set; }
            public bool? Mujeremb { get; set; }
            public bool? MadreEt { get; set; }
            public bool? Postnatal { get; set; }
            public bool? Cuidador6meses { get; set; }
            public bool? Cuidador6a12meses { get; set; }
            public bool? Cuidador1a4anios { get; set; }
            public bool? Cuidador5a9anios { get; set; }
            public bool? C10a19anios { get; set; }
            public bool? C20a59anios { get; set; }
            public bool? C60anios { get; set; }
            public bool? SeCompleta { get; set; }
            public bool? SinCompletar { get; set; }
            public bool? ReforzarParte { get; set; }
            public bool? ReforzarToda { get; set; }
            public bool? Rechazo { get; set; }
            //ImpresionDiag
            public string Diagnostico1 { get; set; }
            public string Diagnostico2 { get; set; }
            public string Diagnostico3 { get; set; }
            public string Diagnostico4 { get; set; }
            public string Diagnostico5 { get; set; }
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
        public ActionResult ConsultarHC_Nut(string Clave_hc_px, string TipoHistoria)//Este parametro lo recivimos de la vista, "Clave_hc_px" viene siendo el Identificador armado de la HC que se desea ver
        {
            try
            {
                Propiedades_HC HC = new Propiedades_HC();

                string query =
                    "SELECT RL.Resultado, RL.Especifica_Resultado, " +
                    "EC.Viene, EC.DeseaLograr, EC.Precontemplacion, EC.Contemplacion, EC.Preparacion, EC.Mantenimiento, EC.Terminacion, " +
                    "DD.Comidas, DD.Desayuno, DD.Comidas2, DD.Cena, DD.ColacionMat, DD.ColacionVes, DD.TiempoConsumir, DD.Suplementos, DD.Especifica_Supl, DD.AlimentosDisgustan, DD.AlimentosFavoritos, DD.ConsumoAgua, DD.Especifica_ConsumoAgua, DD.HorasSuenio, DD.ActividadFisica, DD.Tipo_ActividadFisica, DD.Tiempo_ActividadFisica, DD.Frec_ActividadFisica, " +
                    "AI.LactanciaMaterna, AI.Tiempo_LactanciaM, AI.Sucedaneo, AI.Cual_Sucedaneo, AI.AlimentacionComp, AI.Edad_AlimentacionC, AI.Motivo_AlimentacionC, " +
                    "R.Desayuno_Hora, R.Desayuno_Preparacion, R.Desayuno_Alimento, R.Desayuno_Cantidad, R.ColacionDes_Hora, R.ColacionDes_Preparacion, R.ColacionDes_Alimento, R.ColacionDes_Cantidad, R.Comida_Hora, R.Comida_Preparacion, R.Comida_Alimento, R.Comida_Cantidad, R.ColacionCom_Hora, R.ColacionCom_Preparacion, R.ColacionCom_Alimento, R.ColacionCom_Cantidad, R.Cena_Hora, R.Cena_Preparacion, R.Cena_Alimento, R.Cena_Cantidad, " +
                    "FA.Leche, FA.Frecuencia_Leche, FA.Verdura, FA.Frecuencia_Verdura, FA.Fruta, FA.Frecuencia_Fruta, FA.Cereales, FA.Frecuencia_Cereales, FA.Leguminosas, FA.Frecuencia_Leguminosas, FA.Carne, FA.Frecuencia_Carne, FA.Grasa, FA.Frecuencia_Grasa, FA.Azucar, FA.Frecuencia_Azucar, FA.BebidasAzucar, FA.Frecuencia_BebidasAzucar, FA.BebidasDiet, FA.Frecuencia_BebidasDiet, FA.BebidasAlt, FA.Frecuencia_BebidasAlt, FA.Cafe, FA.Frecuencia_Cafe, FA.Te, " +
                    "FA.Frecuencia_Te, FA.Cerveza, FA.Frecuencia_Cerveza, FA.ProductosPan, FA.Frecuencia_ProductosPan, FA.Confiteria, FA.Frecuencia_Confiteria, FA.Embutidos, FA.Frecuencia_Embutidos, FA.AlimentosEnla, FA.Frecuencia_AlimentosEnla, FA.Sopas, FA.Frecuencia_Sopas, FA.Verduras, FA.Frecuencia_Verduras, FA.ComidaRap, FA.Frecuencia_ComidaRap, FA.ComidaGras, FA.Frecuencia_ComidaGras, FA.ProductosChat, FA.Frecuencia_ProductosChat, FA.Consome, FA.Frecuencia_Consome, FA.Sal, " +
                    "FA.Frecuencia_Sal, FA.sucedaneo, FA.Frecuencia_Sucedaneo, FA.Otros, FA.Frecuencia_Otros, " +
                    "EA.S4Anios_PesoN, EA.S4Anios_TallaN, EA.S4Anios_PesoA, EA.S4Anios_TallaA, EA.S4Tabla_MedPeso, EA.S4Tabla_MedTalla, EA.S4Tabla_MedPeTa, EA.S4Tabla_MedPeId, EA.S4Tabla_DesPeso, EA.S4Tabla_DesTalla, EA.S4Tabla_DesPeTa, EA.S4Tabla_DesPeId, EA.S4Tabla_RanPeso, EA.S4Tabla_RanTalla, EA.S4Tabla_RanPeTa, EA.S4Tabla_RanPeId, EA.S4Tabla_ClaPeso, EA.S4Tabla_ClaTalla, EA.S4Tabla_ClaPeTa, EA.S4Tabla_ClaPeId, EA.S4Anios_Interpretacion, EA.S4Anios_Observaciones, EA.S5Anios_PesoA, EA.S5Anios_TallaA, " +
                    "EA.S5Anios_Indice, EA.S5Anios_Desviacion, EA.S5Anios_Diagnostico, EA.S5Anios_PesoId, EA.S5Anios_Circunf, EA.S5Anios_Percentil, EA.S5Anios_Interpretacion, EA.S5Anios_Observaciones, EA.S10Anios_PesoA, EA.S10Anios_TallaA, EA.S10Anios_Indice, EA.S10Anios_Desviacion, EA.S10Anios_Diagnostico, EA.S10Anios_PesoId, EA.S10Anios_Circunf, EA.S10Anios_Percentil, EA.S10Anios_Interpretacion, EA.S10Anios_Observaciones, EA.S20Anios_PesoA, EA.S20Anios_TallaA, EA.S20Anios_Indice, EA.S20Anios_Diagnostico, EA.S20Anios_CircunfMu, " +
                    "EA.S20Anios_Complexion, EA.S20Anios_PesoId, EA.S20Anios_CircunfCin, EA.S20Anios_CircunfCad, EA.S20Anios_IndiceCin, EA.S20Anios_Riesgo, EA.S20Anios_Observaciones, EA.S60Anios_PesoA, EA.S60Anios_TallaA, EA.S60Anios_Indice, EA.S60Anios_Diagnostico, EA.S60Anios_CircunfMu, EA.S60Anios_Complexion, EA.S60Anios_PesoId, EA.S60Anios_PesoAj, EA.S60Anios_CircunfCin, EA.S60Anios_CircunfCad, EA.S60Anios_IndiceCin, EA.S60Anios_CircunfBra, EA.S60Anios_CircunfPan, EA.S60Anios_Observaciones, EA.SEmbaraz_PesoA, EA.SEmbaraz_TallaA, EA.SEmbaraz_Gestacion, " +
                    "EA.SEmbaraz_CircunfBra, EA.SEmbaraz_PesoPreges, EA.SEmbaraz_Indice, EA.SEmbaraz_Diagnostico, EA.SEmbaraz_GanPesoDesd, EA.SEmbaraz_GanPesoAct, EA.SEmbaraz_Observaciones, " +
                    "EI.CriteriosGen, EI.PrevencionEnf, EI.EducacionMat, EI.PlanificacionDis, EI.AdopcionPat, EI.EducacionMej, EI.EjercicioAd, EI.EducacionRel, EI.EdRelAlimentos, EI.AdherenciaTrat, EI.DudasInq, EI.Especifica_Dudas, EI.Oral, EI.Escrito, EI.Video, EI.Demostracion, EI.Mujeremb, EI.MadreEt, EI.Postnatal, EI.Cuidador6meses, EI.Cuidador6a12meses, EI.Cuidador1a4anios, EI.Cuidador5a9anios, EI.C10a19anios, EI.C20a59anios, EI.C60anios, EI.SeCompleta, EI.SinCompletar, EI.ReforzarParte, EI.ReforzarToda, EI.Rechazo, " +
                    "ID.Diagnostico1, ID.Diagnostico2, ID.Diagnostico3, ID.Diagnostico4, ID.Diagnostico5, " +
                    "PL.[Plan], " +
                    "PR.LigadoEvolucion, PR.Favorable, PR.Desfavorable, " +
                    "O.Interconsulta, O.PadecimientoActual, O.Especifica_PadecimientoActual, O.ProximaCita " +
                                    "FROM HistoriaClinica HCli " +
                                    "LEFT JOIN hc_NUT_ResultadoLab RL ON RL.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_EtapaCambio EC ON EC.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_DatosDieteticos DD ON DD.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_AlimentacionInicial AI ON AI.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_Recordatorio R ON R.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_FrecuenciaAlimen FA ON FA.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_EvaluacionAntro EA ON EA.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_EducacionInicial EI ON EI.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_ImpresionDiag ID ON ID.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_Plan PL ON PL.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_Pronostico PR ON PR.Clave_hc_px = HCli.Clave_hc_px " +
                                    "LEFT JOIN hc_NUT_Otros O ON O.Clave_hc_px = HCli.Clave_hc_px " +
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
