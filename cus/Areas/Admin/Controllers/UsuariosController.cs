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
        public async Task<ActionResult> Agregar(RegisterViewModel model, string role1)
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


            return RedirectToAction("Index", "Usuarios");
            //return View();
        }


        public ActionResult Editar()
        {
            return View();
        }

    }
}