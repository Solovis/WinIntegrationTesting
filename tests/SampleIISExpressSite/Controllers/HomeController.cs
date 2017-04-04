using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SampleIISExpressSite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.SomeExistingSetting = ConfigurationManager.AppSettings["someExistingSetting"] ?? "NotSet";
            ViewBag.SomeNewSetting = ConfigurationManager.AppSettings["someNewSetting"] ?? "NotSet";

            return View();
        }
        
    }
}