using CUS.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CUS.Areas.Admin.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private Models.CUS db = new Models.CUS();
        private ApplicationUserManager _userManager;


        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }


        // GET: Admin/Usuarios
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Agregar()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Agregar(RegisterViewModel model/*, string role1*/)
        {
            //Guardar usuario
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
            };

            var result = await UserManager.CreateAsync(user, model.Password);
            //Se le agrega el rol
            //user.Roles.Add(new IdentityUserRole { RoleId = role1 });


            //Agregar el rol al usuario recién crado
            //1-Recepcion. 2-Expediente. 3-Admin. 4-Enfermeria

            if (model.AdminUsuarios) //Admin (todos los roles)
            {
                //hacer el update para insertar en AspNetUserRoles
                string userId = user.Email;
                string roleId = "3";

                string sqlQuery = $"UPDATE AspNetUserRoles SET UserId = '{userId}', RoleId = '{roleId}'";
                db.Database.ExecuteSqlCommand(sqlQuery);
            }
            else
            {
                if (model.AdminPacientes)//Recepción (registro de px)
                {
                    string userId = user.Email;
                    string roleId = "1";

                    string sqlQuery = $"UPDATE AspNetUserRoles SET UserId = '{userId}', RoleId = '{roleId}'";
                    db.Database.ExecuteSqlCommand(sqlQuery);
                }
                else if (model.ExpMedico)//Expediente
                {
                    string userId = user.Email;
                    string roleId = "2";

                    string sqlQuery = $"UPDATE AspNetUserRoles SET UserId = '{userId}', RoleId = '{roleId}'";
                    db.Database.ExecuteSqlCommand(sqlQuery);
                }
                else //Enfermeria
                {
                    string userId = user.Email;
                    string roleId = "4";

                    string sqlQuery = $"UPDATE AspNetUserRoles SET UserId = '{userId}', RoleId = '{roleId}'";
                    db.Database.ExecuteSqlCommand(sqlQuery);
                }
            }

            return RedirectToAction("Index", "Usuarios");
            //return View();
        }


        public ActionResult Editar()
        {
            return View();
        }

    }
}