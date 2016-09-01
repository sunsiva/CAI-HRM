using HRPortal.Common;
using HRPortal.Helper;
using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HRPortal.Controllers
{
    [LogActionFilter]
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
            List<StagingReportViewModel> lstStagingReport; //= getStagingReport(partner);
            ViewBag.partner = false;
            ViewBag.Offered = true;
            ViewBag.WeekList = false;
            ViewBag.VenderList = true;
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
            return View(lstStagingReport);
        }

        public ActionResult Staging(string partner)
        {
            bool isSuperAdmin = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN") ? true : false;
            ViewBag.VendorList = vmodelCan.GetVendorListWithIDs();
            List<StagingReportViewModel> lstStagingReport;
            ViewBag.partner = true;
            ViewBag.Offered = true;
            ViewBag.VenderList = true;
            ViewBag.WeekList = false;

            if (!isSuperAdmin)
            {
                partner = CookieStore.GetCookie(CacheKey.VendorId.ToString());
            }
            
            var rptList = db.rptGetCandidatesStagingByPartner(partner);
            lstStagingReport = rptList.Select(i => new StagingReportViewModel
            {
                Position_Name = i.POSITION_NAME,
                Partner_Name = Convert.ToString(i.PARTNER),
                Round1 = Convert.ToInt32(i.ROUND1),
                Round2 = Convert.ToInt32(i.ROUND2),
                Round3 = Convert.ToInt32(i.ROUND3),
                Screening = Convert.ToInt32(i.SCREENING),
                Offered = Convert.ToInt32(i.OFFERED),
                Total = Convert.ToInt32(i.Total)
            }).ToList();

            return PartialView("_StagingReport", lstStagingReport);
        }
        public ActionResult CadidatesByLastWorkingDay()
        {
            List<LWDCandidateReportViewModel> lstLWDCandidateReportViewModel;
            ViewBag.WeekList = false;
            var rptList = db.GetCandidateDetailsByLastworkingdate();
            lstLWDCandidateReportViewModel = rptList.Select(i => new LWDCandidateReportViewModel
            {
                Position_Name = i.POSITION_NAME,
                Partner_Name = Convert.ToString(i.PARTNER),
                Candidate_name = Convert.ToString(i.CANDIDATE_NAME),
                Submitted_On = Convert.ToDateTime(i.SUBMITTED_ON) == DateTime.MinValue ? "" : Convert.ToDateTime(i.SUBMITTED_ON).ToShortDateString(),
                Last_Working_Date = Convert.ToDateTime(i.LAST_WORKING_DATE) == DateTime.MinValue ? "" : Convert.ToDateTime(i.LAST_WORKING_DATE).ToShortDateString(),
                Status = Convert.ToString(i.STATUS)
            }).ToList();
            return PartialView("_LWDReport", lstLWDCandidateReportViewModel);
        }
        public ActionResult CadidatesIdleTime(string week)
        {
            if (string.IsNullOrEmpty(week))
                week = "1week";
            List<StagingReportViewModel> lstStagingReportViewModel;
            ViewBag.WeekList = true;
            ViewBag.VenderList = false;
            ViewBag.partner = false;
            ViewBag.Offered = false;
            var rptList = db.rptGetCandidatesIdleTimeByWeek(week);
            lstStagingReportViewModel = rptList.Select(i => new StagingReportViewModel
            {
                Position_Name = i.position_name,
                Round1 = Convert.ToInt32(i.Round1),
                Round2 = Convert.ToInt32(i.Round1),
                Round3 = Convert.ToInt32(i.Round1),
                Screening = Convert.ToInt32(i.ScreeningSubmitted)
            }).ToList();
            return PartialView("_StagingReport", lstStagingReportViewModel);
        }

        public  ActionResult CadidatesIdleTimeDetails(string week)
        {
            if (string.IsNullOrEmpty(week))
                week = "1week";
            List<LWDCandidateReportViewModel> lstLWDCandidateReportViewModel;
            ViewBag.WeekList = true;
            ViewBag.VenderList = false;
            ViewBag.partner = false;
            var rptList =  db.rptGetCandidatesIdleTimeDetailsByWeek(week);
            lstLWDCandidateReportViewModel = rptList.Select(i => new LWDCandidateReportViewModel
            {
                Position_Name = i.position_name,
                Partner_Name = Convert.ToString(i.PARTNER),
                Candidate_name = Convert.ToString(i.CANDIDATE_NAME),
                Submitted_On = Convert.ToDateTime(i.SUBMITTED_ON) == DateTime.MinValue ? "" : Convert.ToDateTime(i.SUBMITTED_ON).ToShortDateString(),
                Last_Working_Date = Convert.ToDateTime(i.LAST_WORKING_DATE)==DateTime.MinValue?"": Convert.ToDateTime(i.LAST_WORKING_DATE).ToShortDateString(),
                Status = Convert.ToString(i.STATUS)
            }).ToList();
            return PartialView("_LWDReport", lstLWDCandidateReportViewModel);
        }
    }
}