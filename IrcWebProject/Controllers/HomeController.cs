using IrcWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace IrcWebApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            LoginModel model = (LoginModel)Session["loginModel"];
            if (null == model)
            {
                return RedirectToAction("Login", "account");
            }
                
            return View(model);
        }
    }
}