//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HRPortal
{
    using System;
    using System.Collections.Generic;
    
    public partial class STATUS_HISTORY
    {
        public int ID { get; set; }
        public System.Guid STATUS_ID { get; set; }
        public System.Guid CANDIDATE_ID { get; set; }
        public string COMMENTS { get; set; }
        public bool ISACTIVE { get; set; }
        public Nullable<System.DateTime> SCHEDULED_TO { get; set; }
        public string SCHEDULED_FOR { get; set; }
        public Nullable<int> SCHEDULE_LENGTH_MINS { get; set; }
        public string MODIFIED_BY { get; set; }
        public Nullable<System.DateTime> MODIFIED_ON { get; set; }
        public Nullable<int> NO_OF_TIMES_APPEARED { get; set; }
    
        public virtual STATUS_MASTER STATUS_MASTER { get; set; }
    }
}
