using HRPortal.Helper;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRPortal.Models
{
    public class CandidateViewModels
    {
        HRPortalEntities dbContext = new HRPortalEntities();

            public Guid CANDIDATE_ID { get; set; }
            public string CANDIDATE_NAME { get; set; }
            public string VENDOR_NAME { get; set; }
            public Guid JOB_ID { get; set; }
            public string POSITION { get; set; }
            public decimal YEARS_OF_EXP_TOTAL { get; set; }
            public Nullable<int> YEARS_OF_EXP_RELEVANT { get; set; }
            public string MOBILE_NO { get; set; }
            public string ALTERNATE_MOBILE_NO { get; set; }
            public string EMAIL { get; set; }
            public string ALTERNATE_EMAIL_ID { get; set; }
            public DateTime DOB { get; set; }
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
            public DateTime CREATED_ON { get; set; }
            public string STATUS { get; set; }
            public string STATUS_ID { get; set; }

        /// <summary>
        /// Update the candidate status on creating the canidate profile
        /// </summary>
        /// <param name="stid"></param>
        /// <param name="cId"></param>
        /// <param name="cmnts"></param>
        /// <returns></returns>
        public string UpdateStatus(Guid stid, Guid cId, string cmnts)
        {
            STATUS_HISTORY stsHist = new STATUS_HISTORY();
            var stsId = dbContext.STATUS_MASTER.Where(i => i.STATUS_ORDER == 1).FirstOrDefault().STATUS_ID;
            var uid = CookieStore.GetCookie(CacheKey.Uid.ToString()) == null ? HttpContext.Current.User.Identity.Name : CookieStore.GetCookie(CacheKey.Uid.ToString());
            stsHist = new STATUS_HISTORY();
            stsHist.STATUS_ID = ((stid == null || stid == Guid.Empty) ? stsId : stid);
            stsHist.CANDIDATE_ID = cId;
            stsHist.COMMENTS = string.IsNullOrEmpty(cmnts) ? "Initial Status - SCR-SBM" : cmnts;
            stsHist.ISACTIVE = true;
            stsHist.MODIFIED_BY = uid.ToString();
            stsHist.MODIFIED_ON = HelperFuntions.GetDateTime();
            dbContext.STATUS_HISTORY.Add(stsHist);
            dbContext.SaveChanges();
            return "OK";
        }

        /// <summary>
        /// An automatic update to Feedback pending status after invterview got over
        /// </summary>
        public void AutoUpdateStatus()
        {
            string autoMsg = "Automatic Status Update: Feedback Pending From";
            STATUS_HISTORY stsHist = new STATUS_HISTORY();
            var sHist = dbContext.STATUS_HISTORY.Where(i => i.ISACTIVE == true).ToList();
            var updHist = sHist.Where(i => i.SCHEDULED_TO != null && i.SCHEDULED_TO <= DateTime.Now).ToList();
            var existHist = sHist.Where(i => i.COMMENTS.Contains(autoMsg)).Select(c=>c.CANDIDATE_ID).ToList();

            if (sHist.Count() > 0)
            { 
                var uid = CookieStore.GetCookie(CacheKey.Uid.ToString()) == null ? HttpContext.Current.User.Identity.Name : CookieStore.GetCookie(CacheKey.Uid.ToString());
                var stsLst = dbContext.STATUS_MASTER.Where(i => i.ISACTIVE == true).ToList();

                foreach (var item in updHist)
                {
                    if (!existHist.Contains(item.CANDIDATE_ID))
                    {
                        int stsOrdr = stsLst.Where(i => i.STATUS_ID == item.STATUS_ID).FirstOrDefault().STATUS_ORDER.GetValueOrDefault();
                        stsHist = new STATUS_HISTORY();
                        stsHist.STATUS_ID = stsLst.Where(i => i.STATUS_ORDER == stsOrdr + 1).FirstOrDefault().STATUS_ID;
                        stsHist.CANDIDATE_ID = item.CANDIDATE_ID;
                        stsHist.COMMENTS = autoMsg + " " + item.SCHEDULED_FOR;//TODO:attach the profile owner/who has scheduled last.
                        stsHist.ISACTIVE = true;
                        stsHist.MODIFIED_BY = item.MODIFIED_BY;
                        stsHist.MODIFIED_ON = HelperFuntions.GetDateTime();
                        dbContext.STATUS_HISTORY.Add(stsHist);
                        dbContext.SaveChanges();

                        CANDIDATE cANDIDATE = dbContext.CANDIDATES.Where(i => i.CANDIDATE_ID == item.CANDIDATE_ID).FirstOrDefault();
                        cANDIDATE.STATUS = stsHist.STATUS_ID.ToString();
                        cANDIDATE.MODIFIED_BY = uid;
                        cANDIDATE.MODIFIED_ON = HelperFuntions.GetDateTime();
                        dbContext.Entry(cANDIDATE).State = EntityState.Modified;
                        dbContext.SaveChanges();
                    }
                }
            }
        }

        public SelectList GetStatusList()
        {
            var sts = dbContext.STATUS_MASTER.Where(i => i.ISACTIVE == true).Select(s => new { s.STATUS_ID, s.STATUS_NAME, s.STATUS_DESCRIPTION, s.STATUS_ORDER }).OrderBy(v => v.STATUS_ORDER).ToList();
           return new SelectList(sts.AsEnumerable(), "STATUS_ID", "STATUS_DESCRIPTION", 1);
        }

        /// <summary>
        /// Get the list of active vendors
        /// </summary>
        /// <returns></returns>
        public SelectList GetVendorList()
        {
            var sts = dbContext.VENDOR_MASTER.Where(i => i.ISACTIVE == true).Select(s => new { s.VENDOR_ID, s.VENDOR_NAME}).OrderBy(v => v.VENDOR_NAME).ToList();
            return new SelectList(sts.AsEnumerable(), "VENDOR_NAME", "VENDOR_NAME", 1);
        }

        /// <summary>
        /// Get the list of active vendors with IDs
        /// </summary>
        /// <returns></returns>
        public SelectList GetVendorListWithIDs()
        {
            var sts = dbContext.VENDOR_MASTER.Where(i => i.ISACTIVE == true).Select(s => new { s.VENDOR_ID, s.VENDOR_NAME }).OrderBy(v => v.VENDOR_NAME).ToList();
            return new SelectList(sts.AsEnumerable(), "VENDOR_ID", "VENDOR_NAME", 1);
        }

        /// <summary>
        /// Get the list of all the active positions with JobCodes(Used for filters)
        /// </summary>
        /// <returns></returns>
        public SelectList GetPositionForPartner()
        {
            var vid = CookieStore.GetCookie(CacheKey.VendorId.ToString());
            
            //var lstJobs = (from j in dbContext.JOBPOSTINGs
            //               join jv in dbContext.JOBXVENDORs on j.JOB_ID equals jv.Job_Id
            //               where j.ISACTIVE == true && jv.Vendor_Id == Guid.Parse(vid)
            //               select new { j.JOB_ID, j.JOB_CODE, j.POSITION_NAME }).OrderBy(v => v.POSITION_NAME).ToList();
            var lstJobs = dbContext.JOBPOSTINGs.Where(i => i.ISACTIVE == true).Select(s => new { s.JOB_ID, s.POSITION_NAME, s.JOB_CODE, s.JOB_DESCRIPTION }).OrderBy(v => v.POSITION_NAME).ToList();
            return new SelectList(lstJobs.AsEnumerable(), "JOB_CODE", "POSITION_NAME", 1);
        }

        /// <summary>
        /// Get the list of all the active/inactive positions
        /// </summary>
        /// <returns></returns>
        public SelectList GetAllPositions()
        {
            var sts = dbContext.JOBPOSTINGs.Select(s => new { s.JOB_ID, s.POSITION_NAME, s.JOB_CODE, s.JOB_DESCRIPTION }).OrderBy(v => v.POSITION_NAME).ToList();
            return new SelectList(sts.AsEnumerable(), "JOB_ID", "POSITION_NAME", 1);
        }

        /// <summary>
        /// Get the list of all the active positions
        /// </summary>
        /// <returns></returns>
        public SelectList GetAllActivePositions()
        {
            var sts = dbContext.JOBPOSTINGs.Where(i => i.ISACTIVE == true).Select(s => new { s.JOB_ID, s.POSITION_NAME, s.JOB_CODE, s.JOB_DESCRIPTION }).OrderBy(v => v.POSITION_NAME).ToList();
            return new SelectList(sts.AsEnumerable(), "JOB_ID", "POSITION_NAME", 1);
        }

        public string GetStatusNameById(Guid id)
        {
            string stsName = "Screening Submitted";// "SCR -SBM";
            var stsSrc = dbContext.STATUS_HISTORY.Where(i => i.CANDIDATE_ID == id).ToList();
            if (stsSrc.Count() > 0)
            {
                var stsH = stsSrc.OrderByDescending(j => j.MODIFIED_ON).FirstOrDefault().STATUS_ID;
                stsName = dbContext.STATUS_MASTER.Where(i => i.STATUS_ID == stsH).FirstOrDefault().STATUS_DESCRIPTION;
            }
            return stsName;
        }

    }
}