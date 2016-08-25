using HRPortal.Common;
using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private CandidateViewModels vmodelCan = new CandidateViewModels();
        HRPortalEntities db;
        public ReportController()
        {
            db = new HRPortalEntities();
        }
        // GET: Reports
        public ActionResult Index(string partner)
        {
            ViewBag.VendorList = vmodelCan.GetVendorListWithIDs();
            List<StagingReportViewModel> lsttagingReport = getStagingReport(partner);
            return View(lsttagingReport);
        }

        public ActionResult Staging(string partner)
        {
            ViewBag.VendorList = vmodelCan.GetVendorListWithIDs();
            List<StagingReportViewModel> lsttagingReport = getStagingReport(partner);
            return PartialView("_StagingReport",lsttagingReport);
        }

        private List<StagingReportViewModel> getStagingReport(string partner)
        {
            List<StagingReportViewModel> lstStagingReport;
            if (string.IsNullOrEmpty(partner))
            {
                ViewBag.partner = false;
                var rptList = db.rptGetCandidatesStaging(partner);

                lstStagingReport = rptList.Select(i => new StagingReportViewModel
                {
                    Position_Name = i.position_name,
                    Round1 = Convert.ToInt32(i.Round1),
                    Round2 = Convert.ToInt32(i.Round2),
                    Round3 = Convert.ToInt32(i.Round3),
                    Screening = Convert.ToInt32(i.ScreeningSubmitted),
                    Offered = Convert.ToInt32(i.OFFERED),
                    Total = Convert.ToInt32(i.Total)
                }).ToList();
            }
            else
            {
                ViewBag.partner = true;
                var rptList  = db.rptGetCandidatesStagingByPartner(partner);
                lstStagingReport = rptList.Select(i => new StagingReportViewModel
                {
                    Position_Name = i.POSITION_NAME,
                    Partner_Name = i.PARTNER,
                    Round1 = Convert.ToInt32(i.ROUND1),
                    Round2 = Convert.ToInt32(i.ROUND2),
                    Round3 = Convert.ToInt32(i.ROUND3),
                    Screening = Convert.ToInt32(i.SCREENING),
                    Offered = Convert.ToInt32(i.OFFERED),
                    Total = Convert.ToInt32(i.Total)
                }).ToList();
            }
            return lstStagingReport;
        }
    }
}