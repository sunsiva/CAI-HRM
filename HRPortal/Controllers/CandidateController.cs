using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.IO;
using HRPortal.Helper;
using HRPortal.Models;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using System.Net.Mail;
using System.Net.Mime;
using HRPortal.Common;
using HRPortal.Common.Enums;
using PagedList;

namespace HRPortal.Controllers
{
    public class CandidateController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private CandidateViewModels vmodel = new CandidateViewModels();
        private LoginViewModel logvmodel = new LoginViewModel();
        private AppointmentViewModels appointmentVM = new AppointmentViewModels();
        const int pageSize = 10;

        // GET: Candidate
        public async Task<ActionResult> Index(string sOdr, int? page)
        {
            try
            {
                
                List<CANDIDATE> canDb = new List<CANDIDATE>();
                var uid = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                canDb = await db.CANDIDATES.Where(c => c.CREATED_BY == uid && c.ISACTIVE == true).ToListAsync();

                var canLst = canDb.Select(i => new CandidateViewModels
                {
                    CANDIDATE_ID = i.CANDIDATE_ID,
                    CANDIDATE_NAME = i.CANDIDATE_NAME,
                    MOBILE_NO = i.MOBILE_NO,
                    EMAIL = i.EMAIL,
                    CURRENT_COMPANY = i.CURRENT_COMPANY,
                    NOTICE_PERIOD = i.NOTICE_PERIOD,
                    YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                    LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                    STATUS_ID = i.STATUS,
                    STATUS = vmodel.GetStatusNameById(i.CANDIDATE_ID),
                }).ToList();

                canLst = canDb != null? GetPagination(canLst, sOdr, page): canLst;

                int pSize = ViewBag.PageSize == null ? 0 : ViewBag.PageSize;
                int pNo = ViewBag.PageNo == null ? 0 : ViewBag.PageNo;

                return View(canLst.ToPagedList(pNo, pSize));
            }
            catch(Exception ex) { throw ex; }
        }

        // GET: Squads Candidates
        public async Task<ActionResult> SquadJobs(string sOdr, int? page)
        {
            try
            {
                var uid = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                var vendorId = Guid.Parse(HelperFuntions.HasValue(Session[CacheKey.VendorId.ToString()]));
                var squadsLst = await db.CANDIDATES.ToListAsync();
                var usrs = await db.AspNetUsers.Where(i => i.Vendor_Id == vendorId && i.Id != uid).Select(s => s.Id).ToListAsync();
                var canLst = squadsLst.Where(c => usrs.Contains(c.CREATED_BY) && c.ISACTIVE == true).Select(i => new CandidateViewModels
                {
                    CANDIDATE_ID = i.CANDIDATE_ID,
                    CANDIDATE_NAME = i.CANDIDATE_NAME,
                    MOBILE_NO = i.MOBILE_NO,
                    EMAIL = i.EMAIL,
                    CURRENT_COMPANY = i.CURRENT_COMPANY,
                    NOTICE_PERIOD = i.NOTICE_PERIOD,
                    YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                    LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                    STATUS_ID = i.STATUS,
                    STATUS = vmodel.GetStatusNameById(i.CANDIDATE_ID),
                    CREATED_ON = i.CREATED_ON,
                    CREATED_BY = logvmodel.GetUserNameById(i.CREATED_BY),
                }).ToList();

                canLst = squadsLst != null ? GetPagination(canLst, sOdr, page) : canLst;

                int pSize = ViewBag.PageSize == null ? 0 : ViewBag.PageSize;
                int pNo = ViewBag.PageNo == null ? 0 : ViewBag.PageNo;

                return View(canLst.ToPagedList(pNo, pSize));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        // GET: Candidate/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            try {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                CANDIDATE cANDIDATE = await db.CANDIDATES.FindAsync(id);
                var owner = (from j in db.CANDIDATES.ToList()
                             join u in db.AspNetUsers.ToList() on j.CREATED_BY equals u.Id
                             where j.CANDIDATE_ID == id
                             select u.FirstName + " " + u.LastName).FirstOrDefault();
                cANDIDATE.CREATED_BY = owner.ToString();
                if (cANDIDATE == null)
                {
                    return HttpNotFound();
                }
                return View(cANDIDATE);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Candidate/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Candidate/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "CANDIDATE_NAME,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,MOBILE_NO,ALTERNATE_MOBILE_NO,EMAIL,ALTERNATE_EMAIL_ID,DOB,CURRENT_COMPANY,CURRENT_LOCATION,NOTICE_PERIOD,COMMENTS,ISINNOTICEPERIOD,LAST_WORKING_DATE")] CANDIDATE cANDIDATE, HttpPostedFileBase file,FormCollection frm)
        {
            var jid = Request.UrlReferrer.Query.ToString().Split('&').Count()>1? Request.UrlReferrer.Query.ToString().Split('&')[1]: cANDIDATE.JOB_ID.ToString();
            if (IsCandidateDuplicate(cANDIDATE.MOBILE_NO, cANDIDATE.DOB.ToShortDateString()))
            {
                ViewBag.IsExist = "Candidate is already exist.";
            }
            else { 
            if (ModelState.IsValid)
            {
                    cANDIDATE.CANDIDATE_ID = Guid.NewGuid();
                    cANDIDATE.JOB_ID = new Guid(jid.Replace("JID=",string.Empty));
                    cANDIDATE.ISACTIVE = true;
                    cANDIDATE.ISINNOTICEPERIOD = (!string.IsNullOrEmpty(frm["IsNP"]) && frm["IsNP"] == "Yes")?true:false;
                    cANDIDATE.NOTICE_PERIOD = string.IsNullOrEmpty(frm["ddlNoticePeriod"]) ? "0" : frm["ddlNoticePeriod"].ToString();
                    cANDIDATE.CREATED_BY = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                    cANDIDATE.CREATED_ON = DateTime.Now;
                    cANDIDATE.STATUS = db.STATUS_MASTER.Where(i => i.STATUS_ORDER == 1).FirstOrDefault().STATUS_ID.ToString();
                    cANDIDATE.RESUME_FILE_PATH = FileUpload(file);
                    db.CANDIDATES.Add(cANDIDATE);
                    await db.SaveChangesAsync();

                vmodel.UpdateStatus(Guid.Empty, cANDIDATE.CANDIDATE_ID, string.Empty);
                return RedirectToAction("Index");
            }
            }
            ModelState.AddModelError("FILD", "Failed to Insert");
            return View(cANDIDATE);
        }
        
        public async Task<ActionResult> ScheduleCandidate(string id, string date,string length,string sendTo, string comments,string statusId)
        {
            try { 
                var stsLst = db.STATUS_MASTER.ToList();
                var sOrdr = stsLst.Where(i => i.STATUS_ID == Guid.Parse(statusId)).FirstOrDefault();
                int stsOrdr = sOrdr.STATUS_NAME.Contains("TBS-F") ? 2 : 1;
                var stsId = stsLst.Where(i => i.STATUS_ORDER == sOrdr.STATUS_ORDER + stsOrdr).FirstOrDefault();

                STATUS_HISTORY sHist = new STATUS_HISTORY();
                sHist.STATUS_ID = stsId.STATUS_ID;
                sHist.CANDIDATE_ID = Guid.Parse(id);
                sHist.ISACTIVE = true;
                sHist.SCHEDULED_TO = Convert.ToDateTime(date);
                sHist.SCHEDULE_LENGTH_MINS = int.Parse(length);
                sHist.COMMENTS = comments;
                sHist.MODIFIED_BY = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                sHist.MODIFIED_ON = DateTime.Now;
                db.STATUS_HISTORY.Add(sHist);
                await db.SaveChangesAsync();

                if (System.Configuration.ConfigurationManager.AppSettings["IsAppointmentMail"] == "true")
                {
                    await appointmentVM.SendInvite(date, length, sendTo, Guid.Parse(id));
                }

                return Json(stsId.STATUS_DESCRIPTION.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { throw ex; }
        }
        
        private string FileUpload(HttpPostedFileBase file)
        {
            string filename = string.Empty;
            if (file != null && file.ContentLength > 0)
                try
                {
                    filename = Path.GetFileName(file.FileName); //TODO:filename should be with vendorname appended.
                    string path = Path.Combine(Server.MapPath("~/UploadDocument"), filename);
                    file.SaveAs(path);
                    ViewBag.FileMessage = "File uploaded successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.FileMessage = "ERROR:" + ex.Message.ToString();
                    throw ex;
                }
            else
            {
                ViewBag.FileMessage = "You have not specified a file.";
            }
            return filename;
        }

        // GET: Candidate/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CANDIDATE cANDIDATE = await db.CANDIDATES.FindAsync(id);
            if (cANDIDATE == null)
            {
                return HttpNotFound();
            }
            return View(cANDIDATE);
        }

        // POST: Candidate/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CANDIDATE_ID,JOB_ID,CANDIDATE_NAME,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,MOBILE_NO,ALTERNATE_MOBILE_NO,EMAIL,ALTERNATE_EMAIL_ID,DOB,CURRENT_COMPANY,CURRENT_LOCATION,NOTICE_PERIOD,COMMENTS,ISINNOTICEPERIOD,ISACTIVE,MODIFIED_BY,CREATED_ON,CREATED_BY")] CANDIDATE cANDIDATE, HttpPostedFileBase file, FormCollection frm)
        {
            try { 
            if (ModelState.IsValid)
            {
                cANDIDATE.ISINNOTICEPERIOD = (!string.IsNullOrEmpty(frm["IsNP"]) && frm["IsNP"] == "Yes") ? true : false;
                cANDIDATE.NOTICE_PERIOD = string.IsNullOrEmpty(frm["ddlNoticePeriod"]) ? "0" : frm["ddlNoticePeriod"].ToString();
                cANDIDATE.MODIFIED_BY = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                cANDIDATE.MODIFIED_ON = DateTime.Now;
                cANDIDATE.LAST_WORKING_DATE = string.IsNullOrEmpty(frm["LAST_WORKING_DATE"])? cANDIDATE.LAST_WORKING_DATE : DateTime.Parse(frm["LAST_WORKING_DATE"]);
                cANDIDATE.RESUME_FILE_PATH = (file==null? frm["RESUME_FILE_PATH"]: FileUpload(file));
                db.Entry(cANDIDATE).State = EntityState.Modified;
                await db.SaveChangesAsync();
                if(Request.UrlReferrer.Query.ToString()== "?styp=S")
                    return RedirectToAction("SquadJobs");
                else
                    return RedirectToAction("Index"); 
                }
            return View(cANDIDATE);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Candidate/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            try { 
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CANDIDATE cANDIDATE = await db.CANDIDATES.FindAsync(id);
            if (cANDIDATE == null)
            {
                return HttpNotFound();
            }
            return View(cANDIDATE);
            }
            catch (Exception ex) { throw ex; }
        }

        // POST: Candidate/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            try { 
            CANDIDATE cANDIDATE = await db.CANDIDATES.FindAsync(id);
                //db.CANDIDATES.Remove(cANDIDATE);
                cANDIDATE.COMMENTS = "";
                cANDIDATE.ISACTIVE = false;
                db.Entry(cANDIDATE).State = EntityState.Modified;
                await db.SaveChangesAsync();
            return RedirectToAction("Index","Home");
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Check if Candidates mobile number and DOB are exist.
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="dob"></param>
        /// <returns></returns>
        private bool IsCandidateDuplicate(string mob, string dob)
        {
            var dupli = db.CANDIDATES.Where(u => u.MOBILE_NO == mob).FirstOrDefault();
            bool isDupli = dupli != null && dupli.DOB.ToShortDateString() == dob ? true : false;
            return isDupli;
        }

        //private string GetErrMsg(FormCollection frm, HttpPostedFileBase file)
        //{
        //    string errMsg = string.Empty;
        //    if (frm["ISINNOTICEPERIOD"] == "Yes" && string.IsNullOrEmpty(frm["LAST_WORKING_DATE"]))
        //        errMsg = "Last working date is required.";
        //    else if (frm["ISINNOTICEPERIOD"] == "No" && string.IsNullOrEmpty(frm["NOTICE_PERIOD"]))
        //        errMsg = "Notice Period is required.";
        //    else if (file == null)
        //        errMsg = "Please upload a resume.";
        //    else
        //        errMsg = "Required field is empty.";
        //    return errMsg;
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private List<CandidateViewModels> GetPagination(List<CandidateViewModels> jobCanObj, string sOdr, int? page)
        {
            ViewBag.CurrentSort = sOdr;

            if (jobCanObj != null && jobCanObj.Count > 0)
            {
                ViewBag.CNameSort = string.IsNullOrEmpty(sOdr) ? "Name_desc" : "";
                ViewBag.EmailSort = sOdr == "EMail_desc" ? "EMail_asc" : "EMail_desc";
                ViewBag.StatusSort = sOdr == "Sts_desc" ? "Sts_asc" : "Sts_desc";

                switch (sOdr)
                {
                    case "Name_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.CANDIDATE_NAME).ToList();
                        break;
                    case "EMail_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.EMAIL).ToList();
                        break;
                    case "EMail_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.EMAIL).ToList();
                        break;
                    case "Sts_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.STATUS).ToList();
                        break;
                    case "Sts_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.STATUS).ToList();
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

        public async Task<ActionResult> ExportToExcel()
        {
            try
            {
                System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
                List<CANDIDATE> canDb = new List<CANDIDATE>();
                var uid = HelperFuntions.HasValue(Session[CacheKey.Uid.ToString()]);
                canDb = await db.CANDIDATES.Where(c => c.CREATED_BY == uid && c.ISACTIVE == true).ToListAsync();

                var canLst = canDb.Select(i => new 
                {
                    CANDIDATE_NAME = i.CANDIDATE_NAME,
                    MOBILE_NO = i.MOBILE_NO,
                    EMAIL = i.EMAIL,
                    CURRENT_COMPANY = i.CURRENT_COMPANY,
                    NOTICE_PERIOD = i.NOTICE_PERIOD,
                    YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                    LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                    STATUS = vmodel.GetStatusNameById(i.CANDIDATE_ID),
                }).ToList();

                gv.DataSource = canLst;
                gv.DataBind();
                Response.ClearContent();
                Response.Buffer = true;
                string fileName = "Candidates_" + DateTime.Now.Day + DateTime.Now.ToString("MMM") + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".xls";
                Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
                Response.ContentType = "application/ms-excel";
                Response.Charset = "";
                StringWriter sw = new StringWriter();
                System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
                gv.RenderControl(htw);
                Response.Output.Write(sw.ToString());
                Response.Flush();
                Response.End();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) { throw ex; }
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;
            //Log Exception e to DB.
            if (!filterContext.ExceptionHandled)
            {
                LoggingUtil.LogException(e, errorLevel: ErrorLevel.Critical);
            }
            //filterContext.Result = new ViewResult()
            //{
            //    ViewName = "Error"
            //};
        }
    }
}
