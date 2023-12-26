using CUS.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    public class ArchivosController : Controller
    {
        Models.CUS db = new Models.CUS();
        // GET: Admin/Archivos
        public ActionResult Index(string expediente)
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

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Create(Archivos archivos, string expediente, HttpPostedFileBase imagenSubida, string dia, string mes, string anio)
        {
            try
            {
                if (imagenSubida != null && imagenSubida.ContentLength > 0)
                {
                    //var nombreArchivo = Path.GetFileName(imagenSubida.FileName);
                    //var path = Path.Combine(Server.MapPath("~/Content/"), nombreArchivo);

                    //var path = Path.Combine(Server.MapPath("~/Content/" + archivos.tipo_archivo + "/" + expediente + "/"), nombreArchivo);
                    string rutaGuardado = Server.MapPath("~/Content/" + archivos.tipo_archivo + "/" + expediente + "/");
                    
                    if (!Directory.Exists(rutaGuardado))
                    {
                        Directory.CreateDirectory(rutaGuardado);
                    }


                    string nombreArchivo = "";
                    //HttpPostedFile archivoImagen = imagenSubida;
                    if (imagenSubida != null && imagenSubida.ContentLength > 0)
                    {
                        nombreArchivo = Path.GetFileName(imagenSubida.FileName);
                        string rutaCompleta = Path.Combine(rutaGuardado, nombreArchivo);
                        imagenSubida.SaveAs(rutaCompleta);
                    }



                    //imagenSubida.SaveAs(file);

                    var fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    var fechaDT = DateTime.Parse(fecha);
                    var fechaArchivoSplit = DateTime.Now.ToString(anio + "-" + mes + "-" + dia + "THH:mm:ss");
                    var fechaArchivo = DateTime.Parse(fechaArchivoSplit);
                    var username = User.Identity.GetUserName();

                    Archivos arch = new Archivos();
                    arch.titulo = archivos.titulo;
                    arch.usuario = username;
                    arch.tipo_archivo = archivos.tipo_archivo;
                    arch.archivo = nombreArchivo;
                    //arch.fecha_archivo = archivos.fecha_archivo;
                    arch.expediente = expediente;
                    arch.fecha_registro = fechaDT;
                    arch.fecha_archivo = fechaArchivo;
                    //Historia.Id_HistoriaClinica = Id_claveHC;
                    //nota.Clave_hc_px = Id_claveHC;
                    db.Archivos.Add(arch);
                    db.SaveChanges();


                    TempData["message_success"] = "Archivo agregado con éxito";
                    return Redirect(Request.UrlReferrer.ToString());
                }
                else
                {
                    TempData["message_error"] = "Debes subir un archivo valido";
                    return Redirect(Request.UrlReferrer.ToString());
                }
            }
            catch (Exception ex)
            {
                //return Json(new { MENSAJE = "Error: Error de sistema: " + ex.Message }, JsonRequestBehavior.AllowGet);
                TempData["message_error"] = "Error, vuelve a intentar";
                return Redirect(Request.UrlReferrer.ToString());
            }


        }


        public class ArchivosLista
        {
            public string Expediente { get; set; }
            public string Paciente { get; set; }
            public string Medico { get; set; }
            public string Fecha_registro { get; set; }
            public string Fecha_archivo { get; set; }
            public string Boton { get; set; }

        }


        public JsonResult ListaArchivos(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.Archivos
                         join pa in db.Paciente on ne.expediente equals pa.Expediente into pax
                         from paIn in pax.DefaultIfEmpty()
                         where ne.usuario == username
                         //where ne.num_exp == expediente
                         select new
                         {
                             Expediente = ne.expediente,
                             Paciente = paIn.Nombre + " " + paIn.PrimerApellido + " " + paIn.SegundoApellido,
                             Medico = ne.usuario,
                             Fecha_archivo = ne.fecha_archivo,
                             Fecha_registro = ne.fecha_registro,
                             Boton = ne.id,

                         }).ToList().OrderByDescending(n => n.Fecha_registro);



            var listaNotas = new List<ArchivosLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new ArchivosLista
                {
                    //Expediente = item.Expediente,
                    Paciente = item.Paciente,
                    Medico = item.Medico,
                    Fecha_archivo = string.Format("{0:dddd, dd MMMM yyyy}", item.Fecha_archivo, new CultureInfo("es-ES")),
                    Fecha_registro = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.Fecha_registro, new CultureInfo("es-ES")),
                    Boton = "<button data-id='" + item.Boton + "' class='btn btn-primary vermas'>Ver más</button>",
                };

                listaNotas.Add(listaLlenar);
            }



            return new JsonResult { Data = listaNotas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        public JsonResult ListaArchivosPx(string expediente)
        {
            var username = User.Identity.GetUserName();

            var notas = (from ne in db.Archivos
                         join pa in db.Paciente on ne.expediente equals pa.Expediente into pax
                         from paIn in pax.DefaultIfEmpty()
                         where ne.usuario == username
                         where ne.expediente == expediente
                         select new
                         {
                             Expediente = ne.expediente,
                             Paciente = paIn.Nombre + " " + paIn.PrimerApellido + " " + paIn.SegundoApellido,
                             Medico = ne.usuario,
                             Fecha_archivo = ne.fecha_archivo,
                             Fecha_registro = ne.fecha_registro,
                             Boton = ne.id,

                         }).ToList().OrderByDescending(n => n.Fecha_registro);



            var listaNotas = new List<ArchivosLista>();

            foreach (var item in notas)
            {

                var listaLlenar = new ArchivosLista
                {
                    //Expediente = item.Expediente,
                    Paciente = item.Paciente,
                    Medico = item.Medico,
                    Fecha_archivo = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.Fecha_archivo, new CultureInfo("es-ES")),
                    Fecha_registro = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", item.Fecha_registro, new CultureInfo("es-ES")),
                    Boton = "<button data-id='" + item.Boton + "' class='btn btn-primary vermas'>Ver más</button>",
                };

                listaNotas.Add(listaLlenar);
            }



            return new JsonResult { Data = listaNotas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        public JsonResult DtsArchivos(int? id)
        {

            var archivo = (from ar in db.Archivos
                        join paci in db.Paciente on ar.expediente equals paci.Expediente into paciX
                        from paciIn in paciX.DefaultIfEmpty()
                        where ar.id == id
                        select new
                        {
                            archivo = ar.archivo,
                            expediente = ar.expediente,
                            titulo = ar.titulo,
                            tipo_archivo = ar.tipo_archivo,
                            fecha_archivo = ar.fecha_archivo,
                            fecha_registro = ar.fecha_registro,

                        }).FirstOrDefault();

            var rst = new Object();

            if (archivo != null)
            {
                var arch = "";
                var archivoSplit = archivo.archivo.Split('.');

                if(archivoSplit[1] == "pdf" || archivoSplit[1] == "docx")
                {
                    arch = "<a target='_blank' href='../Content/" + archivo.tipo_archivo + "/" + archivo.expediente + "/" + archivo.archivo + "' class='btn btn-primary'>Ver archivo</a>";
                }
                else
                {
                    arch = "<a target='_blank' href='../Content/" + archivo.tipo_archivo + "/" + archivo.expediente + "/" + archivo.archivo + "' class='btn btn-primary'>Ver archivo</a>";
                }

                rst = new
                {
                    arch = arch,
                    titulo = archivo.titulo,
                    tipo_archivo = archivo.tipo_archivo,
                    fecha_archivo = string.Format("{0:dddd, dd MMMM yyyy}", archivo.fecha_archivo, new CultureInfo("es-ES")),
                    fecha_registro = string.Format("{0:dddd, dd MMMM yyyy HH:mm}", archivo.fecha_registro, new CultureInfo("es-ES"))
                };
            }

            return new JsonResult { Data = rst, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }





    }
}