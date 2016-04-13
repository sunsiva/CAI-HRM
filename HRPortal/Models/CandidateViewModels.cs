using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class CandidateViewModels
    {
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
            public string COMMENTS { get; set; }
            public Nullable<bool> ISINNOTICEPERIOD { get; set; }
            public bool ISACTIVE { get; set; }
            public string MODIFIED_BY { get; set; }
            public Nullable<System.DateTime> MODIFIED_ON { get; set; }
            public string CREATED_BY { get; set; }
            public System.DateTime CREATED_ON { get; set; }
            //public List<SearchCandidatesViewModels> SEARCHCANDIDATES { get; set; } 
    }

    //public class SearchCandidatesViewModels
    //{
    //    public string CANDIDATE_NAME { get; set; }
    //    public string POSITION { get; set; }
    //    public System.DateTime PUBLISHED_START_DATE { get; set; }
    //    public System.DateTime PUBLISHED_END_DATE { get; set; }
    //    public string VENDOR { get; set; }
    //    public string STATUS { get; set; }
    //}
}