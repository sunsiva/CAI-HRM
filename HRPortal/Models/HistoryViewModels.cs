using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class HistoryViewModels
    {
        public string ID { get; set; }
        public string NAME { get; set; }
        public string DESC { get; set; }
        public int ToT_Active_Jobs { get; set; }
        public int ToT_Candidates_PRGS { get; set; }
        public int ToT_Candidates_OFRD { get; set; }
        public int ToT_Candidates_RJTD { get; set; }
        public int ToT_Candidates_JOIN { get; set; }
        public int ToT_Candidates_DROP { get; set; }
        public int ToT_Candidates_DEFRD { get; set; }
    }
}