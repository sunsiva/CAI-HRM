using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class ReportsViewModels
    {
    }
    public class StagingReportViewModel
    {
        public string Position_Name { get; set; }
        public string Partner_Name { get; set; }
        public int Screening { get; set; }
        public int Round1 { get; set; }
        public int Round2 { get; set; }
        public int Round3 { get; set; }
        public int Offered { get; set; }
        public int Total { get; set; }
    }
    public class LWDCandidateReportViewModel
    {
        public string Position_Name { get; set; }
        public string Candidate_name { get; set; }
        public string Partner_Name { get; set; }
        public DateTime Submitted_On { get; set; }
        public DateTime Last_Working_Date { get; set; }
        public string Status { get; set; }
    }
}