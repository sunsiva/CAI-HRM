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
    
    public partial class JOB_HISTORY
    {
        public System.Guid JOB_HIST_ID { get; set; }
        public System.Guid JOB_ID { get; set; }
        public string JOB_COMMENTS { get; set; }
        public Nullable<bool> IS_ACTIVE { get; set; }
        public string CREATED_BY { get; set; }
        public Nullable<System.DateTime> CREATED_ON { get; set; }
    
        public virtual JOBPOSTING JOBPOSTING { get; set; }
    }
}