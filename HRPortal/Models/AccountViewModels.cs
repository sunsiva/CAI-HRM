﻿using HRPortal.Common;
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

        public void SetUserToCache(string email)
        {
            int cookieTimeout = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["CookieTimeOutInDays"]);
            var user = db.AspNetUsers.Where(x=>x.UserName==email).FirstOrDefault();
            var rolename = (from rle in db.AspNetRoles.ToList()
                            join rlx in db.UserXRoles.ToList() on Guid.Parse(rle.Id) equals rlx.RoleId
                            where rlx.UserId == Guid.Parse(user.Id)
                            select rle.Name).FirstOrDefault();

            CookieStore.SetCookie(CacheKey.Uid.ToString(), user.Id, TimeSpan.FromDays(cookieTimeout));
            CookieStore.SetCookie(CacheKey.VendorId.ToString(), user.Vendor_Id.Value.ToString(), TimeSpan.FromDays(cookieTimeout));
            CookieStore.SetCookie(CacheKey.RoleName.ToString(), rolename, TimeSpan.FromDays(cookieTimeout));
            CookieStore.SetCookie(CacheKey.UserName.ToString(), user.FirstName + " " + user.LastName, TimeSpan.FromDays(cookieTimeout));
        }
        
        /// <summary>
        /// Clears the cookie values
        /// </summary>
        public void ClearCookie()
        {
            CookieStore.ClearCookie(CacheKey.Uid.ToString());
            CookieStore.ClearCookie(CacheKey.VendorId.ToString());
            CookieStore.ClearCookie(CacheKey.RoleName.ToString());
            CookieStore.ClearCookie(CacheKey.UserName.ToString());
        }

        public void UserLogs(bool isIn,string email, string username)
        {
            UserLog ulog = new UserLog();
            

            if (isIn)
            {
                ulog.UserLogName = username;
                ulog.LoggedInBy = email;
                ulog.LoggedInOn = HelperFuntions.GetDateTime();
                ulog.UserLogDesc = "Logged In";
                ulog.IsOnline = true;
                ulog.UserIP =  GetLocalIPAddress();
                db.UserLogs.Add(ulog);
                db.SaveChanges();
                int cookieTimeout = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["CookieTimeOutInDays"]);
                CookieStore.SetCookie(CacheKey.LoginId.ToString(), ulog.UserLogId.ToString(), TimeSpan.FromDays(cookieTimeout));
            }
            else {
                int lid = (string.IsNullOrEmpty(CookieStore.GetCookie(CacheKey.LoginId.ToString()))?0: Convert.ToInt32(CookieStore.GetCookie(CacheKey.LoginId.ToString())));
                ulog = db.UserLogs.Where(u => u.IsOnline == true && u.UserLogId == lid).FirstOrDefault();
                if (ulog != null)
                {
                    ulog.LoggedOutOn = HelperFuntions.GetDateTime();
                    ulog.UserLogDesc = username;
                    ulog.IsOnline = false;
                    db.Entry(ulog).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
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
