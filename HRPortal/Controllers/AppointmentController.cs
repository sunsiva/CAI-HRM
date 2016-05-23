using System;
using System.IO;
using System.Net.Mail;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using System.Web.Mvc;
using System.Net.Mime;
using HRPortal.Models;
using System.Linq;

namespace HRPortal.Controllers
{
    public class AppointmentController : Controller
    {
        private HRPortalEntities db = new HRPortalEntities();
        private AppointmentViewModels appVM = new AppointmentViewModels();

        const string filepath = @"D:\source\HDC\HR_Portal\Source\Application\HRPortal\HRPortal\App_Data\ical.test.ics";
        // use PUBLISH for appointments
        // use REQUEST for meeting requests
        const string METHOD = "REQUEST";

        // Properties of the meeting request
        // keep guid in sending program to modify or cancel the request later
        Guid uid = Guid.Parse("2B127C67-73B3-43C5-A804-5666C2CA23C9");
        string VisBetreff = "This is the subject of the meeting request";
        
        string bodyPlainText = "This is the simple iCal plain text msg";
        string bodyHtml = "This is the simple <b>iCal HTML message</b>";
        string location = "Any available meeting room";
        string strOrganizerMail = "sivaprakasams@gmail.com";
        string mailTo = "sivaprakasam_Sundaram@compaid.co.in";
        string mailFrom = "sivaprakasam_Sundaram@compaid.co.in";
        int priority = 1; // 1: High; 5: Normal; 9: low
        
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
            appVM.UpdateDiaryEvent(id, NewEventStart, NewEventEnd);
        }


        public bool SaveEvent(string Title, string NewEventDate, string NewEventTime, string NewEventDuration)
        {
            return appVM.CreateNewEvent(Title, NewEventDate, NewEventTime, NewEventDuration);
        }

        public JsonResult GetDiarySummary(double start, double end)
        {
            //var sstart = DateTime.UtcNow.Date.;
            var ApptListForDate = appVM.LoadAppointmentSummaryInDateRange(start, end);
            var eventList = from e in ApptListForDate.ToList()
                            select new
                            {
                                id = e.ID,
                                title = e.TITLE,
                                start = e.StartDateString,
                                end = e.EndDateString,
                                someKey = e.SOMEIMPORTANTKEY,
                                allDay = false
                            };
            var rows = eventList.ToArray();
            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDiaryEvents(double start, double end)
        {
            var ApptListForDate = appVM.LoadAllAppointmentsInDateRange(start, end);
            var eventList = from e in ApptListForDate
                            select new
                            {
                                id = e.ID,
                                title = e.TITLE,
                                start = e.StartDateString,
                                end = e.EndDateString,
                                color = e.StatusColor,
                                className = e.ClassName,
                                someKey = e.SOMEIMPORTANTKEY,
                                allDay = false
                            };
            var rows = eventList.ToArray();
            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        //private void appointment()
        //{
        //    //Using DDay.iCal 0.7.0
        //    //parameters
        //    string title = "Test";
        //    string body = "Test body";
        //    DateTime startDate = DateTime.Now;
        //    double duration = 1;
        //    string location = "B4F1 Meeting Room";
        //    string organizer = "Chris G";
        //    bool updatePreviousEvent = false;
        //    string eventId = "000832";
        //    bool allDayEvent = false;
        //    int recurrenceDaysInterval = 0;
        //    int recurrenceCount = 0;

        //    iCalendar iCal = new iCalendar();

        //    // outlook 2003 needs this property,
        //    // or we’ll get an error (a Lunar error!)
        //    iCal.Method = "PUBLISH";

        //    // Create the event
        //    Event evt = iCal.Create();

        //    evt.Summary = title;
        //    evt.Start = new iCalDateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second);
        //    evt.Duration = TimeSpan.FromHours(duration);
        //    evt.Description = body;
        //    evt.Location = location;

        //    if (recurrenceDaysInterval > 0)
        //    {
        //        RecurrencePattern rp = new RecurrencePattern();
        //        rp.Frequency = FrequencyType.Daily;
        //        rp.Interval = recurrenceDaysInterval; // interval of days

        //        rp.Count = recurrenceCount;
        //        evt.AddRecurrencePattern(rp);
        //    }
        //    evt.IsAllDay = allDayEvent;

        //    //organizer is mandatory for outlook 2007 – think about
        //    // trowing an exception here.
        //    if (!String.IsNullOrEmpty(organizer)) evt.Organizer = organizer;

        //    if (!String.IsNullOrEmpty(eventId)) evt.UID = eventId;

        //    //"REQUEST" will update an existing event with the same
        //    // UID (Unique ID) and a newer time stamp.
        //    if (updatePreviousEvent) iCal.Method = "REQUEST";

        //    // Save into calendar file.
        //    iCalendarSerializer serializer =
        //    new iCalendarSerializer(iCal);
        //    //serializer.Serialize(@"iCalendar.ics");

        //    MailMessage msg = new MailMessage();
        //    msg.From = new MailAddress("AAA@BBB.com");
        //    msg.To.Add("CCC@BBB.com");
        //    msg.Subject = title;
        //    msg.Body = body;

        //    Attachment att = Attachment.CreateAttachmentFromString(serializer.SerializeToString(), new ContentType("text / calendar"));
        //    att.TransferEncoding = TransferEncoding.Base64;
        //    att.Name = eventId + ".ics";

        //    msg.Attachments.Add(att);

        //    SmtpClient clt = new SmtpClient("mailhost.BBB.com");
        //    try
        //    {
        //        clt.Send(msg);
        //    }
        //    catch { }
        //}

        private static byte[] ReadBinary(string fileName)
        {
            byte[] binaryData = null;
            using (FileStream reader = new FileStream(fileName,
              FileMode.Open, FileAccess.Read))
            {
                binaryData = new byte[reader.Length];
                reader.Read(binaryData, 0, (int)reader.Length);
            }
            return binaryData;
        }
        
        /// <summary>
        /// Alternate way of sending calendar invite.
        /// </summary>
        private void SendRequest()
        {
            var m = new MailMessage();
            m.From = new MailAddress(mailFrom);
            m.To.Add(new MailAddress(mailTo));
            m.Subject = "Meeting";
            m.Body = "My new candidate meeting request";

            string iCal =
                @"BEGIN:VCALENDAR
                PRODID:-//Microsoft Corporation//Outlook 14.0 MIMEDIR//EN
                VERSION:2.0
                METHOD:PUBLISH
                X-MS-OLK-FORCEINSPECTOROPEN:TRUE
                BEGIN:VEVENT
                CLASS:PUBLIC
                CREATED:20140423T045933Z
                DESCRIPTION:desc
                DTEND:20140430T080000Z
                DTSTAMP:20140423T045933Z
                DTSTART:20140430T060000Z
                LAST-MODIFIED:20140423T045933Z
                LOCATION:location...
                PRIORITY:1
                SEQUENCE:0
                SUMMARY;LANGUAGE=en-us:Summary...
                TRANSP:OPAQUE
                UID:D8BFD357-88A7-455C-86BC-C2CECA9AC5C6
                X-MICROSOFT-CDO-BUSYSTATUS:BUSY
                X-MICROSOFT-CDO-IMPORTANCE:1
                X-MICROSOFT-DISALLOW-COUNTER:FALSE
                X-MS-OLK-AUTOFILLLOCATION:FALSE
                X-MS-OLK-CONFTYPE:0
                BEGIN:VALARM
                TRIGGER:-PT60M
                ACTION:DISPLAY
                DESCRIPTION:Reminder
                END:VALARM
                END:VEVENT
                END:VCALENDAR";

            using (var iCalView = AlternateView.CreateAlternateViewFromString(iCal, new ContentType("text/calendar")))
            {
                m.AlternateViews.Add(iCalView);
                var c = new SmtpClient();
                // Send message
                c.Send(m);
            }
        }
    }
}