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
using HRPortal.Models;
using System.IO;
using HRPortal.Helper;

namespace HRPortal.Controllers
{
    public class JobController : Controller
    {

        private HRPortalEntities dbContext = new HRPortalEntities();

        // GET: Job
        public async Task<ActionResult> Index()
        {
            return View(await dbContext.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync());
        }

        // GET: Job/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JOBPOSTING jOBPOSTING = await dbContext.JOBPOSTINGs.FindAsync(id);
            var owner = (from j in dbContext.JOBPOSTINGs.ToList()
                          join u in dbContext.AspNetUsers.ToList() on j.CREATED_BY equals u.Id
                          where j.JOB_ID == id
                          select u.FirstName+" "+u.LastName).FirstOrDefault();
            jOBPOSTING.CREATED_BY = owner.ToString();
            if (jOBPOSTING == null)
            {
                return HttpNotFound();
            }
            return View(jOBPOSTING);
        }

        // GET: Job/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Job/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "JOB_ID,JOB_CODE,JOB_DESCRIPTION,POSITION_NAME,NO_OF_VACANCIES,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,CLOSE_DATE,ISIMMEDIATEPOSITION,WORK_LOCATION,CUSTOMER_NAME,COMMENTS,JD_FILE_PATH,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON")] JOBPOSTING jOBPOSTING)
        {
            try { 
            if (ModelState.IsValid)
            {
                jOBPOSTING.JOB_ID = Guid.NewGuid();
                jOBPOSTING.JOB_CODE = GetAutoJobCode(jOBPOSTING.POSITION_NAME.ToUpper());
                jOBPOSTING.ISACTIVE = true;
                jOBPOSTING.CREATED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
                jOBPOSTING.CREATED_ON = DateTime.Now;
                dbContext.JOBPOSTINGs.Add(jOBPOSTING);
                await dbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(jOBPOSTING);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Job/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JOBPOSTING jOBPOSTING = await dbContext.JOBPOSTINGs.FindAsync(id);
            if (jOBPOSTING == null)
            {
                return HttpNotFound();
            }
            return View(jOBPOSTING);
        }

        // POST: Job/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "JOB_ID,JOB_CODE,JOB_DESCRIPTION,POSITION_NAME,NO_OF_VACANCIES,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,CLOSE_DATE,ISIMMEDIATEPOSITION,WORK_LOCATION,CUSTOMER_NAME,COMMENTS,JD_FILE_PATH,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_ON,CREATED_BY")] JOBPOSTING jOBPOSTING)
        {
            try { 
            if (ModelState.IsValid)
            {
                jOBPOSTING.MODIFIED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
                jOBPOSTING.MODIFIED_ON = DateTime.Now;
                dbContext.Entry(jOBPOSTING).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(jOBPOSTING);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Job/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            JOBPOSTING jOBPOSTING = await dbContext.JOBPOSTINGs.FindAsync(id);
            if (jOBPOSTING == null)
            {
                return HttpNotFound();
            }
            return View(jOBPOSTING);
        }

        // POST: Job/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            try { 
            JOBPOSTING jOBPOSTING = await dbContext.JOBPOSTINGs.FindAsync(id);
            if (jOBPOSTING == null)
            {
                return HttpNotFound();
            }
            else {
                dbContext.JOBPOSTINGs.Remove(jOBPOSTING);
                await dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Index");
            }
            catch (Exception ex) { throw ex; }
        }

        //// POST: Job/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public JsonResult JsonDeleteConfirmed(Guid id)
        //{
        //    JOBPOSTING jOBPOSTING = db.JOBPOSTINGs.Find(id);
        //    if (jOBPOSTING == null)
        //    {
        //        return Json("not ok");
        //    }
        //    else {
        //        db.JOBPOSTINGs.Remove(jOBPOSTING);
        //        db.SaveChangesAsync();
        //    }
        //    return Json("ok");
        //}

        public async Task<ActionResult> ExportToExcel()
        {
            System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            JobAndCandidateViewModels obj = new JobAndCandidateViewModels();
            var dbJobs = await dbContext.JOBPOSTINGs.ToListAsync();
            gv.DataSource = dbJobs.Select(i => new
                             {
                                JobCode = i.JOB_CODE,
                                JobDescription = i.JOB_DESCRIPTION,
                                Position = i.POSITION_NAME,
                                TotalExp = i.YEARS_OF_EXP_TOTAL,
                                WorkLocation = i.WORK_LOCATION,
                                NoOfVacancies = i.NO_OF_VACANCIES,
                                Comments = i.COMMENTS,
                                Status=i.ISACTIVE == true ? "Active" : "In-Active",
                                PublishedOn = i.CREATED_ON
                             }).ToList();
            gv.DataBind();
            Response.ClearContent();
            Response.Buffer = true;
            string fileName = "Jobs_" + DateTime.Now.Day + DateTime.Now.ToString("MMM") + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".xls";
            Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            StringWriter sw = new StringWriter();
            System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
            gv.RenderControl(htw);
            // Response.WriteFile("CandidateSearchList");
            Response.Output.Write(sw.ToString());
            Response.Flush();
            Response.End();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Generating Random number.
        /// </summary>
        /// <param name="posName"></param>
        /// <returns></returns>
        private string GetAutoJobCode(string posName)
        {
            string obj = string.Empty;
            Random rnd = new Random();
            posName = posName.ToUpper();
            if (posName.Contains("JAVA")) //TODO:has to be changed with switch case.
                obj = "JVA";
            else if(posName.Contains("ASP"))
                obj = "ASP";
            else if (posName.Contains("SQL"))
                obj = "SQL";
            else if (posName.Contains("CONTENT"))
                obj = "CTN";
            else if (posName.Contains("MOBILE"))
                obj = "MBL";
            else
                obj = "GNL";
            obj = obj + rnd.Next().ToString();
            return obj.Remove(7);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
