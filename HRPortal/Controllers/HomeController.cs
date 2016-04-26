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

        public async Task<ActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                int page = 1;
                GetStatusList();
                if (HttpRuntime.Cache.Get(CacheKey.Uid.ToString()) == null)
                    loginVM.SetUserToCache(User.Identity.Name);
                var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
                if (HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
                {
                    var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                    jobCanObj.CandidateItems = (from c in dbCan
                                             join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                          select new { Candidate = c, Job = j}).Select(i => new CandidateViewModels
                                          {
                                              CANDIDATE_ID = i.Candidate.CANDIDATE_ID,
                                              CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                              POSITION = i.Job.POSITION_NAME,
                                              RESUME_FILE_PATH = string.IsNullOrEmpty(i.Candidate.RESUME_FILE_PATH)? string.Empty: Path.Combine("/UploadDocument/", i.Candidate.RESUME_FILE_PATH),
                                              NOTICE_PERIOD = i.Candidate.NOTICE_PERIOD,
                                              YEARS_OF_EXP_TOTAL = i.Candidate.YEARS_OF_EXP_TOTAL,
                                              LAST_WORKING_DATE = i.Candidate.LAST_WORKING_DATE,
                                              VENDOR_NAME = GetPartnerName(i.Candidate.CREATED_BY),
                                              STATUS = vmodelCan.GetStatusNameById(i.Candidate.CANDIDATE_ID),
                                              CREATED_ON = i.Candidate.CREATED_ON,
                                              MODIFIED_ON = i.Candidate.MODIFIED_ON,
                                              MODIFIED_BY = GetModifiedById(i.Candidate.CANDIDATE_ID),
                                          }).Skip((page-1)*pageSize).Take(pageSize).ToList();

                    ViewBag.CurrentPage = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalPages = Math.Ceiling((double)dbCan.Count() / pageSize);
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
            if (HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN"))
            {
                CookieStore.SetCookie("CANSEARCHHOME", name + "|" + vendor + "|" + status + "|" + stdt + "|" + edt, TimeSpan.FromMinutes(4));
                var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
                jobCanObj = GetCandidateSearchResults(dbCan, dbJobs);
            }
            else {
                jobCanObj.JobItems = dbJobs;
            }
            return PartialView("_CandidateList", jobCanObj.CandidateItems);
        }

        public async Task<ActionResult> ExportToExcel(int id)
        {
            System.Web.UI.WebControls.GridView gv = new System.Web.UI.WebControls.GridView();
            var dbCan = await db.CANDIDATES.Where(row => row.ISACTIVE == true).ToListAsync();
            var dbJobs = await db.JOBPOSTINGs.Where(row => row.ISACTIVE == true).ToListAsync();
            gv.DataSource = GetCandidateSearchResults(dbCan, dbJobs).CandidateItems.ToList(); //TODO: select the columns only wanted for excel
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

            //vmodelCan.UpdateStatus(Guid.Parse(status), Guid.Parse(id), comments);
            return new EmptyResult();
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
                                  join u in db.AspNetUsers.ToList() on c.MODIFIED_BY equals u.Id into tempUsr
                                  from u in tempUsr.DefaultIfEmpty()
                                  where c.CANDIDATE_NAME.ToUpper().Contains(name.ToUpper())
                                  && (status != string.Empty ? c.STATUS == status : true)
                                  && (stdt != string.Empty ? ((Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) >= Convert.ToDateTime(stdt))) : true)
                                  && (edt != string.Empty ? Convert.ToDateTime(c.CREATED_ON.ToShortDateString()) <= Convert.ToDateTime(edt) : true)
                                  && v.VENDOR_NAME.ToUpper().Contains(vendor.ToUpper())
                                  select new { Candidate = c, Job = j, Users = u, Users1 = u1, Vendor = v }).Select(i => new CandidateViewModels
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
                                      CREATED_BY = i.Users1 == null ? string.Empty : i.Users1.FirstName + " " + i.Users1.LastName,
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
                
       private string GetModifiedById(Guid id)
        {
            string strUser = string.Empty;
            var stsSrc = db.STATUS_HISTORY.Where(i => i.CANDIDATE_ID == id).ToList();
            if (stsSrc.Count > 0) { 
            var usr = (from c in stsSrc
                           join u in db.AspNetUsers.ToList() on c.MODIFIED_BY equals u.Id
                          select new { Name = u.FirstName+" "+u.LastName+" / "+c.MODIFIED_ON.Value.ToString("dd-MMM") }).FirstOrDefault();
                strUser = usr.Name;
            }
            
            return strUser;
        }

        private void GetStatusList()
        {
            var sts = db.STATUS_MASTER.Where(i => i.ISACTIVE == true).Select(s=> new { s.STATUS_ID,s.STATUS_NAME,s.STATUS_DESCRIPTION,s.STATUS_ORDER }).OrderBy(v=>v.STATUS_ORDER).ToList();
            ViewBag.StatusList = new SelectList(sts.AsEnumerable(), "STATUS_ID", "STATUS_DESCRIPTION", 1);
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