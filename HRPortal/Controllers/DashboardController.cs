using HRPortal.Common;
using HRPortal.Models;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : Controller
    {
        HRPortalEntities db;
        private CandidateViewModels vmodel = new CandidateViewModels();
        public DashboardController()
        {
            db = new HRPortalEntities();
        }

        // GET: Dashboard
        public ActionResult Index()
        {
            HistoryViewModels obj = new HistoryViewModels();
            ViewBag.VendorList = vmodel.GetVendorListWithIDs();
            obj = FilterResult(string.Empty, string.Empty, string.Empty, string.Empty);
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
                        && ven.VENDOR_NAME.ToUpper().Trim().Contains(partner.ToUpper())
                        && job.POSITION_NAME.ToUpper().Trim().Contains(position.ToUpper())
                        && (stdt != string.Empty ? ((item.MODIFIED_ON.HasValue ? Convert.ToDateTime(item.MODIFIED_ON.Value.ToShortDateString()) :
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

        public ActionResult ExportToExcel(bool status, bool r1, bool r2, bool r3,bool offerd, string partner)
        {
          //  try
           // {
            //    System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            //    string isSS = (status? "true":"false");
            //    partner = (partner == "null" ? string.Empty : partner);
            //    if (partner==string.Empty)
            //    {
            //        var canDb = db.rptGetCandidatesStaging(isSS, r1, r2, r3, offerd, partner);
            //        var canLst = canDb.Select(i => new
            //        {
            //            POSITION_NAME = i.POSITION_NAME,
            //            SCREENING = i.ScreeningSubmitted,
            //            ROUND1 = i.Round1,
            //            ROUND2 = i.Round2,
            //            ROUND3 = i.Round3
            //        }).ToList();
            //        gv.DataSource = canLst;
            //    }
            //    else {
            //        var canDb = db.rptGetCandidatesStagingByPartner(isSS, r1, r2, r3,offerd, partner);
            //        var canLst = canDb.Select(i => new
            //        {
            //            POSITION_NAME = i.POSITION_NAME,
            //            PARTNER = i.PARTNER,
            //            SCREENING = i.SCREENING,
            //            ROUND1 = i.ROUND1,
            //            ROUND2 = i.ROUND2,
            //            ROUND3 = i.ROUND3
            //        }).ToList();
            //        gv.DataSource = canLst;
            //    }

            //    gv.DataBind();
            //    Response.ClearContent();
            //    Response.Buffer = true;
            //    string fileName = "Report_" + DateTime.Now.Day + DateTime.Now.ToString("MMM") + ".xls";
            //    Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
            //    Response.ContentType = "application/ms-excel";  //application/vnd.ms-excel
                
            //    Response.Charset = "";
            //    StringWriter sw = new StringWriter();
            //    System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
            //    gv.RenderControl(htw);
            //    Response.Output.Write(sw.ToString());
            //    Response.Flush();
            //    Response.End();
                return new EmptyResult();// RedirectToAction("Index", "Dashboard");
            //}
            //catch (Exception ex) { throw ex; }
        }
    }
}