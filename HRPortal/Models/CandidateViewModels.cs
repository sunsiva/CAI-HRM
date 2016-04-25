using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace HRPortal.Models
{
    public class CandidateViewModels
    {
        HRPortalEntities dbContext = new HRPortalEntities();

            public System.Guid CANDIDATE_ID { get; set; }
            public string CANDIDATE_NAME { get; set; }
            public string VENDOR_NAME { get; set; }
            public System.Guid JOB_ID { get; set; }
            public string POSITION { get; set; }
            public int YEARS_OF_EXP_TOTAL { get; set; }
            public Nullable<int> YEARS_OF_EXP_RELEVANT { get; set; }
            public string MOBILE_NO { get; set; }
            public string ALTERNATE_MOBILE_NO { get; set; }
            public string EMAIL { get; set; }
            public string ALTERNATE_EMAIL_ID { get; set; }
            public System.DateTime DOB { get; set; }
            public string CURRENT_COMPANY { get; set; }
            public string NOTICE_PERIOD { get; set; }
            public Nullable<bool> ANY_OTHER_OFFER { get; set; }
            public Nullable<System.DateTime> LAST_WORKING_DATE { get; set; }
            public string RESUME_FILE_PATH { get; set; }
            public string COMMENTS { get; set; }
            public Nullable<bool> ISINNOTICEPERIOD { get; set; }
            public bool ISACTIVE { get; set; }
            public string MODIFIED_BY { get; set; }
            public Nullable<System.DateTime> MODIFIED_ON { get; set; }
            public string CREATED_BY { get; set; }
            public System.DateTime CREATED_ON { get; set; }
            public string STATUS { get; set; }
            public PaginationViewModels PageIndex { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stid"></param>
        /// <param name="cId"></param>
        /// <param name="cmnts"></param>
        /// <returns></returns>
        public string UpdateStatus(Guid stid, Guid cId, string cmnts)
        {
            STATUS_HISTORY stsHist = new STATUS_HISTORY();
            var stsId = dbContext.STATUS_MASTER.Where(i => i.STATUS_ORDER == 1).FirstOrDefault().STATUS_ID;
            var uid = HttpRuntime.Cache.Get("user") == null ? Guid.NewGuid() : HttpRuntime.Cache.Get("user");
            stsHist = new STATUS_HISTORY();
            stsHist.STATUS_ID = ((stid == null || stid == Guid.Empty) ? stsId : stid);
            stsHist.CANDIDATE_ID = cId;
            stsHist.COMMENTS = string.IsNullOrEmpty(cmnts) ? "Initial Status - SCR-SBM" : cmnts;
            stsHist.ISACTIVE = true;
            stsHist.MODIFIED_BY = HttpRuntime.Cache.Get("user") == null ? string.Empty : HttpRuntime.Cache.Get("user").ToString();
            stsHist.MODIFIED_ON = DateTime.Now;
            dbContext.STATUS_HISTORY.Add(stsHist);
            dbContext.SaveChanges();

            return "OK";
        }

        public string GetStatusNameById(Guid id)
        {
            string stsName = "SCR-SBM";
            var stsSrc = dbContext.STATUS_HISTORY.Where(i => i.CANDIDATE_ID == id).ToList();
            if (stsSrc.Count > 0)
            {
                var stsH = stsSrc.OrderByDescending(j => j.MODIFIED_ON).FirstOrDefault().STATUS_ID;
                stsName = dbContext.STATUS_MASTER.Where(i => i.STATUS_ID == stsH).FirstOrDefault().STATUS_NAME;
            }
            return stsName;
        }

    }
}