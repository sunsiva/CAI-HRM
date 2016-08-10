using HRPortal.Common;
using HRPortal.Common.Enums;
using HRPortal.Helper;
using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace HRPortal
{
    [LogActionFilter]
    [Authorize(Roles = "Admin,SuperUser")]
    public class VendorController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();

        // GET: Vendor
        public ActionResult Index()
        {
            return View(db.VENDOR_MASTER.Where(i=>i.ISACTIVE== true).ToList());
        }

        // GET: Vendor/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VENDOR_MASTER vENDOR_MASTER = db.VENDOR_MASTER.Find(id);
            if (vENDOR_MASTER == null)
            {
                return HttpNotFound();
            }
            return View(vENDOR_MASTER);
        }

        // GET: Vendor/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Vendor/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VENDOR_ID,VENDOR_NAME,VENDOR_SPOC,EMAIL,VENDOR_CONTACT_NO")] VendorMasterViewModel vendor, FormCollection frm)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    VENDOR_MASTER vENDOR_MASTER = new VENDOR_MASTER();
                    vENDOR_MASTER.VENDOR_NAME = vendor.VENDOR_NAME;
                    vENDOR_MASTER.VENDOR_SPOC = vendor.VENDOR_SPOC;
                    vENDOR_MASTER.EMAIL = vendor.EMAIL;
                    vENDOR_MASTER.VENDOR_CONTACT_NO = vendor.VENDOR_CONTACT_NO;
                    vENDOR_MASTER.VENDOR_ID = Guid.NewGuid();
                   // vENDOR_MASTER.VENDOR_TYPE = frm["PartnerType"]; //P-Permanent;C-Contract;B-Both
                    vENDOR_MASTER.CREATED_BY= HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                    vENDOR_MASTER.CREATED_ON = DateTime.Now;
                    vENDOR_MASTER.ISACTIVE = true;
                    db.VENDOR_MASTER.Add(vENDOR_MASTER);
                    db.SaveChanges();
                    return RedirectToAction("Index");
            }

            return View(vendor);
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: Vendor/Edit/5
        public ActionResult Edit(Guid? id)
        {
            try { 
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VENDOR_MASTER vENDOR_MASTER = db.VENDOR_MASTER.Find(id);
            if (vENDOR_MASTER == null)
            {
                return HttpNotFound();
            }

                VendorMasterViewModel vendor = new VendorMasterViewModel();
                vendor.VENDOR_ID = vENDOR_MASTER.VENDOR_ID;
                vendor.VENDOR_NAME = vENDOR_MASTER.VENDOR_NAME;
                vendor.VENDOR_SPOC = vENDOR_MASTER.VENDOR_SPOC;
                vendor.EMAIL = vENDOR_MASTER.EMAIL;
                vendor.VENDOR_CONTACT_NO = vENDOR_MASTER.VENDOR_CONTACT_NO;
               // vendor.VENDOR_TYPE = vENDOR_MASTER.VENDOR_TYPE; //P-Permanent;C-Contract;B-Both
                vendor.MODIFIED_BY = vENDOR_MASTER.MODIFIED_BY;
                vendor.MODIFIED_ON = vENDOR_MASTER.MODIFIED_ON;
                vendor.CREATED_BY = vENDOR_MASTER.CREATED_BY;
                vendor.CREATED_ON = vENDOR_MASTER.CREATED_ON;
                vendor.ISACTIVE = vENDOR_MASTER.ISACTIVE;

                return View(vendor);
            }
            catch (Exception ex) { throw ex; }
        }

        // POST: Vendor/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VENDOR_ID,VENDOR_NAME,VENDOR_SPOC,EMAIL,VENDOR_CONTACT_NO,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON")] VendorMasterViewModel vendor, FormCollection frm)
        {
            try
            { 
            if (ModelState.IsValid)
            {
                    VENDOR_MASTER vENDOR_MASTER = db.VENDOR_MASTER.Find(vendor.VENDOR_ID);
                    vENDOR_MASTER.VENDOR_NAME = vendor.VENDOR_NAME;
                    vENDOR_MASTER.VENDOR_SPOC = vendor.VENDOR_SPOC;
                    vENDOR_MASTER.EMAIL = vendor.EMAIL;
                    vENDOR_MASTER.VENDOR_CONTACT_NO = vendor.VENDOR_CONTACT_NO;
                   // vENDOR_MASTER.VENDOR_TYPE = frm["PartnerType"]; //P-Permanent;C-Contract;B-Both
                    vENDOR_MASTER.MODIFIED_BY = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                    vENDOR_MASTER.MODIFIED_ON = DateTime.Now;
                    db.Entry(vENDOR_MASTER).State = EntityState.Modified;
                    db.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(vendor);
            }
            catch(Exception ex)
            { throw ex;
            }
        }

        // GET: Vendor/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VENDOR_MASTER vENDOR_MASTER = db.VENDOR_MASTER.Find(id);
            if (vENDOR_MASTER == null)
            {
                return HttpNotFound();
            }
            return View(vENDOR_MASTER);
        }

        // POST: Vendor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            VENDOR_MASTER vENDOR_MASTER = db.VENDOR_MASTER.Find(id);
           // db.VENDOR_MASTER.Remove(vENDOR_MASTER);
            vENDOR_MASTER.MODIFIED_BY = CookieStore.GetCookie(CacheKey.Uid.ToString()) == null ? User.Identity.Name : CookieStore.GetCookie(CacheKey.Uid.ToString()).ToString();
            vENDOR_MASTER.MODIFIED_ON = DateTime.Now;
            vENDOR_MASTER.ISACTIVE = false;
            db.Entry(vENDOR_MASTER).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
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
    }
}
