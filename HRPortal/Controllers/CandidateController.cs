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

namespace HRPortal.Controllers
{
    public class CandidateController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private CandidateViewModels vmodel = new CandidateViewModels();
        private LoginViewModel logvmodel = new LoginViewModel();
        private string _uid;

        public CandidateController()
        {
            _uid = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
        }

        // GET: Candidate
        public async Task<ActionResult> Index()
        {
            //bool isAdmin = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN");
            List<CANDIDATE> canDb = new List<CANDIDATE>();
            //if (isAdmin) {
                canDb = await db.CANDIDATES.Where(c => c.CREATED_BY == _uid && c.ISACTIVE == true).ToListAsync();
            //} else { 
            //    canDb = await db.CANDIDATES.Where(c => c.ISACTIVE == true).ToListAsync();
            //}

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

            return View(canLst);
        }

        // GET: Squads Candidates
        public async Task<ActionResult> SquadJobs()
        {
            var vendorId = Guid.Parse(HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.VendorId.ToString())));
            var squadsLst = await db.CANDIDATES.ToListAsync();
            var usrs = await db.AspNetUsers.Where(i => i.Vendor_Id == vendorId && i.Id != _uid).Select(s=>s.Id).ToListAsync();
            var canLst = squadsLst.Where(c => usrs.Contains(c.CREATED_BY) && c.ISACTIVE == true).Select(i => new CandidateViewModels
                {
                    CANDIDATE_ID = i.CANDIDATE_ID,
                    CANDIDATE_NAME = i.CANDIDATE_NAME,
                    MOBILE_NO = i.MOBILE_NO,
                    EMAIL = i.EMAIL,
                    CURRENT_COMPANY=i.CURRENT_COMPANY,
                    NOTICE_PERIOD = i.NOTICE_PERIOD,
                    YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                    LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                    STATUS_ID = i.STATUS,
                    STATUS = vmodel.GetStatusNameById(i.CANDIDATE_ID),
                    CREATED_BY = logvmodel.GetUserNameById(i.CREATED_BY),
                }).ToList();
            return View(canLst);
        }
        
        // GET: Candidate/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
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
                    cANDIDATE.CREATED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
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
        
        public async Task<ActionResult> ScheduleCandidate(string id, string date, string comments,string statusId)
        {
            var stsLst = db.STATUS_MASTER.ToList();
            var sOrdr = stsLst.Where(i => i.STATUS_ID == Guid.Parse(statusId)).FirstOrDefault();
            int stsOrdr = sOrdr.STATUS_NAME.Contains("TBS-F") ? 2 : 1;
            var stsId = stsLst.Where(i => i.STATUS_ORDER == sOrdr.STATUS_ORDER + stsOrdr).FirstOrDefault();

            STATUS_HISTORY sHist = new STATUS_HISTORY();
            sHist.STATUS_ID = stsId.STATUS_ID;
            sHist.CANDIDATE_ID= Guid.Parse(id);
            sHist.ISACTIVE = true;
            sHist.SCHEDULED_TO = Convert.ToDateTime(date);
            sHist.COMMENTS = comments;
            sHist.MODIFIED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
            sHist.MODIFIED_ON = DateTime.Now;
            db.STATUS_HISTORY.Add(sHist);
            await db.SaveChangesAsync();
            
            return Json(stsId.STATUS_DESCRIPTION.ToString(), JsonRequestBehavior.AllowGet);
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
        public async Task<ActionResult> Edit([Bind(Include = "CANDIDATE_ID,JOB_ID,CANDIDATE_NAME,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,MOBILE_NO,ALTERNATE_MOBILE_NO,EMAIL,ALTERNATE_EMAIL_ID,DOB,CURRENT_COMPANY,CURRENT_LOCATION,NOTICE_PERIOD,COMMENTS,ISINNOTICEPERIOD,ISACTIVE,MODIFIED_BY,CREATED_ON,CREATED_BY")] CANDIDATE cANDIDATE, FormCollection frm)
        {
            if (ModelState.IsValid)
            {
                cANDIDATE.ISINNOTICEPERIOD = (!string.IsNullOrEmpty(frm["IsNP"]) && frm["IsNP"] == "Yes") ? true : false;
                cANDIDATE.NOTICE_PERIOD = string.IsNullOrEmpty(frm["ddlNoticePeriod"]) ? "0" : frm["ddlNoticePeriod"].ToString();
                cANDIDATE.MODIFIED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
                cANDIDATE.MODIFIED_ON = DateTime.Now;
                db.Entry(cANDIDATE).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(cANDIDATE);
        }

        // GET: Candidate/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
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

        // POST: Candidate/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            CANDIDATE cANDIDATE = await db.CANDIDATES.FindAsync(id);
            db.CANDIDATES.Remove(cANDIDATE);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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
    }
}
