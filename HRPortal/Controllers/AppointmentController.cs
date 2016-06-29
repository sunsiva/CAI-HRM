using System;
using System.IO;
using System.Net.Mail;

using System.Web.Mvc;
using System.Net.Mime;
using HRPortal.Models;
using System.Linq;
using DDay.iCal.Serialization.iCalendar;
using DDay.iCal;
using System.Text;
using System.Threading.Tasks;
using HRPortal.Common;
using HRPortal.Common.Enums;

namespace HRPortal.Controllers
{
    [LogActionFilter]
    public class AppointmentController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private AppointmentViewModels appVM = new AppointmentViewModels();
                
        // GET: Appointment
        public ActionResult Index()
        {
           return View();
        }
        
        public string Init()
        {
            bool rslt = false;// Utils.InitialiseDiary();
            return rslt.ToString();
        }

        public void UpdateEvent(int id, string NewEventStart, string NewEventEnd)
        {
            try { 
            appVM.UpdateDiaryEvent(id, NewEventStart, NewEventEnd);
            }
            catch (Exception ex) { throw ex; }
        }
        
        public async Task<bool> SaveEvent(string Title, string NewEventDate, string NewEventTime, string NewEventDuration, string sendTo)
        {
            try
            {
                if(System.Configuration.ConfigurationManager.AppSettings["IsAppointmentMail"]=="true")
                {
                    Guid canId = Guid.Parse("BC30BED0-8F41-4CE0-B8BD-DF47B30060CB");
                    await appVM.SendInvite(NewEventDate+" "+NewEventTime, NewEventDuration, sendTo, canId,Title);
                }
                return appVM.SaveEvent(Title, NewEventDate, NewEventTime, NewEventDuration);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult GetDiarySummary(double start, double end)
        {
            try { 
            //var sstart = DateTime.UtcNow.Date.;
            var ApptListForDate = appVM.LoadAppointmentSummaryInDateRange(start, end);
            var eventList = from e in ApptListForDate.ToList()
                            select new
                            {
                                id = e.ID,
                                title = e.Title,
                                start = e.StartDateString,
                                end = e.EndDateString,
                                someKey = e.KeyID,
                                allDay = false
                            };
            var rows = eventList.ToArray();
            return Json(rows, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { throw ex; }
        }

        public JsonResult GetDiaryEvents(double start, double end)
        {
            try { 
            var ApptListForDate = appVM.LoadAllAppointmentsInDateRange(start, end);
            var eventList = from e in ApptListForDate
                            select new
                            {
                                id = e.ID,
                                title = e.Title,
                                start = e.StartDateString,
                                end = e.EndDateString,
                                color = e.StatusColor,
                                className = e.ClassName,
                                someKey = e.KeyID,
                                allDay = false
                            };
            var rows = eventList.ToArray();
            return Json(rows, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { throw ex; }
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