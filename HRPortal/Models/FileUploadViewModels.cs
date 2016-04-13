using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HRPortal.Models
{
    public class FileUploadViewModels
    {
        [Required]
        public HttpPostedFileBase File { get; set; }
    }
}