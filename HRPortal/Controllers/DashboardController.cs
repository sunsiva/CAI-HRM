using HRPortal.Common;
using HRPortal.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        HRPortalEntities db;
        public DashboardController()
        {
            db = new HRPortalEntities();
        }

        // GET: Dashboard
        public ActionResult Index()
        {
            HistoryViewModels obj = new HistoryViewModels();
            obj = FilterResult(string.Empty, string.Empty, string.Empty, string.Empty);
            //var data = (from item in db.CANDIDATES.Where(x => x.ISACTIVE == true).ToList()
            //            join stsMst in db.STATUS_MASTER on string.IsNullOrEmpty(item.STATUS) ? Guid.Empty : Guid.Parse(item.STATUS) equals stsMst.STATUS_ID
            //            where stsMst.ISACTIVE == true
            //            select stsMst).ToList();
            //obj.ToT_Candidates_OFRD = data.Where(x => x.STATUS_NAME.Contains("OFFRD")).Count();
            //obj.ToT_Candidates_PRGS = data.Where(x => !x.STATUS_NAME.Contains("OFFRD") && !x.STATUS_NAME.Contains("RJ")).Count();
            //obj.ToT_Candidates_RJTD = data.Where(x => x.STATUS_NAME.Contains("RJ")).Count();
            //obj.ToT_Candidates_JOIN = data.Where(x => x.STATUS_NAME.Contains("JOIN")).Count();
            //obj.ToT_Active_Jobs = db.JOBPOSTINGs.Where(x => x.ISACTIVE == true).ToList().Count();
            return View(obj);

        }

        public ActionResult GetCount(string partner, string position, string stdt, string edt)
        {
            HistoryViewModels model = FilterResult(partner, position, stdt, edt);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private HistoryViewModels FilterResult(string partner, string position, string stdt, string edt)
        {
            HistoryViewModels model = new HistoryViewModels();
            var data = (from item in db.CANDIDATES.Where(x => x.ISACTIVE == true).ToList()
                        join stsMst in db.STATUS_MASTER on string.IsNullOrEmpty(item.STATUS) ? Guid.Empty : Guid.Parse(item.STATUS) equals stsMst.STATUS_ID
                        join job in db.JOBPOSTINGs on item.JOB_ID equals job.JOB_ID
                        join usr in db.AspNetUsers on item.CREATED_BY equals usr.Id
                        join ven in db.VENDOR_MASTER on usr.Vendor_Id equals ven.VENDOR_ID
                        where ven.ISACTIVE == true
                        && ven.VENDOR_NAME.ToUpper().Contains(partner.ToUpper())
                        && job.POSITION_NAME.ToUpper().Contains(position.ToUpper())
                        && (stdt != string.Empty ? ((item.MODIFIED_ON.HasValue? Convert.ToDateTime(item.MODIFIED_ON.Value.ToShortDateString()):
                        Convert.ToDateTime(item.CREATED_ON.ToShortDateString())) >= Convert.ToDateTime(stdt)) : true)
                        && (edt != string.Empty ? ((item.MODIFIED_ON.HasValue ? Convert.ToDateTime(item.MODIFIED_ON.Value.ToShortDateString()) : 
                        Convert.ToDateTime(item.CREATED_ON.ToShortDateString())) <= Convert.ToDateTime(edt)) : true)
                        select stsMst).ToList();

            model.ToT_Candidates_OFRD = data.Where(x => x.STATUS_NAME.Contains("OFFRD")).Count();
            model.ToT_Candidates_PRGS = data.Where(x => !x.STATUS_NAME.Contains("OFFRD") && !x.STATUS_NAME.Contains("JOIN") && !x.STATUS_NAME.Contains("RJ")).Count();
            model.ToT_Candidates_RJTD = data.Where(x => x.STATUS_NAME.Contains("RJ")).Count();
            model.ToT_Candidates_JOIN = data.Where(x => x.STATUS_NAME.Contains("JOIN")).Count();
            model.ToT_Active_Jobs = db.JOBPOSTINGs.Where(x => x.ISACTIVE == true).ToList().Count();
            return model;
        }
    }
}