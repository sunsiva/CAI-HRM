using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Web.Mvc;
using System.Net;
using System.Threading.Tasks;
using System.Data.Entity;
using HRPortal.Models;
using System.IO;
using HRPortal.Helper;

namespace HRPortal.Controllers
{
    public class HomeController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        LoginViewModel loginVM = new LoginViewModel();
        JobAndCandidateViewModels jobCanObj = new JobAndCandidateViewModels();
        public async Task<ActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if(HttpRuntime.Cache.Get("user") == null)
                    loginVM.SetUserToCache(User.Identity.Name);
                var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
                if (HelperFuntions.HasValue(HttpRuntime.Cache.Get("rolename")).ToUpper().Contains("ADMIN"))
                {
                    var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                    jobCanObj.CandidateItems = (from c in dbCan
                                          join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                          join u in db.AspNetUsers.ToList() on c.MODIFIED_BY equals u.Id into tempUsr
                                          from u in tempUsr.DefaultIfEmpty()
                                          select new { Candidate = c, Job = j, Users = u}).Select(i => new CandidateViewModels
                                          {
                                              CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                              CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                              POSITION = i.Job.POSITION_NAME,
                                              RESUME_FILE_PATH = string.IsNullOrEmpty(i.Candidate.RESUME_FILE_PATH)? string.Empty: Path.Combine("/UploadDocument/", i.Candidate.RESUME_FILE_PATH),
                                              NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                              YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                              LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                              VENDOR_NAME = GetPartnerName(i.Candidate.CREATED_BY),
                                              CREATED_ON = i.Candidate.CREATED_ON,
                                              MODIFIED_ON = i.Candidate.MODIFIED_ON,
                                              MODIFIED_BY = i.Users == null ?string.Empty: i.Users.FirstName + " " + i.Users.LastName,
                                          }).ToList();
                }
                else {
                    jobCanObj.JobItems = dbJobs;
                }
                return View(jobCanObj);
            }
            return RedirectToAction("Login", "Account");
        }

        public async Task<ActionResult> SearchCriteria(string name, string vendor, string status, string stdt, string edt)
        {
            var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            if (HelperFuntions.HasValue(HttpRuntime.Cache.Get("rolename")).ToUpper().Contains("ADMIN"))
            {
                CookieStore.SetCookie("CANSEARCHHOME", name + "|" + vendor + "|" + status + "|" + stdt + "|" + edt, TimeSpan.FromDays(1));
                var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
            }
            else {
                jobCanObj.JobItems = dbJobs;
            }
            return PartialView("_CandidateList", jobCanObj.CandidateItems);
        }

        public async Task<ActionResult> ExportToExcel()
        {
            System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
            var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            gv.DataSource = GetCandidateSearchResults(dbCan, dbJobs).CandidateItems;
            gv.DataBind();
            Response.ClearContent();
            Response.Buffer = true;
            string fileName = "Candidates_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".xls";
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

        private JobAndCandidateViewModels GetCandidateSearchResults(List<CANDIDATE> dbCan, List<JOBPOSTING> dbJobs)
        {
            var cookie = CookieStore.GetCookie("CANSEARCHHOME");
            string[] val = cookie.Split('|');
            string name = val[0], vendor = val[1], status = val[2], stdt = val[3], edt = val[4];

            jobCanObj.CandidateItems = (from c in dbCan
                                  join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                  join u1 in db.AspNetUsers.ToList() on c.CREATED_BY equals u1.Id into tempUsr1
                                  from u1 in tempUsr1.DefaultIfEmpty()
                                  join v in db.VENDOR_MASTER.ToList() on u1.Vendor_Id equals v.VENDOR_ID into tempVendor
                                  from v in tempVendor.DefaultIfEmpty()
                                  join u in db.AspNetUsers.ToList() on c.MODIFIED_BY equals u.Id into tempUsr
                                  from u in tempUsr.DefaultIfEmpty()
                                  where c.CANDIDATE_NAME.ToUpper().Contains(name.ToUpper())
                                  && (status != string.Empty ? c.STATUS == status : true)
                                  && (stdt != string.Empty ? ((Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) >= Convert.ToDateTime(stdt))) : true)
                                  && (edt != string.Empty ? Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) <= Convert.ToDateTime(edt) : true)
                                  && v.VENDOR_NAME.ToUpper().Contains(vendor.ToUpper())
                                  select new { Candidate = c, Job = j, Users = u, Vendor = v }).Select(i => new CandidateViewModels
                                  {
                                      CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                      CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                      POSITION = i.Job.POSITION_NAME,
                                      NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                      YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                      VENDOR_NAME = i.Vendor == null ? string.Empty : i.Vendor.VENDOR_NAME,
                                      LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                      CREATED_ON = i.Candidate.CREATED_ON,
                                      MODIFIED_ON = i.Candidate.MODIFIED_ON,
                                      MODIFIED_BY = i.Users == null ? string.Empty : i.Users.FirstName + " " + i.Users.LastName,
                                  }).ToList();
            return jobCanObj;
        }

        private string GetPartnerName(string pId)
        {
            var vendor = (from u in db.AspNetUsers.Where(i=>i.Id == pId)
                          join v in db.VENDOR_MASTER on u.Vendor_Id equals v.VENDOR_ID
                          select v.VENDOR_NAME).FirstOrDefault();
            return vendor.ToString();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

    }
}