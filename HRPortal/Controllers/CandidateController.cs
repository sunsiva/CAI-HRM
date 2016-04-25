using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HRPortal;
using System.IO;
using HRPortal.Helper;
using HRPortal.Models;

namespace HRPortal.Controllers
{
    public class CandidateController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private CandidateViewModels vmodel = new CandidateViewModels();
        // GET: Candidate
        public async Task<ActionResult> Index()
        {
            var uid = HelperFuntions.HasValue(HttpRuntime.Cache.Get("user"));
            return View(await db.CANDIDATES.Where(i => i.CREATED_BY == uid && i.ISACTIVE == true).ToListAsync());
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
                cANDIDATE.CREATED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get("user"));
                cANDIDATE.CREATED_ON = DateTime.Now;
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
                cANDIDATE.MODIFIED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get("user"));
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
