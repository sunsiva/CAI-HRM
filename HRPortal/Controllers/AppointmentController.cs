using System;
using System.IO;
using System.Web.Mvc;
using HRPortal.Models;
using System.Linq;
using System.Threading.Tasks;
using HRPortal.Common;
using HRPortal.Common.Enums;
using System.Collections.Generic;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    public class AppointmentController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private CandidateViewModels vmodel = new CandidateViewModels();
        private AppointmentViewModels appVM = new AppointmentViewModels();
        const int pageSize = 10;

        #region "Appointment"
        // GET: Appointment
        public ActionResult Index()
        {
            return View();
        }

        public string Init()
        {
            bool rslt = false;// Utils.InitialiseDiary();
            return rslt.ToString();
        }

        public void UpdateEvent(int id, string NewEventStart, string NewEventEnd)
        {
            try
            {
                appVM.UpdateDiaryEvent(id, NewEventStart, NewEventEnd);
            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<bool> SaveEvent(string Title, string NewEventDate, string NewEventTime, string NewEventDuration, string sendTo)
        {
            try
            {
                if (System.Configuration.ConfigurationManager.AppSettings["IsAppointmentMail"] == "true")
                {
                    Guid canId = Guid.Parse("BC30BED0-8F41-4CE0-B8BD-DF47B30060CB");
                    await appVM.SendInvite(NewEventDate + " " + NewEventTime, NewEventDuration, sendTo, canId, Title);
                }
                return appVM.SaveEvent(Title, NewEventDate, NewEventTime, NewEventDuration);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult GetDiarySummary(double start, double end)
        {
            try
            {
                //var sstart = DateTime.UtcNow.Date.;
                var ApptListForDate = appVM.LoadAppointmentSummaryInDateRange(start, end);
                var eventList = from e in ApptListForDate.ToList()
                                select new
                                {
                                    id = e.ID,
                                    title = e.Title,
                                    start = e.StartDateString,
                                    end = e.EndDateString,
                                    someKey = e.KeyID,
                                    allDay = false
                                };
                var rows = eventList.ToArray();
                return Json(rows, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { throw ex; }
        }

        public JsonResult GetDiaryEvents(double start, double end)
        {
            try
            {
                var ApptListForDate = appVM.LoadAllAppointmentsInDateRange(start, end);
                var eventList = from e in ApptListForDate
                                select new
                                {
                                    id = e.ID,
                                    title = e.Title,
                                    start = e.StartDateString,
                                    end = e.EndDateString,
                                    color = e.StatusColor,
                                    className = e.ClassName,
                                    someKey = e.KeyID,
                                    allDay = false
                                };
                var rows = eventList.ToArray();
                return Json(rows, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region "Schedules"
        public ActionResult Schedules(string sOdr, int? page)
        {
            List<CandidateViewModels> lstCan = GetCandidateSchedules(DateTime.Now);
            var objCan = lstCan != null ? GetPagination(lstCan, sOdr, page) : lstCan;
            return View(objCan);
        }

        private List<CandidateViewModels> GetCandidateSchedules(DateTime scheduleDt)
        {
            try
            {
                var dbCan = db.CANDIDATES.Where(c => c.ISACTIVE == true).ToList();
                var dbJobs = db.JOBPOSTINGs.ToList();
                var Canlist = (from c in dbCan
                                    join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                    join u1 in db.AspNetUsers.ToList() on c.CREATED_BY equals u1.Id
                                    join v in db.VENDOR_MASTER.ToList() on u1.Vendor_Id equals v.VENDOR_ID
                                    join sh in db.STATUS_HISTORY.ToList() on c.CANDIDATE_ID equals sh.CANDIDATE_ID
                                    where sh.SCHEDULED_TO != null && sh.SCHEDULED_TO.Value.ToString("dd-MM-yyyy")== scheduleDt.ToString("dd-MM-yyyy")
                                    select new { Candidate = c, Job = j,StsHist = sh }).Select(i => new CandidateViewModels
                                    {
                                        CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                        CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                        POSITION = i.Job.POSITION_NAME,
                                        RESUME_FILE_PATH = string.IsNullOrEmpty(i.Candidate.RESUME_FILE_PATH) ? string.Empty : Path.Combine("/UploadDocument/", i.Candidate.RESUME_FILE_PATH),
                                        NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                        YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                        LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                        VENDOR_NAME = GetPartnerName(i.Candidate.CREATED_BY),
                                        STATUS = vmodel.GetStatusNameById(i.Candidate.CANDIDATE_ID),
                                        STATUS_ID = i.Candidate.STATUS,
                                        CREATED_ON = i.Candidate.CREATED_ON,
                                        MODIFIED_ON = i.StsHist.SCHEDULED_TO 
                                    }).ToList();
                
                return Canlist;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string GetPartnerName(string pId)
        {
            var vendor = (from u in db.AspNetUsers.Where(i => i.Id == pId)
                          join v in db.VENDOR_MASTER on u.Vendor_Id equals v.VENDOR_ID
                          select v.VENDOR_NAME).FirstOrDefault();
            return vendor.ToString();
        }


        private List<CandidateViewModels> GetPagination(List<CandidateViewModels> jobCanObj, string sOdr, int? page)
        {
            ViewBag.CurrentSort = sOdr;

            if (jobCanObj != null && jobCanObj.Count > 0)
            {
                ViewBag.CNameSort = string.IsNullOrEmpty(sOdr) ? "Name_desc" : "";
                ViewBag.StatusSort = sOdr == "Sts_desc" ? "Sts_asc" : "Sts_desc";
                ViewBag.SkillSort = sOdr == "Skill_desc" ? "Skill_asc" : "Skill_desc";
                
                switch (sOdr)
                {
                    case "Name_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.CANDIDATE_NAME).ToList();
                        break;
                    case "Sts_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.STATUS).ToList();
                        break;
                    case "Sts_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.STATUS).ToList();
                        break;
                    case "Skill_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.POSITION).ToList();
                        break;
                    case "Skill_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.POSITION).ToList();
                        break;
                    default:
                        jobCanObj = jobCanObj.OrderBy(s => s.CANDIDATE_NAME).ToList();
                        break;
                }
            }

            ViewBag.PageSize = pageSize;
            ViewBag.PageNo = (page ?? 1);
            return jobCanObj;
        }
        #endregion

        protected override void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;
            //Log Exception e to DB.
            if (!filterContext.ExceptionHandled)
            {
                LoggingUtil.LogException(e, errorLevel: ErrorLevel.Critical);
                filterContext.ExceptionHandled = true;
            }
            //filterContext.Result = new ViewResult()
            //{
            //    ViewName = "Error"
            //};
        }
    }
}