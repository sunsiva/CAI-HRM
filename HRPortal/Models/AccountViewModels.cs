using HRPortal.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Net.Mail;
using HRPortal.Helper;

namespace HRPortal.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class VendorList
    {
        public string Vendor_Id { get; set; }
        //public IEnumerable<System.Web.Mvc.SelectListItem> VendorList { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        private HRPortalEntities db = new HRPortalEntities();
        ApplicationDbContext dbContext = new ApplicationDbContext();

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public async Task<string> sendMail()
        {
            var body = "<p>Email From: {0} ({1})</p><p>Message:</p><p>{2}</p>";
            var message = new MailMessage();
            message.To.Add(new MailAddress("sivaprakasam_sundaram@compaid.co.in")); 
            //message.From = new MailAddress("sivaprakasam_sundaram@compaid.co.in");
            // message.To.Add(new MailAddress("one@gmail.com"));
            //message.Bcc.Add(new MailAddress("one@gmail.com"));
            //if (model.Upload != null && model.Upload.ContentLength > 0)
            //{
            //    message.Attachments.Add(new Attachment(model.Upload.InputStream, Path.GetFileName(model.Upload.FileName)));
            //}
            //message.Attachments.Add(new Attachment(HttpContext.Server.MapPath("~/App_Data/Test.docx")));
            message.Subject = "My first mail for HR portal";
            message.Body = string.Format(body, "siv", "", "its my first mail");
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
              await smtp.SendMailAsync(message);
            }
            return "Mail Sent";
        }

        public void SetUserToCache(string email)
        {
            var user = db.AspNetUsers.Where(x=>x.UserName==email).FirstOrDefault();
            var rolename = (from rle in db.AspNetRoles.ToList()
                            join rlx in db.UserXRoles.ToList() on Guid.Parse(rle.Id) equals rlx.RoleId
                            where rlx.UserId == Guid.Parse(user.Id)
                            select rle.Name).FirstOrDefault();

            //var rolename = (from usr in db.AspNetUsers.ToList()
            //                join rlx in db.UserXRoles.ToList() on Guid.Parse(usr.Id) equals rlx.UserId
            //                join rle in db.AspNetRoles.ToList() on rlx.RoleId equals Guid.Parse(rle.Id)
            //                where usr.Id == id
            //                select rle.Name).FirstOrDefault();

            HttpContext.Current.Session[CacheKey.VendorId.ToString()]= user.Vendor_Id;
            HttpContext.Current.Session[CacheKey.RoleName.ToString()]= rolename;
            HttpContext.Current.Session[CacheKey.Uid.ToString()]= user.Id;
            HttpContext.Current.Session[CacheKey.UserName.ToString()]= user.FirstName + " " + user.LastName;
        }

        public void UserLogs(bool isIn,string email, string username)
        {
            UserLog ulog = new UserLog();
            if (isIn)
            {
                ulog.UserLogName = username;
                ulog.LoggedInBy = email;// HttpRuntime.Cache.Get("LogInEmail") != null ? HttpRuntime.Cache.Get("LogInEmail").ToString() : string.Empty;
                ulog.LoggedInOn = DateTime.Now;
                ulog.UserLogDesc = "Computer Name is-" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostAddress).HostName;
                ulog.IsOnline = true;
                ulog.UserIP = GetLocalIPAddress();// System.Net.Dns.GetHostName();
                db.UserLogs.Add(ulog);
            }
            else {
                ulog = db.UserLogs.Where(u => u.LoggedInBy == email && u.IsOnline == true).FirstOrDefault();
                if (ulog != null)
                {
                    ulog.LoggedOutOn = DateTime.Now;
                    ulog.IsOnline = false;
                    db.Entry(ulog).State = EntityState.Modified;
                }
            }
            db.SaveChanges();
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        
        public string GetUserNameById(string id)
        {
            return db.AspNetUsers.Where(i => i.Id == id).Select(s=> s.FirstName + " " + s.LastName).FirstOrDefault();
        }

    }

    public class RegisterViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [StringLength(20, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 9)]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression("^\\D?(\\d{3})\\D?\\D?(\\d{3})\\D?(\\d{4})$", ErrorMessage = "Please provide a valid number.")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Is Admin")]
        public bool IsAdmin { get; set; }
        public string CreatedBy { get; set; }
        [Display(Name = "Vendor")]
        public Nullable<System.Guid> Vendor_Id { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
