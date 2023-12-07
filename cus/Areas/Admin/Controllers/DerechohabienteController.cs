using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CUS.Models;

namespace CUS.Areas.Admin.Controllers
{
    public class DerechohabienteController : Controller
    {
        private Models.CUS db = new Models.CUS();
        Models.HC_Medicina hcMed = new Models.HC_Medicina();

        public class ItemViewModel
        {
            public Paciente SingleItem { get; set; }
            public List<Paciente> ItemList { get; set; }
        }

        // GET: Admin/Derechohabiente
        public ActionResult Index()
        {
            if (User.IsInRole("Recepcion"))
            {

                return View();
            }
            else
            {
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }
        }

        // GET: Admin/Derechohabiente/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Paciente paciente = db.Paciente.Find(id);
            if (paciente == null)
            {
                return HttpNotFound();
            }

            ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");
            return View(paciente);
        }

        // GET: Admin/Derechohabiente/Create
        public ActionResult Create()
        {
            if (User.IsInRole("Recepcion"))
            {
                ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");
                return View();
            }
            else
            {
                return RedirectToAction("BuscarPaciente", "DerechoHabiente");
            }
        }

        // POST: Admin/Derechohabiente/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,UnidadAfiliacion,CURP,Nombre,PrimerApellido,SegundoApellido,Sexo,FechaNacimiento,Nacionalidad,EntidadNacimiento,Edad,Expediente,Dir_Calle,Dir_Numero,Dir_CP,Dir_Colonia,Dir_Municipio,Dir_EntidadFed,Telefono1,Telefono2,Email,RFC,EstadoCivil,Escolaridad,Ocupacion,ServicioSalud,ServicioSalud_Extra,DE_Indigena,DE_Migrante,DE_Afroamericano,BA_Visual,BA_Auditiva,BA_Fisica,BA_Mental,BA_Idioma,BA_Idioma_Nombre,BA_Analfabeta,CE_Nombre,CE_Calle,CE_Numero,CE_CP,CE_Colonia,CE_Municipio,CE_EntidadFed,CE_Telefono,Nombre_Tutor,PrimerApellido_Tutor,SegundoApellido_Tutor,Curp_Calculado,Curp_CalculadoMotivo, NA_DE, NA_BA")] Paciente paciente)
        {
            if (ModelState.IsValid)
            {
                var pacienteNuevo = 0;

                //Buscamos si el px ya está registrado
                var PxYaRegistrado1 = (from a in db.Paciente
                                       where a.CURP == paciente.CURP
                                       select a).FirstOrDefault();

                //Si no se encontró px registrado con CURP ORIGINAL
                if (PxYaRegistrado1 == null)
                {
                    //Buscamos si está registrado con CURP PROVISIONAL
                    var PxYaRegistrado2 = (from a in db.Paciente
                                           where a.Curp_Calculado == paciente.Curp_Calculado
                                           where a.Curp_Calculado != null
                                           select a).FirstOrDefault();

                    //Si tampoco está el px registrado con CURP PROV la variable bandera la establecemos en 1, quiere decir que no existe el px y se registrará como nuevo
                    if (PxYaRegistrado2 == null)
                    {
                        pacienteNuevo = 1;
                    }
                }
                else if (PxYaRegistrado1 != null)//Si se encontró px registrado con CURP ORIGINAL la variable bandera la establecemos en 0
                {
                    pacienteNuevo = 0;
                }
                else//Si no se encontró px registrado será un px nuevo
                {
                    pacienteNuevo = 1;
                }

                //Registrar px solo si no existe (es nuevo)
                if (pacienteNuevo == 1)
                {
                    //Llamamos función que está en la clase de la tbl(bd) para que convierta a mayusculas los campos que trae el objeto 'paciente' que recibe de la view
                    paciente.ConvertirAMayusculas();

                    //Guardamos la Fecha en que se registró el px
                    var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    var fechaDT = DateTime.Parse(fecha);
                    paciente.FechaRegistro = fechaDT;

                    //      SEXO PX
                    if (paciente.Sexo == "Masculino" || paciente.Sexo == "Hombre" || paciente.Sexo == "H")
                    {
                        paciente.Sexo = "Hombre";
                    }
                    else
                    {
                        paciente.Sexo = "Mujer";
                    }

                    //***NOTA*** Se comenta esto ya que cuando sea 'sin curp' se pondrá comoquiera el curp que se arme al llenar datos (al: 18 noviembre 2023)
                    //                                                                                      CURP CALCULADO *Sin Curp*
                    //si la propiedad 'Curp_Calculado' es diferente de null quiere decir que el px no tiene curp
                    //if (paciente.Curp_Calculado != null)
                    //{
                    //    paciente.CURP = "SIN CURP";
                    //}

                    //Convertimos a mayúsculas los datos del tutor en caso que el px sea menor de edad
                    if ((paciente.Nombre_Tutor != null) || (paciente.PrimerApellido_Tutor != null) || (paciente.SegundoApellido_Tutor != null))
                    {
                        if (paciente.Nombre_Tutor != null || paciente.Nombre_Tutor != "")
                            paciente.Nombre_Tutor = paciente.Nombre_Tutor.ToUpper();

                        if (paciente.PrimerApellido_Tutor != null || paciente.PrimerApellido_Tutor != "")
                            paciente.PrimerApellido_Tutor = paciente.PrimerApellido_Tutor.ToUpper();

                        if (paciente.SegundoApellido_Tutor != null || paciente.SegundoApellido_Tutor != "")
                            paciente.SegundoApellido_Tutor = paciente.SegundoApellido_Tutor.ToUpper();
                    }

                    //Convertir FechaNacimiento a Años-Meses-Dias
                    var today = DateTime.Today;
                    DateTime fnac = (DateTime)paciente.FechaNacimiento;
                    int Years = 0;
                    int Months = 0;
                    int Days = 0;

                    if ((today.Year - fnac.Year) > 0 ||
                    (((today.Year - fnac.Year) == 0) && ((fnac.Month < today.Month) ||
                    ((fnac.Month == today.Month) && (fnac.Day <= today.Day)))))
                    {
                        int DaysInBdayMonth = DateTime.DaysInMonth(fnac.Year, fnac.Month);
                        int DaysRemain = today.Day + (DaysInBdayMonth - fnac.Day);

                        if (today.Month > fnac.Month)
                        {
                            Years = today.Year - fnac.Year;
                            Months = today.Month - (fnac.Month + 1) + Math.Abs(DaysRemain / DaysInBdayMonth);
                            Days = (DaysRemain % DaysInBdayMonth + DaysInBdayMonth) % DaysInBdayMonth;
                        }
                        else if (today.Month == fnac.Month)
                        {
                            if (today.Day >= fnac.Day)
                            {
                                Years = today.Year - fnac.Year;
                                Months = 0;
                                Days = today.Day - fnac.Day;
                            }
                            else
                            {
                                Years = (today.Year - 1) - fnac.Year;
                                Months = 11;
                                Days = DateTime.DaysInMonth(fnac.Year, fnac.Month) - (fnac.Day - today.Day);
                            }
                        }
                        else
                        {
                            Years = (today.Year - 1) - fnac.Year;
                            Months = today.Month + (11 - fnac.Month) + Math.Abs(DaysRemain / DaysInBdayMonth);
                            Days = (DaysRemain % DaysInBdayMonth + DaysInBdayMonth) % DaysInBdayMonth;
                        }
                    }
                    var edad = Years + " años con " + Months + " meses" + " y " + Days + " días";


                    //     ***EXPEDIENTE*** que se guardará (nuevo)
                    var Unidad = (from a in db.UnidadAfiliacion
                                  where a.Id == paciente.UnidadAfiliacion
                                  select a).FirstOrDefault();

                    //Buscamos el último paciente registrado de la Unidad para obtener el #exp de ese último paciente y así armar el consecutivo
                    var PX = (from a in db.Paciente
                              where a.UnidadAfiliacion == paciente.UnidadAfiliacion
                              select a).OrderByDescending(u => u.Expediente).FirstOrDefault();

                    //Obtenemos el prefijo de la Unidad
                    var Prefijo = Unidad.Prefijo;
                    //Obtener los espacios del prefijo
                    var Pre = Prefijo.Length;

                    int UltimoConsecutivo;
                    int Conse;

                    //Obtenemos el último consecutivo (el número)
                    if (PX == null)
                    {
                        UltimoConsecutivo = 1;
                        //Generamos el PRÓXIMO NUMERO consecutivo nuevo
                        Conse = UltimoConsecutivo;
                    }
                    else
                    {
                        UltimoConsecutivo = Convert.ToInt32(PX.Expediente.Substring(Pre));
                        //Generamos el PRÓXIMO *NUMERO* consecutivo nuevo
                        Conse = ((UltimoConsecutivo) + 1);
                    }

                    var Conse1 = "";
                    if (Conse < 1000)
                    {
                        if (Conse < 100)
                        {
                            if (Conse < 10)
                            {
                                Conse1 = "000" + Conse;
                            }
                            else
                            {
                                Conse1 = "00" + Conse;
                            }
                        }
                        else
                        {
                            Conse1 = "0" + Conse;
                        }
                    }
                    else
                    {
                        Conse1 = Conse.ToString();
                    }
                    //Nuevo expediente
                    var ConsecutivoNuevo = Prefijo + Conse1;
                    paciente.Expediente = ConsecutivoNuevo;

                    db.Paciente.Add(paciente);
                    db.SaveChanges();

                    //---------Guardamos el Id y FechaRegistro del Paciente que se está creando en la **tbl PacienteCopia**

                    //Buscamos el px que se acaba de registrar (que es el de esta función) para obtener el Id y guardarlo en la tbl PacienteCopia
                    var PxRegis = (from a in db.Paciente
                                           where a.CURP == paciente.CURP
                                           select a).FirstOrDefault();

                    Models.PacienteCopia NuevoPX = new Models.PacienteCopia();
                    NuevoPX.Id_Paciente = PxRegis.Id;
                    NuevoPX.FechaRegistro = fechaDT;
                    hcMed.PacienteCopia.Add(NuevoPX);
                    hcMed.SaveChanges();

                    return Json(new { MENSAJE = "Succe1: " }, JsonRequestBehavior.AllowGet);
                }
                //return RedirectToAction("Index");
                return Json(new { MENSAJE = "Succe2: "}, JsonRequestBehavior.AllowGet);
            }

            ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");
            //TempData["Error"] = "Ocurrio un problema, intente de nuevo";
            return Json(new { MENSAJE = "Succe3: " }, JsonRequestBehavior.AllowGet);

            //return View();
        }

        // GET: Admin/Derechohabiente/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Paciente paciente = db.Paciente.Find(id);
            if (paciente == null)
            {
                return HttpNotFound();
            }
            ViewBag.UNIDADES = new SelectList(db.UnidadAfiliacion.ToList(), "Id", "NombreUnidad");
            return View(paciente);
        }

        // POST: Admin/Derechohabiente/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,UnidadAfiliacion,CURP,Nombre,PrimerApellido,SegundoApellido,Sexo,FechaNacimiento,Nacionalidad,EntidadNacimiento,Edad,Expediente,Dir_Calle,Dir_Numero,Dir_CP,Dir_Colonia,Dir_Municipio,Dir_EntidadFed,Telefono1,Telefono2,Email,RFC,EstadoCivil,Escolaridad,Ocupacion,ServicioSalud,ServicioSalud_Extra,DE_Indigena,DE_Migrante,DE_Afroamericano,BA_Visual,BA_Auditiva,BA_Fisica,BA_Mental,BA_Idioma,BA_Idioma_Nombre,BA_Analfabeta,CE_Nombre,CE_Calle,CE_Numero,CE_CP,CE_Colonia,CE_Municipio,CE_EntidadFed,CE_Telefono,Nombre_Tutor,PrimerApellido_Tutor,SegundoApellido_Tutor,Curp_Calculado,Curp_CalculadoMotivo, NA_DE, NA_BA")] Paciente paciente)
        {
            if (ModelState.IsValid)
            {
                db.Entry(paciente).State = EntityState.Modified;

                //      SEXO PX
                if (paciente.Sexo == "Masculino" || paciente.Sexo == "Hombre" || paciente.Sexo == "H")
                {
                    paciente.Sexo = "Hombre";
                }
                else
                {
                    paciente.Sexo = "Mujer";
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(paciente);
        }

        // GET: Admin/Derechohabiente/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Paciente paciente = db.Paciente.Find(id);
            if (paciente == null)
            {
                return HttpNotFound();
            }
            //return View(paciente);

            //db.Paciente.Remove(paciente);
            paciente.PxEliminado = true;
            db.SaveChanges();

            return Json("PX eliminado exitósamente", JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Index");
        }

        // POST: Admin/Derechohabiente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Paciente paciente = db.Paciente.Find(id);
            //db.Paciente.Remove(paciente);
            paciente.PxEliminado = true;
            db.SaveChanges();

            return Json("PX eliminado exitósamente", JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public JsonResult GenerarExpediente(int UnidadAfi)
        {
            try
            {
                //Buscamos la Unidad de Afiliación con el Id que pasamos de la vista (id de la unidad)
                var Unidad = (from a in db.UnidadAfiliacion
                              where a.Id == UnidadAfi
                              select a).FirstOrDefault();

                //Buscamos el último paciente registrado de la Unidad para obtener el #exp. de ese último paciente y así armar el consecutivo
                var PX = (from a in db.Paciente
                          where a.UnidadAfiliacion == UnidadAfi
                          select a).OrderByDescending(u => u.Expediente).FirstOrDefault();

                //Obtenemos el prefijo de la Unidad
                var Prefijo = Unidad.Prefijo;
                //Obtener los espacios del prefijo
                var Pre = Prefijo.Length;

                int UltimoConsecutivo;
                int Conse;

                //Obtenemos el último consecutivo (el número)
                if (PX == null)
                {
                    UltimoConsecutivo = 1;

                    //Generamos el PRÓXIMO NUMERO consecutivo nuevo
                    Conse = UltimoConsecutivo;
                }
                else
                {
                    UltimoConsecutivo = Convert.ToInt32(PX.Expediente.Substring(Pre));

                    //Generamos el PRÓXIMO *NUMERO* consecutivo nuevo
                    Conse = ((UltimoConsecutivo) + 1);
                }

                var Conse1 = "";
                if (Conse < 1000)
                {
                    if (Conse < 100)
                    {
                        if (Conse < 10)
                        {
                            Conse1 = "000" + Conse;
                        }
                        else
                        {
                            Conse1 = "00" + Conse;
                        }
                    }
                    else
                    {
                        Conse1 = "0" + Conse;
                    }
                }
                else
                {
                    Conse1 = Conse.ToString(); 
                }

                //Consecutivo Nuevo con prefijo
                //var ConsecutivoNuevo = Prefijo + Conse;
                var ConsecutivoNuevo = Prefijo + Conse1;

                return Json(new { MENSAJE = "Succe: S", Exp = ConsecutivoNuevo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
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
            public string FechaNacimiento { get; set; }
            public DateTime FechaNacimiento2 { get; set; }
            public int Edad { get; set; }
            public string Expediente { get; set; }
            public string Curp_Calculado { get; set; }
            public string FechaRegistro { get; set; }
        }

        //********      Función para buscar PX para crear su H.C.
        [HttpPost]
        public JsonResult BuscarPaciente(string NombrePX, string ExpedientePX)
        {
            try
            {
                List<Propiedades> Pac = new List<Propiedades>();
                var result1 = new List<Propiedades>();

                //Búsqueda de px por Expediente
                if (NombrePX == null || NombrePX == "")
                {
                    //string query = "SELECT Id,UnidadAfiliacion,CURP,Nombre,PrimerApellido,SegundoApellido,Sexo,FechaNacimiento,FechaNacimiento as FechaNacimiento2,Edad,Expediente,Curp_Calculado " +
                    //                "FROM Paciente " +
                    //                "WHERE Expediente LIKE '%" + ExpedientePX + "%'";
                    //var result = db.Database.SqlQuery<Propiedades>(query);
                    //Pac = result.ToList();

                    var query = (from a in db.Paciente
                                 where a.Expediente.Contains(ExpedientePX)
                                 select a).ToList();
                    
                    foreach (var q in query)
                    {
                        var resultado = new Propiedades
                        {
                            Id = q.Id,
                            UnidadAfiliacion = q.UnidadAfiliacion,
                            CURP = q.CURP,
                            Nombre = q.Nombre,
                            PrimerApellido = q.PrimerApellido,
                            SegundoApellido = q.SegundoApellido,
                            Sexo = q.Sexo,
                            FechaNacimiento = string.Format("{0:dd/MM/yyyy}", q.FechaNacimiento, new CultureInfo("es-ES")),
                            Edad = q.Edad,
                            Expediente = q.Expediente,
                            Curp_Calculado = q.Curp_Calculado
                        };
                        result1.Add(resultado);
                    }
                }
                //Búsqueda de px por NOmbre
                if (ExpedientePX == null || ExpedientePX == "")
                {
                    //Pacientes = db.Paciente.Where(v => v.Nombre.Contains(NombrePX)).ToList();

                    //string query = "SELECT Id,UnidadAfiliacion,CURP,Nombre,PrimerApellido,SegundoApellido,Sexo,FechaNacimiento,Edad,Expediente,Curp_Calculado " +
                    //                "FROM Paciente " +
                    //                "WHERE Nombre LIKE '%" + NombrePX + "%'";
                    //var result = db.Database.SqlQuery<Propiedades>(query);
                    //Pac = result.ToList();

                    var query = (from a in db.Paciente
                                 where a.Nombre.Contains(NombrePX)
                                 select a).ToList();

                    foreach (var q in query)
                    {
                        var resultado = new Propiedades
                        {
                            Id = q.Id,
                            UnidadAfiliacion = q.UnidadAfiliacion,
                            CURP = q.CURP,
                            Nombre = q.Nombre,
                            PrimerApellido = q.PrimerApellido,
                            SegundoApellido = q.SegundoApellido,
                            Sexo = q.Sexo,
                            FechaNacimiento = string.Format("{0:dd/MM/yyyy}", q.FechaNacimiento, new CultureInfo("es-ES")),
                            Edad = q.Edad,
                            Expediente = q.Expediente,
                            Curp_Calculado = q.Curp_Calculado
                        };
                        result1.Add(resultado);
                    }
                }

                //return Json(new { MENSAJE = "Succe: ", PACIENTES = Pac }, JsonRequestBehavior.AllowGet);
                return Json(new { MENSAJE = "Succe: ", PACIENTES = result1 }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //********      Función para buscar PX al entrar a la LISTA DE PX
        [HttpPost]
        public JsonResult BuscarPacienteIndex()
        {
            //List<Propiedades> Pac = new List<Propiedades>();

            //string query = "SELECT TOP(10) Id,UnidadAfiliacion,CURP,Nombre,PrimerApellido,SegundoApellido,Sexo,FechaNacimiento,Edad,Expediente,Curp_Calculado,FechaRegistro " +
            //                        "FROM Paciente ORDER BY Id DESC ";
            //var result = db.Database.SqlQuery<Propiedades>(query);
            //Pac = result.ToList();

            var result1 = new List<Propiedades>();

            var query = (from a in db.Paciente
                         where a.PxEliminado != true
                         select a).OrderByDescending(a=>a.Id).Take(10).ToList();

            foreach (var q in query)
            {
                var resultado = new Propiedades
                {
                    Id = q.Id,
                    UnidadAfiliacion = q.UnidadAfiliacion,
                    CURP = q.CURP,
                    Nombre = q.Nombre,
                    PrimerApellido = q.PrimerApellido,
                    SegundoApellido = q.SegundoApellido,
                    Sexo = q.Sexo,
                    FechaNacimiento = string.Format("{0:dd/MM/yyyy}", q.FechaNacimiento, new CultureInfo("es-ES")),
                    Edad = q.Edad,
                    Expediente = q.Expediente,
                    Curp_Calculado = q.Curp_Calculado,
                    FechaRegistro = string.Format("{0:dd/MM/yyyy}", q.FechaRegistro, new CultureInfo("es-ES")),
                };
                result1.Add(resultado);
            }
            //return View(Paciente);
            return Json(new { MENSAJE = "Succe: ", PACIENTES = result1 }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BuscarPaciente()
        {
            // Tu lógica global aquí
            return View();
        }

    }
}
