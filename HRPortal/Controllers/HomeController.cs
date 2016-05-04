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
using PagedList;

namespace HRPortal.Controllers
{
    public class HomeController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        LoginViewModel loginVM = new LoginViewModel();
        JobAndCandidateViewModels jobCanObj = new JobAndCandidateViewModels();
        private CandidateViewModels vmodelCan = new CandidateViewModels();
        const int pageSize = 10;

        public async Task<ActionResult> Index(string sOdr, int? page)
        {
            if (User.Identity.IsAuthenticated)
            {
                CookieStore.ClearCookie(CacheKey.CANSearchHome.ToString());
                if (HttpRuntime.Cache.Get(CacheKey.Uid.ToString()) == null)
                    loginVM.SetUserToCache(User.Identity.Name);

                vmodelCan.AutoUpdateStatus(); //Auto update the status of all the candidates to feedback pending if the due is passed.

                var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
                if (HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
                {
                    var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                    jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
                    ViewBag.StatusList = vmodelCan.GetStatusList();
                }
                else {
                    jobCanObj.JobItems = dbJobs;
                }

                jobCanObj = GetPagination(jobCanObj, sOdr, page);
                return View(jobCanObj);
            }
            return RedirectToAction("Login", "Account");
        }

        public async Task<ActionResult> SearchCriteria(string name, string vendor, string status, string stdt, string edt)
        {
            var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            if (HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
            {
                CookieStore.SetCookie(CacheKey.CANSearchHome.ToString(), name + "|" + vendor + "|" + status + "|" + stdt + "|" + edt, TimeSpan.FromMinutes(2));
                var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
                ViewBag.StatusList = vmodelCan.GetStatusList();
                jobCanObj = GetPagination(jobCanObj, string.Empty, 1);
                return PartialView("_CandidateList", jobCanObj.CandidateItems);
            }
            else {
                CookieStore.SetCookie(CacheKey.JobSearchHome.ToString(), name + "|" + stdt + "|" + edt, TimeSpan.FromMinutes(2));
                jobCanObj.JobItems=dbJobs;
                GetJobSearchResults(dbJobs);
                jobCanObj = GetPagination(jobCanObj, string.Empty, 1);
                return PartialView("_JobList", jobCanObj.JobItems);
            }
        }

        public async Task<ActionResult> ExportToExcel()
        {
            System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
            var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            var srchSrc = GetCandidateSearchResults(dbCan, dbJobs).CandidateItems.ToList();
            var dataSrc = srchSrc.Select(i => new {
                CandidateName= i.CANDIDATE_NAME,
                Partner = i.VENDOR_NAME,
                Position = i.POSITION,
                NoticePeriod=i.NOTICE_PERIOD,
                LastWorkingDay=i.LAST_WORKING_DATE,
                TotalExp = i.YEARS_OF_EXP_TOTAL,
                PublishedOn = i.CREATED_ON,
                LastModifiedBy=i.MODIFIED_BY,
                Status =i.STATUS
            }).ToList();
            gv.DataSource = dataSrc;
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

        /// <summary>
        /// Update the status on change by the position owners
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="comments"></param>
        /// <returns></returns>
        public async Task<ActionResult> StatusUpdate(string id, string status, string comments)
        {
            STATUS_HISTORY sHist = new STATUS_HISTORY();
            sHist.STATUS_ID = Guid.Parse(status);
            sHist.COMMENTS = comments;
            sHist.CANDIDATE_ID = Guid.Parse(id);
            sHist.ISACTIVE = true;
            sHist.MODIFIED_BY = HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
            sHist.MODIFIED_ON = DateTime.Now;
            db.STATUS_HISTORY.Add(sHist);
            await db.SaveChangesAsync();

            var cId = Guid.Parse(id);
            CANDIDATE cANDIDATE = db.CANDIDATES.Where(i => i.CANDIDATE_ID == cId).FirstOrDefault();
            cANDIDATE.STATUS =  status;
            cANDIDATE.MODIFIED_BY= HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
            cANDIDATE.MODIFIED_ON = DateTime.Now;
            db.Entry(cANDIDATE).State = EntityState.Modified;
            await db.SaveChangesAsync();
            
            return new EmptyResult();
        }

        private JobAndCandidateViewModels GetPagination(JobAndCandidateViewModels jobCanObj, string sOdr, int? page)
        {
            ViewBag.CurrentSort = sOdr;

            if (jobCanObj.CandidateItems != null && jobCanObj.CandidateItems.Count > 0) { 
            ViewBag.CNameSort = string.IsNullOrEmpty(sOdr) ? "Name_desc" : "";
            ViewBag.PartnerSort = sOdr == "Partner_desc" ? "Partner_asc" : "Partner_desc";
            ViewBag.PDateSort = sOdr == "SubDate_desc" ? "SubDate_asc" : "SubDate_desc";
            ViewBag.SkillSort = sOdr == "Skill_desc" ? "Skill_asc" : "Skill_desc";
            ViewBag.StatusSort = sOdr == "Sts_desc" ? "Sts_asc" : "Sts_desc";

            switch (sOdr)
            {
                case "Name_desc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderByDescending(s => s.CANDIDATE_NAME).ToList();
                    break;
                case "Partner_desc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderByDescending(s => s.VENDOR_NAME).ToList();
                    break;
                case "Partner_asc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderBy(s => s.VENDOR_NAME).ToList();
                    break;
                case "SubDate_desc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderByDescending(s => s.CREATED_ON).ToList();
                    break;
                case "SubDate_asc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderBy(s => s.CREATED_ON).ToList();
                    break;
                case "Skill_desc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderByDescending(s => s.POSITION).ToList();
                    break;
                case "Skill_asc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderBy(s => s.POSITION).ToList();
                    break;
                case "Sts_desc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderByDescending(s => s.STATUS).ToList();
                    break;
                case "Sts_asc":
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderBy(s => s.STATUS).ToList();
                    break;
                default:
                    jobCanObj.CandidateItems = jobCanObj.CandidateItems.OrderBy(s => s.CANDIDATE_NAME).ToList();
                    break;
            }
            }
            else {
                ViewBag.JCodeSort = string.IsNullOrEmpty(sOdr) ? "JCode_desc" : "";
                ViewBag.PositionSort = sOdr == "Position_desc" ? "Position_asc" : "Position_desc";
                ViewBag.PDateSort = sOdr == "PubDate_desc" ? "PubDate_asc" : "PubDate_desc";

                switch (sOdr)
                {
                    case "JCode_desc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderByDescending(s => s.JOB_CODE).ToList();
                        break;
                    case "Position_desc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderByDescending(s => s.POSITION_NAME).ToList();
                        break;
                    case "Position_asc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderBy(s => s.POSITION_NAME).ToList();
                        break;
                    case "PubDate_desc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderByDescending(s => s.CREATED_ON).ToList();
                        break;
                    case "PubDate_asc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderBy(s => s.CREATED_ON).ToList();
                        break;
                    default:
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderBy(s => s.JOB_CODE).ToList();
                        break;
                }
            }
            ViewBag.PageSize = pageSize;
            ViewBag.PageNo = (page ?? 1);
            return jobCanObj;
        }

        private JobAndCandidateViewModels GetCandidateSearchResults(List<CANDIDATE> dbCan, List<JOBPOSTING> dbJobs)
        {
            var cookie = CookieStore.GetCookie(CacheKey.CANSearchHome.ToString());
            string name = string.Empty, vendor = string.Empty, status = string.Empty, stdt = string.Empty, edt = string.Empty;
            if (!string.IsNullOrEmpty(cookie))
            {
                string[] val = cookie.Split('|');
                name = val[0]; vendor = val[1]; status = val[2]; stdt = val[3]; edt = val[4];
            }
            jobCanObj.CandidateItems = (from c in dbCan
                                        join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                        join u1 in db.AspNetUsers.ToList() on c.CREATED_BY equals u1.Id into tempUsr1
                                        from u1 in tempUsr1.DefaultIfEmpty()
                                        join v in db.VENDOR_MASTER.ToList() on u1.Vendor_Id equals v.VENDOR_ID into tempVendor
                                        from v in tempVendor.DefaultIfEmpty()
                                        where c.CANDIDATE_NAME.ToUpper().Contains(name.ToUpper())
                                        && (status != string.Empty ? c.STATUS == status : true)
                                        && (stdt != string.Empty ? ((Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) >= Convert.ToDateTime(stdt))) : true)
                                        && (edt != string.Empty ? Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) <= Convert.ToDateTime(edt) : true)
                                        && v.VENDOR_NAME.ToUpper().Contains(vendor.ToUpper())
                                        select new { Candidate = c, Job = j }).Select(i => new CandidateViewModels
                                        {
                                            CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                            CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                            POSITION = i.Job.POSITION_NAME,
                                            RESUME_FILE_PATH = string.IsNullOrEmpty(i.Candidate.RESUME_FILE_PATH) ? string.Empty : Path.Combine("/UploadDocument/", i.Candidate.RESUME_FILE_PATH),
                                            NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                            YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                            LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                            VENDOR_NAME = GetPartnerName(i.Candidate.CREATED_BY),
                                            STATUS = vmodelCan.GetStatusNameById(i.Candidate.CANDIDATE_ID),
                                            CREATED_ON = i.Candidate.CREATED_ON,
                                            MODIFIED_ON = i.Candidate.MODIFIED_ON,
                                            MODIFIED_BY = GetModifiedById(i.Candidate),
                                        }).ToList();
            return jobCanObj;
        }

        private JobAndCandidateViewModels GetJobSearchResults(List<JOBPOSTING> dbJobs)
        {
            var cookie = CookieStore.GetCookie(CacheKey.JobSearchHome.ToString());
            string name = string.Empty, stdt = string.Empty, edt = string.Empty;
            if (!string.IsNullOrEmpty(cookie))
            {
                string[] val = cookie.Split('|');
                name = val[0]; stdt = val[1]; edt = val[2];
            }
            jobCanObj.JobItems = (from j in dbJobs
                                        join u1 in db.AspNetUsers.ToList() on j.CREATED_BY equals u1.Id into tempUsr1
                                        from u1 in tempUsr1.DefaultIfEmpty()
                                        where j.POSITION_NAME.ToUpper().Contains(name.ToUpper())
                                        && (stdt != string.Empty ? ((Convert.ToDateTime(j.CREATED_ON.ToShortDateString()) >= Convert.ToDateTime(stdt))) : true)
                                        && (edt != string.Empty ? Convert.ToDateTime(j.CREATED_ON.ToShortDateString()) <= Convert.ToDateTime(edt) : true)
                                        select new { Job = j }).Select(item => new JOBPOSTING
                                        {
                                            JOB_ID = item.Job.JOB_ID,
                                            JOB_CODE = item.Job.JOB_CODE,
                                            JOB_DESCRIPTION = item.Job.JOB_DESCRIPTION,
                                            POSITION_NAME = item.Job.POSITION_NAME,
                                            NO_OF_VACANCIES = item.Job.NO_OF_VACANCIES,
                                            YEARS_OF_EXP_TOTAL = item.Job.YEARS_OF_EXP_TOTAL,
                                            CREATED_ON = item.Job.CREATED_ON,
                                            ISACTIVE=item.Job.ISACTIVE
                                        }).ToList();
            return jobCanObj;
        }

        public ActionResult Autocomplete(string term)
        {
            try
            {
                var model = db.STATUS_MASTER
                    .Where(p => p.STATUS_NAME.StartsWith(term) && p.ISACTIVE==true)
                    .Take(10)
                    .Select(p => new
                    {
                // jQuery UI needs the label property to function 
                label = p.STATUS_NAME.Trim()
                    });

                // Json returns [{"label":value}]
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("{'ex':'Exception'}");
            }
        }


        private string GetPartnerName(string pId)
        {
            var vendor = (from u in db.AspNetUsers.Where(i => i.Id == pId)
                          join v in db.VENDOR_MASTER on u.Vendor_Id equals v.VENDOR_ID
                          select v.VENDOR_NAME).FirstOrDefault();
            return vendor.ToString();
        }
                
       private string GetModifiedById(CANDIDATE objCan)
        {
            string strUser = string.Empty;
            if (!string.IsNullOrEmpty(objCan.STATUS)) //is to check first time candidates-to show admin modified as empty
            { 
                var stsSrc = db.STATUS_HISTORY.Where(i => i.CANDIDATE_ID == objCan.CANDIDATE_ID).ToList();
                if (stsSrc.Count > 0) { 
                var usr = (from c in stsSrc
                               join u in db.AspNetUsers.ToList() on c.MODIFIED_BY equals u.Id
                              select new { Name = u.FirstName+" "+u.LastName+" / "+c.MODIFIED_ON.Value.ToString("dd-MMM") }).FirstOrDefault();
                    strUser = usr.Name;
                }
            }
            return strUser;
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