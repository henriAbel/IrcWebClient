using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using IrcWebApplication.Models;
using System.Web.Security;

namespace IrcWebApplication.Controllers
{
    public class AccountController : Controller
    {
        public ViewResult Login()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Login")]
        public ActionResult PostLogin(LoginModel loginModel)
        {
            if (ModelState.IsValid)
            {
                // Guid will be used in SignalR auth
                Guid uuid = Guid.NewGuid();
                loginModel.Uuid = uuid.ToString();
                Session["loginModel"] = loginModel;
                return RedirectToAction("index", "home");
            }

            return View(loginModel);
        }

        [HttpPost]
        [ActionName("SignOut")]
        public ActionResult PostSignOut()
        {
            Session["loginModel"] = null;
            return RedirectToAction("Login", "account");
        }
    }
}