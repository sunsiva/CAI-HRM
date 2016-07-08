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
using PagedList;
using HRPortal.Common;
using HRPortal.Common.Enums;
using Microsoft.AspNet.Identity;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    public class JobController : Controller
    {
        private HRPortalEntities dbContext = new HRPortalEntities();
        const int pageSize = 10;

        // GET: Job
        public async Task<ActionResult> Index(string sOdr, int? page)
        {
            var jobLst = await dbContext.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            jobLst = GetPagination(jobLst, sOdr, page);
            ViewBag.TotalRecord = jobLst.Count();
            int pSize = ViewBag.PageSize == null ? 0 : ViewBag.PageSize;
            int pNo = ViewBag.PageNo == null ? 0 : ViewBag.PageNo;
            return View(jobLst.ToPagedList(pNo, pSize));
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
                          where j.JOB_ID == id
                          select new {jd=j.JD_FILE_PATH }).FirstOrDefault();
            var userLst = dbContext.AspNetUsers.ToList();
            jOBPOSTING.CREATED_BY = userLst.Where(u => u.Id == jOBPOSTING.CREATED_BY).Select(um => um.FirstName + " " + um.LastName).FirstOrDefault();
            jOBPOSTING.MODIFIED_BY = userLst.Where(u => u.Id == jOBPOSTING.MODIFIED_BY).Select(um => um.FirstName + " " + um.LastName).FirstOrDefault();

            jOBPOSTING.JD_FILE_PATH = string.IsNullOrEmpty(owner.jd) ? string.Empty : Path.Combine("/UploadDocument/", owner.jd);
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create([Bind(Include = "JOB_ID,JOB_CODE,JOB_DESCRIPTION,POSITION_NAME,NO_OF_VACANCIES,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,CLOSE_DATE,ISIMMEDIATEPOSITION,WORK_LOCATION,CUSTOMER_NAME,COMMENTS,JD_FILE_PATH,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON")] JOBPOSTING jOBPOSTING, HttpPostedFileBase file)
        {
            try { 
            if (ModelState.IsValid)
            {
                jOBPOSTING.JOB_ID = Guid.NewGuid();
                jOBPOSTING.JOB_CODE = GetAutoJobCode(jOBPOSTING.POSITION_NAME.ToUpper());
                jOBPOSTING.ISACTIVE = true;
                    jOBPOSTING.JD_FILE_PATH = FileUpload(file);
                    jOBPOSTING.CREATED_BY = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
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
        [Authorize(Roles = "Admin")]
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
        public async Task<ActionResult> Edit([Bind(Include = "JOB_ID,JOB_CODE,JOB_DESCRIPTION,POSITION_NAME,NO_OF_VACANCIES,YEARS_OF_EXP_TOTAL,YEARS_OF_EXP_RELEVANT,CLOSE_DATE,ISIMMEDIATEPOSITION,WORK_LOCATION,CUSTOMER_NAME,COMMENTS,JD_FILE_PATH,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_ON,CREATED_BY")] JOBPOSTING jOBPOSTING, HttpPostedFileBase file, FormCollection frm)
        {
            try { 
            if (ModelState.IsValid)
            {
                    jOBPOSTING.MODIFIED_BY = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                    jOBPOSTING.MODIFIED_ON = DateTime.Now;
                    jOBPOSTING.JD_FILE_PATH = (file == null ? frm["JD_FILE_PATH"] : FileUpload(file));
                    dbContext.Entry(jOBPOSTING).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(jOBPOSTING);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Job/Delete/5
        [Authorize(Roles = "Admin")]
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
        public async Task<ActionResult> DeleteConfirmed(Guid id, JOBPOSTING model)
        {
            try { 
            JOBPOSTING jOBPOSTING = await dbContext.JOBPOSTINGs.FindAsync(id);
                string _uid = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
            if (jOBPOSTING == null)
            {
                return HttpNotFound();
            }
            else {
                    //dbContext.JOBPOSTINGs.Remove(jOBPOSTING);
                    jOBPOSTING.MODIFIED_BY = _uid;
                    jOBPOSTING.MODIFIED_ON = DateTime.Now;
                    jOBPOSTING.ISACTIVE = false;
                    dbContext.Entry(jOBPOSTING).State = EntityState.Modified;
                    await dbContext.SaveChangesAsync();

                    JOB_HISTORY jobhist = new JOB_HISTORY();
                    jobhist.JOB_HIST_ID = Guid.NewGuid();
                    jobhist.JOB_ID = id;
                    jobhist.JOB_COMMENTS = model.COMMENTS;
                    jobhist.IS_ACTIVE = false;
                    jobhist.CREATED_BY = string.IsNullOrEmpty(_uid) ? User.Identity.Name : _uid;
                    jobhist.CREATED_ON = DateTime.Now;
                    dbContext.JOB_HISTORY.Add(jobhist);
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
            else if (posName.Contains("SAP"))
                obj = "SAP";
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

        private List<JOBPOSTING> GetPagination(List<JOBPOSTING> jobCanObj, string sOdr, int? page)
        {
            ViewBag.CurrentSort = sOdr;

            if (jobCanObj != null && jobCanObj.Count > 0)
            {
                ViewBag.JCodeSort = string.IsNullOrEmpty(sOdr) ? "JCode_desc" : "";
                ViewBag.PosSort = sOdr == "Pos_desc" ? "Pos_asc" : "Pos_desc";
                ViewBag.CustSort = sOdr == "Cust_desc" ? "Cust_asc" : "Cust_desc";
                switch (sOdr)
                {
                    case "JCode_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.JOB_CODE).ToList();
                        break;
                    case "Pos_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.POSITION_NAME).ToList();
                        break;
                    case "Pos_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.POSITION_NAME).ToList();
                        break;
                    case "Cust_desc":
                        jobCanObj = jobCanObj.OrderByDescending(s => s.CUSTOMER_NAME).ToList();
                        break;
                    case "Cust_asc":
                        jobCanObj = jobCanObj.OrderBy(s => s.CUSTOMER_NAME).ToList();
                        break;
                    default:
                        jobCanObj = jobCanObj.OrderBy(s => s.JOB_CODE).ToList();
                        break;
                }
            }

            ViewBag.PageSize = pageSize;
            ViewBag.PageNo = (page ?? 1);
            return jobCanObj;
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
