using HRPortal.Helper;
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
    public class VendorController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();

        // GET: Vendor
        public ActionResult Index()
        {

            return View(db.VENDOR_MASTER.ToList());
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
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VENDOR_ID,VENDOR_NAME,VENDOR_SPOC,EMAIL,VENDOR_CONTACT_NO,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON")] VENDOR_MASTER vENDOR_MASTER)
        {
            try { 
            if (ModelState.IsValid)
            {
                vENDOR_MASTER.VENDOR_ID = Guid.NewGuid();
                vENDOR_MASTER.CREATED_BY= HelperFuntions.HasValue(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
                vENDOR_MASTER.CREATED_ON = DateTime.Now;
                db.VENDOR_MASTER.Add(vENDOR_MASTER);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(vENDOR_MASTER);
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
            return View(vENDOR_MASTER);
            }
            catch (Exception ex) { throw ex; }
        }

        // POST: Vendor/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VENDOR_ID,VENDOR_NAME,VENDOR_SPOC,EMAIL,VENDOR_CONTACT_NO,ISACTIVE,MODIFIED_BY,MODIFIED_ON,CREATED_BY,CREATED_ON")] VENDOR_MASTER vENDOR_MASTER)
        {
            if (ModelState.IsValid)
            {
                db.Entry(vENDOR_MASTER).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(vENDOR_MASTER);
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
            db.VENDOR_MASTER.Remove(vENDOR_MASTER);
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
    }
}
