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
using HRPortal.Models;

namespace HRPortal.Controllers
{
    public class CandidateController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();

        // GET: Candidate
        public async Task<ActionResult> Index()
        {
            return View(await db.CANDIDATES.ToListAsync());
        }

        // GET: Candidate/Details/5
        public async Task<ActionResult> Details(Guid? id)
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
        public async Task<ActionResult> Create([Bind(Include = "CANDIDATE_ID,CANDIDATE_NAME,JOB_ID,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,MOBILE_NO,ALTERNATE_MOBILE_NO,EMAIL,ALTERNATE_EMAIL_ID,DOB,CURRENT_COMPANY,NOTICE_PERIOD,COMMENTS,ISINNOTICEPERIOD,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON,File")] CANDIDATE cANDIDATE)
        {
            /*BEGIN: FILE UPLOAD */
            //var fil = Request.Files;
            //byte[] uploadedFile = new byte[cANDIDATE.File.InputStream.Length];
            /*END*/
            if (IsCandidateDuplicate(cANDIDATE.MOBILE_NO, cANDIDATE.DOB.ToShortDateString()))
            {
                ViewBag.IsExist = "Candidate is already exist.";
            }
            else { 
            if (ModelState.IsValid)
            {
                cANDIDATE.CANDIDATE_ID = Guid.NewGuid();
                cANDIDATE.ISACTIVE = true;
                cANDIDATE.CREATED_BY = Guid.NewGuid().ToString();
                cANDIDATE.CREATED_ON = DateTime.Now;
                db.CANDIDATES.Add(cANDIDATE);
               await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            }
            return View(cANDIDATE);
        }

        [HttpGet]
        public EmptyResult _FileUpload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
                try
                {
                    string path = Path.Combine(Server.MapPath("~/UploadDocument"),
                                               Path.GetFileName(file.FileName));
                    file.SaveAs(path);
                    ViewBag.Message = "File uploaded successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                }
            else
            {
                ViewBag.Message = "You have not specified a file.";
            }
            return null;
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
        public async Task<ActionResult> Edit([Bind(Include = "CANDIDATE_ID,CANDIDATE_NAME,JOB_ID,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,MOBILE_NO,ALTERNATE_MOBILE_NO,EMAIL,ALTERNATE_EMAIL_ID,DOB,CURRENT_COMPANY,NOTICE_PERIOD,COMMENTS,ISINNOTICEPERIOD,ISACTIVE,MODIFIED_BY,CREATED_ON,CREATED_BY")] CANDIDATE cANDIDATE)
        {
            if (ModelState.IsValid)
            {
                cANDIDATE.MODIFIED_BY = Session["EMail"]!=null? Session["EMail"].ToString():User.Identity.Name;// Guid.NewGuid().ToString();
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
