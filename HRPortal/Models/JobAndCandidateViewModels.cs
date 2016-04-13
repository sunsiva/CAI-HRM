using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class JobAndCandidateViewModels
    {
        public List<JOBPOSTING> JobItems { get; set; }
        public List<CandidateViewModels> CandidateItems { get; set; }
    }
}