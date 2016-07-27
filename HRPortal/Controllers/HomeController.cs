using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;
using HRPortal.Models;
using System.IO;
using HRPortal.Helper;
using HRPortal.Common;
using HRPortal.Common.Enums;
using System.Security.Claims;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    public class HomeController : Controller
    {
        ApplicationDbContext ctxt = new ApplicationDbContext();
        private HRPortalEntities db = new HRPortalEntities();
        LoginViewModel loginVM = new LoginViewModel();
        JobAndCandidateViewModels jobCanObj = new JobAndCandidateViewModels();
        private CandidateViewModels vmodelCan = new CandidateViewModels();
        AppointmentViewModels appVM = new AppointmentViewModels();
        const int pageSize = 10;

        public async Task<ActionResult> Index(string sOdr, int? page)
        {
            try {

                if (User.Identity.IsAuthenticated)
                {
                    //GetCurrentUser(ctxt); TODO: get everything form application db context for initial setup

                    if (CookieStore.GetCookie(CacheKey.Uid.ToString()) == null) { 
                        loginVM.SetUserToCache(User.Identity.Name);
                    }

                    if(System.Configuration.ConfigurationManager.AppSettings["IsAutoUpdateFdkPending"]=="true")
                    { 
                        vmodelCan.AutoUpdateStatus(); //Auto update the status of all the candidates to feedback pending if the due is passed.
                    }
                    
                    var dbJobs = await db.JOBPOSTINGs.ToListAsync();
                    if (HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
                    {
                        var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                        if (dbCan != null && dbCan.Count > 0)
                        {
                            jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
                        }
                        ViewBag.StatusList = vmodelCan.GetStatusList();
                        ViewBag.VendorList = vmodelCan.GetVendorList();
                        ViewBag.PositionList = vmodelCan.GetPositionList();
                    }
                    else
                    {
                        jobCanObj.JobItems = dbJobs.Where(row => row.ISACTIVE == true).ToList();
                        jobCanObj=GetJobSearchResults(jobCanObj.JobItems);
                    }

                    jobCanObj = (jobCanObj.CandidateItems != null && jobCanObj.CandidateItems.Count > 0)
                        || (jobCanObj.JobItems != null && jobCanObj.JobItems.Count > 0) ? GetPagination(jobCanObj, sOdr, page) : jobCanObj;

                    return View(jobCanObj);
                }
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<ActionResult> SearchCriteria(string name, string vendor, string position, string status, string stdt, string edt)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var dbJobs = await db.JOBPOSTINGs.ToListAsync();
                    if (HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
                    {
                        CookieStore.SetCookie(CacheKey.CANSearchHome.ToString(), name + "|" + vendor + "|" + position + "|" + status + "|" + stdt + "|" + edt, TimeSpan.FromHours(4));
                        var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                        jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
                        ViewBag.StatusList = vmodelCan.GetStatusList();
                        ViewBag.VendorList = vmodelCan.GetVendorList();
                        ViewBag.PositionList = vmodelCan.GetPositionList();

                        if (jobCanObj.CandidateItems.Count > 0)
                        {
                            jobCanObj = GetPagination(jobCanObj, string.Empty, 1);
                            return PartialView("_CandidateList", jobCanObj.CandidateItems);
                        }
                        else
                            return PartialView("_CandidateList");
                    }
                    else {
                        CookieStore.SetCookie(CacheKey.JobSearchHome.ToString(), name + "|" + stdt + "|" + edt, TimeSpan.FromHours(4));
                        jobCanObj.JobItems = dbJobs.Where(row => row.ISACTIVE == true).ToList();
                        jobCanObj = GetJobSearchResults(jobCanObj.JobItems);
                        jobCanObj = GetPagination(jobCanObj, string.Empty, 1);
                        return PartialView("_JobList", jobCanObj.JobItems);
                    }
                }

                return RedirectToAction("Login", "Account");

            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<ActionResult> ExportToExcel()
        {
            try { 
            System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
            var dbJobs = await db.JOBPOSTINGs.ToListAsync();
            var srchSrc = GetCandidateSearchResults(dbCan, dbJobs).CandidateItems.ToList();
            var dataSrc = srchSrc.Select(i => new {
                CandidateName= i.CANDIDATE_NAME,
                MobileNo = i.MOBILE_NO,
                Email = i.EMAIL,
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
            catch (Exception ex) { throw ex; }
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
            try
            {
                if(string.IsNullOrEmpty(status) && string.IsNullOrEmpty(id))
                {
                    return new EmptyResult();
                }
                
                Guid cId = Guid.Parse(id);
                string uid = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                STATUS_HISTORY sHist = new STATUS_HISTORY();
                Guid stsId = Guid.Parse(status);
                var stsMst = db.STATUS_MASTER.Where(s => s.STATUS_ID == stsId).FirstOrDefault();
             
                sHist.STATUS_ID = Guid.Parse(status);
                sHist.COMMENTS = comments;
                sHist.CANDIDATE_ID = Guid.Parse(id);
                sHist.ISACTIVE = true;
                sHist.MODIFIED_BY = uid;
                sHist.MODIFIED_ON = DateTime.Now;
                db.STATUS_HISTORY.Add(sHist);
                await db.SaveChangesAsync();

                CANDIDATE cANDIDATE = db.CANDIDATES.Where(i => i.CANDIDATE_ID == cId).FirstOrDefault();
                cANDIDATE.STATUS = status;
                cANDIDATE.MODIFIED_BY = uid;
                cANDIDATE.MODIFIED_ON = DateTime.Now;
                db.Entry(cANDIDATE).State = EntityState.Modified;
                await db.SaveChangesAsync();

                if (stsMst != null && stsMst.STATUS_DESCRIPTION.Contains("ToBeSchedule") && System.Configuration.ConfigurationManager.AppSettings["IsTBSMail"] == "true")
                {
                    await appVM.sendMailTBS(cId, comments);
                }
                return new EmptyResult();
            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<ActionResult> ClearSearch(string id)
        {
            CookieStore.ClearCookie(id == "CAN" ? CacheKey.CANSearchHome.ToString() : CacheKey.JobSearchHome.ToString());
            return await SearchCriteria(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
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
                ViewBag.JCodeSort = sOdr == "JCode_asc" ? "JCode_desc" : "JCode_asc";
                ViewBag.PositionSort = sOdr == "Position_desc" ? "Position_asc" : "Position_desc";
                ViewBag.PDateSort = sOdr == "PubDate_desc" ? "PubDate_asc" : "PubDate_desc";

                switch (sOdr)
                {
                    case "JCode_desc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderByDescending(s => s.JOB_CODE).ToList();
                        break;
                    case "JCode_asc":
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderBy(s => s.JOB_CODE).ToList();
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
                        jobCanObj.JobItems = jobCanObj.JobItems.OrderByDescending(s => s.CREATED_ON).ToList();
                        break;
                }
            }
            ViewBag.PageSize = pageSize;
            ViewBag.PageNo = (page ?? 1);
            return jobCanObj;
        }

        private JobAndCandidateViewModels GetCandidateSearchResults(List<CANDIDATE> dbCan, List<JOBPOSTING> dbJobs)
        {
            try
            {
                var cookie = CookieStore.GetCookie(CacheKey.CANSearchHome.ToString());
                string name = string.Empty, vendor = string.Empty, position = string.Empty, status = string.Empty, stdt = string.Empty, edt = string.Empty;
                if (!string.IsNullOrEmpty(cookie))
                {
                    string[] val = cookie.Split('|');
                    name = val[0]; vendor = val[1]; position = val[2]; status = val[3]; stdt = val[4]; edt = val[5];
                }

                if(System.Configuration.ConfigurationManager.AppSettings["IsCallSP_Temp"] == "true")
                { 
                    var canlist = db.getSearchResults(position, name, status, vendor, stdt, edt, "").ToList();
                    jobCanObj.CandidateItems = canlist.Select(i => new CandidateViewModels
                                                {
                                                CANDIDATE_ID = i.CANDIDATE_ID,
                                                CANDIDATE_NAME = i.CANDIDATE_NAME,
                                                MOBILE_NO = i.MOBILE_NO,
                                                EMAIL = i.EMAIL,
                                                POSITION = i.POSITION,
                                                RESUME_FILE_PATH = (string.IsNullOrEmpty(i.RESUME_FILE_PATH) ? string.Empty :i.RESUME_FILE_PATH),
                                                NOTICE_PERIOD = i.NOTICE_PERIOD,
                                                YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                                                LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                                                VENDOR_NAME = i.VENDOR_NAME,
                                                STATUS = i.STATUS,
                                                STATUS_ID = i.STATUS_ID,
                                                CREATED_ON = i.CREATED_ON,
                                                MODIFIED_ON = i.MODIFIED_ON,
                                                MODIFIED_BY = i.MODIFIED_BY,
                                            }).ToList();
            }
                else
                { 
                    jobCanObj.CandidateItems = (from c in dbCan
                                                join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                                join u1 in db.AspNetUsers.ToList() on c.CREATED_BY equals u1.Id
                                                join v in db.VENDOR_MASTER.ToList() on u1.Vendor_Id equals v.VENDOR_ID
                                                where c.CANDIDATE_NAME.ToUpper().Contains(name.ToUpper())
                                                && (status != string.Empty ? status.Split(',').Contains(c.STATUS) : true)
                                                && (stdt != string.Empty ? ((Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) >= Convert.ToDateTime(stdt))) : true)
                                                && (edt != string.Empty ? Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) <= Convert.ToDateTime(edt) : true)
                                                && (!string.IsNullOrEmpty(vendor) ? vendor.Split(',').Contains(v.VENDOR_NAME) : true)
                                                && (!string.IsNullOrEmpty(position) ? position.Split(',').Contains(j.JOB_CODE) : true)
                                                select new { Candidate = c, Job = j }).Select(i => new CandidateViewModels
                                                {
                                                    CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                                    CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                                    MOBILE_NO = i.Candidate.MOBILE_NO,
                                                    EMAIL = i.Candidate.EMAIL,
                                                    POSITION = i.Job.POSITION_NAME,
                                                    RESUME_FILE_PATH = string.IsNullOrEmpty(i.Candidate.RESUME_FILE_PATH) ? string.Empty : Path.Combine("/UploadDocument/", i.Candidate.RESUME_FILE_PATH),
                                                    NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                                    YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                                    LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                                    VENDOR_NAME = GetPartnerName(i.Candidate.CREATED_BY),
                                                    STATUS = vmodelCan.GetStatusNameById(i.Candidate.CANDIDATE_ID),
                                                    STATUS_ID = i.Candidate.STATUS,
                                                    CREATED_ON = i.Candidate.CREATED_ON,
                                                    MODIFIED_ON = i.Candidate.MODIFIED_ON,
                                                    MODIFIED_BY = GetModifiedById(i.Candidate),
                                                }).ToList();
                }

                return jobCanObj;
            }
            catch(Exception ex) {
                throw ex; }
           
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
                var stsSrc = db.STATUS_HISTORY.Where(i => i.CANDIDATE_ID == objCan.CANDIDATE_ID && i.ISACTIVE == true).OrderByDescending(j => j.MODIFIED_ON).FirstOrDefault();
                if (stsSrc != null && stsSrc.MODIFIED_BY != null && stsSrc.MODIFIED_ON != null)
                {
                    strUser = db.AspNetUsers.Where(x => x.Id == stsSrc.MODIFIED_BY).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault();
                    strUser = strUser + " / " + stsSrc.MODIFIED_ON.Value.ToString("dd-MMM");
                }
            }
            return strUser;
        }

        private ApplicationUser GetCurrentUser(ApplicationDbContext context)
        {
            var identity = User.Identity as ClaimsIdentity;
            Claim identityClaim = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            return context.Users.FirstOrDefault(u => u.Id == identityClaim.Value);
        }

        [AllowAnonymous]
        public ActionResult Unauthorized()
        {
            return View("_Unauthorized");
        }

        [AllowAnonymous]
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

        protected override void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;
            //Log Exception e to DB.
            if (!filterContext.ExceptionHandled) { 
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