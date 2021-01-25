using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MI_MVC.Controllers
{
    public class PortfolioController : BaseController
    {
        // GET: Portfolio
        public ActionResult Index()
        {
            return View();
        }
    }
}