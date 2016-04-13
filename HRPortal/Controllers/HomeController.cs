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

namespace HRPortal.Controllers
{
    public class HomeController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();

        public async Task<ActionResult> Index()
        {
            JobAndCandidateViewModels obj = new JobAndCandidateViewModels();
            if (User.Identity.IsAuthenticated)
            {
                var dbJobs = await db.JOBPOSTINGs.ToListAsync();
                if (User.Identity.Name.ToUpper().Contains("ADMIN"))
                {
                    var dbCan = await db.CANDIDATES.ToListAsync();
                    obj.CandidateItems = (from c in dbCan
                                  join j in dbJobs on c.JOB_ID equals j.JOB_ID
                                  select new { Candidate = c, Job=j }).Select(i => new CandidateViewModels {
                                      CANDIDATE_ID=i.Candidate.CANDIDATE_ID,
                                      CANDIDATE_NAME = i.Candidate.CANDIDATE_NAME,
                                      POSITION=i.Job.POSITION_NAME,
                                      NOTICE_PERIOD=i.Candidate.NOTICE_PERIOD,
                                      YEARS_OF_EXP_TOTAL=i.Candidate.YEARS_OF_EXP_TOTAL,
                                      LAST_WORKING_DATE=i.Candidate.LAST_WORKING_DATE,
                                      CREATED_ON=i.Candidate.CREATED_ON,
                                      MODIFIED_ON=i.Candidate.MODIFIED_ON,
                                      MODIFIED_BY=i.Candidate.MODIFIED_BY
                                  }).ToList();
                }
                else {
                    obj.JobItems = dbJobs;
                }
                return View(obj);
            }
            return RedirectToAction("Login", "Account");
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