using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRPortal.Controllers
{
    public class DashboardController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        // GET: Dashboard
        public ActionResult Index()
        {
            HistoryViewModels obj = new HistoryViewModels();
            var data = (from item in db.STATUS_HISTORY.Where(x => x.ISACTIVE == true).ToList()
                        join stsMst in db.STATUS_MASTER on item.STATUS_ID equals stsMst.STATUS_ID
                        where stsMst.ISACTIVE == true
                        select stsMst).ToList();
            obj.ToT_Candidates_OFRD = data.Where(x => x.STATUS_NAME.Contains("OFFRD")).Count();
            obj.ToT_Candidates_PRGS = data.Where(x => !x.STATUS_NAME.Contains("OFFRD") && !x.STATUS_NAME.Contains("RJ")).Count();
            obj.ToT_Candidates_RJTD = data.Where(x => x.STATUS_NAME.Contains("RJ")).Count();
            obj.ToT_Active_Jobs = db.JOBPOSTINGs.Where(x => x.ISACTIVE == true).ToList().Count();
            return View(obj);
        }
    }
}