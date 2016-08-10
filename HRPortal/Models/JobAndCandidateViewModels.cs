using System.Collections.Generic;
using System.Web.Mvc;

namespace HRPortal.Models
{
    public class JobAndCandidateViewModels
    {
        public List<JOBPOSTING> JobItems { get; set; }
        public List<CandidateViewModels> CandidateItems { get; set; }
        public IEnumerable<SelectListItem> StatusList { get; set; }
        public string ddlStatusId { get; set; }
    }
}