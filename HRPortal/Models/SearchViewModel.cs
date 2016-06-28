using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class SearchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string Partners { get; set; }
    }
}