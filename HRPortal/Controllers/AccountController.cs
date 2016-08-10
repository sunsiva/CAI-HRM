using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using HRPortal.Models;
using System.Data.Entity;
using HRPortal.Helper;
using HRPortal.Common;
using HRPortal.Common.Enums;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Globalization;

namespace HRPortal.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private HRPortalEntities db = new HRPortalEntities();
        ApplicationDbContext dbContext = new ApplicationDbContext();
        LoginViewModel loginVM = new LoginViewModel();
        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            // User was redirected here because of authorization section
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Unauthorized","Home");

            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email.ToLower(), model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    loginVM.ClearCookie();
                    loginVM.SetUserToCache(model.Email);
                    if (System.Configuration.ConfigurationManager.AppSettings["IsUserLogEnable"] == "true")
                    {
                        loginVM.UserLogs(true, model.Email, CookieStore.GetCookie(CacheKey.UserName.ToString()).ToString());
                    }
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            //var query = db.VENDOR_MASTER.Select(i=> new SelectListItem {
            //     Text = i.VENDOR_NAME,
            //     Value = i.VENDOR_ID.ToString()
            // });
            SetRoleList();
            SetVendorList();
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model,FormCollection frm)
        {
            SetVendorList();
            SetRoleList();
            if (ModelState.IsValid)
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    var user = new ApplicationUser { UserName = model.Email.ToLower(), Email = model.Email.ToLower(), PhoneNumber = model.PhoneNumber };
                    var result = await UserManager.CreateAsync(user, model.Password);
                    //string sqlqry = "UPDATE[dbo].[AspNetUsers] SET[Vendor_Id] = '"+Guid.NewGuid()+"',[IsAdmin] = 0,[CreatedBy] ='" + user.Id + "',[CreatedOn] = GETDATE() WHERE Id = '" + user.Id+"'";
                    //await db.Database.ExecuteSqlCommandAsync(sqlqry);
                    try
                    {
                        if (result.Succeeded)
                        {
                           //Update the miscellanous columns
                            AspNetUser objUser = await db.AspNetUsers.FindAsync(user.Id);
                            objUser.FirstName = model.FirstName;
                            objUser.LastName = model.LastName;
                            objUser.CreatedOn = DateTime.Now;
                            objUser.CreatedBy = CookieStore.GetCookie(CacheKey.Uid.ToString())==string.Empty?User.Identity.Name: CookieStore.GetCookie(CacheKey.Uid.ToString());
                            objUser.IsActive  = true;
                            objUser.Vendor_Id = !string.IsNullOrEmpty(frm["ddlVendorList"]) ? Guid.Parse(frm["ddlVendorList"]) : Guid.Empty;// model.Vendor_Id;
                            db.Entry(objUser).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            //Assign Role to user Here: Note:-the type2,type3 are not working, so implemented type1.
                            //TYPE:1
                            string roleid = !string.IsNullOrEmpty(frm["ddlRoleList"]) ? frm["ddlRoleList"] : string.Empty;
                            string sqlqry = "INSERT INTO [DBO].[AspNetUserRoles] VALUES('" + user.Id + "','" + roleid + "')";
                            await db.Database.ExecuteSqlCommandAsync(sqlqry);
                            //Insert role here, to access while log in to the system.
                            UserXRole role = new UserXRole();
                            role.UserId = Guid.Parse(user.Id);
                            role.RoleId = Guid.Parse(roleid);
                            db.UserXRoles.Add(role);
                            await db.SaveChangesAsync();

                            //TYPE:2
                            //string rolename = db.AspNetRoles.Find(roleid).Name;
                            //await UserManager.AddToRoleAsync(user.Id, rolename);
                            //TYPE:3
                            //var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
                            //userManager.AddToRole(user.Id, rolename);
                            
                            dbContextTransaction.Commit();

                            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                            // Send an email with this link
                            //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                            //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                            //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                            ViewBag.RegSuccess = "User registered successfully.";
                            return View(model);
                       
                         }

                        ViewBag.RegFail = result.Errors.FirstOrDefault();
                        AddErrors(result);
                    }
                    catch (Exception ex) {
                        ViewBag.RegFail = result.Errors.FirstOrDefault();
                        dbContextTransaction.Rollback();
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult GetAllUsers()
        {
            var usrs = db.AspNetUsers.Where(u=>u.IsActive== true).ToList().Select(x => new RegisterViewModel {
                Id = x.Id, FirstName = x.FirstName, LastName = x.LastName, Email = x.Email, PhoneNumber = x.PhoneNumber, Vendor_Id=x.Vendor_Id, CreatedBy=x.CreatedBy }).ToList();
            if (CookieStore.GetCookie(CacheKey.RoleName.ToString()).ToString().ToUpper().Contains("SUPERUSER"))
            {
                var vendorId = Guid.Parse(HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.VendorId.ToString())));
                usrs = usrs.Where(i => i.Vendor_Id == vendorId).ToList();
            }
            return View("UserIndex", usrs);
        }

        public ActionResult UserEdit(string id)
        {
            var usr = db.AspNetUsers.Where(x=>x.Id==id).Select(x => new RegisterViewModel { Id = x.Id, FirstName = x.FirstName, LastName = x.LastName, Email = x.Email, PhoneNumber = x.PhoneNumber }).FirstOrDefault();
            return View(usr);
        }

        [HttpPost]
        public ActionResult UserEdit(RegisterViewModel model)
        {
            try
            {
                    //Update Users
                    AspNetUser objUser = db.AspNetUsers.Find(model.Id);
                    objUser.Email = model.Email;
                    objUser.FirstName = model.FirstName;
                    objUser.LastName = model.LastName;
                    objUser.PhoneNumber = model.PhoneNumber;
                    db.Entry(objUser).State = EntityState.Modified;
                    db.SaveChanges();
                ViewBag.RegSuccess = "User modified successfully.";
                return View("UserEdit");
            }
            catch (Exception ex) { throw ex; }
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult LogOff(string id)
        {
            return RedirectToAction("Login");
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            if (System.Configuration.ConfigurationManager.AppSettings["IsUserLogEnable"] == "true")
            {
                loginVM.UserLogs(false, User.Identity.Name, "Log Off");
            }
            return RedirectToAction("Login");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region "private methods"
        private void SetVendorList()
        {
            var query = db.VENDOR_MASTER.Where(s=>s.ISACTIVE==true).Select(i => new { i.VENDOR_ID, i.VENDOR_NAME });
            if(CookieStore.GetCookie(CacheKey.Uid.ToString()).ToString().ToUpper().Contains("SUPERUSER"))
            {
                var vendorId = Guid.Parse(HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.VendorId.ToString())));
                query = query.Where(i => i.VENDOR_ID == vendorId);
            }
            ViewBag.VendorList = new SelectList(query.AsEnumerable(), "VENDOR_ID", "VENDOR_NAME", 3);
        }

        private void SetRoleList()
        {
            var query = db.AspNetRoles.Select(i => new { i.Id, i.Name });
            if (CookieStore.GetCookie(CacheKey.Uid.ToString()).ToString().ToUpper().Contains("SUPERUSER"))
            {
                query = query.Where(i => i.Name == "User");
            }
            ViewBag.RoleList = new SelectList(query.AsEnumerable(), "Id", "Name", 3);
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        
        protected override void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;
            //Log Exception e to DB.
            if (!filterContext.ExceptionHandled)
            {
                LoggingUtil.LogException(e, errorLevel: ErrorLevel.Critical);
                filterContext.ExceptionHandled = true;
            }
            //filterContext.Result = new ViewResult()
            //{
            //    ViewName = "Error"
            //};
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            bool isSuperAdmin = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.RoleName.ToString())).ToUpper().Contains("ADMIN") ? true : false;
            var isHome = isSuperAdmin == true ? "Dashboard" : "Home";
            return RedirectToAction("Index", isHome);
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }


        #endregion
    }
}