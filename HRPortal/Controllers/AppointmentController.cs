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
            setInvite();
            return true;// appVM.SaveEvent(Title, NewEventDate, NewEventTime, NewEventDuration);
        }

        public JsonResult GetDiarySummary(double start, double end)
        {
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

        public JsonResult GetDiaryEvents(double start, double end)
        {
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
        //    evt.Organizer = organizer;

        //    evt.UID = eventId;

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

        //    System.Net.Mail.Attachment att = System.Net.Mail.Attachment.CreateAttachmentFromString(serializer.SerializeToString(), new ContentType("text / calendar"));
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

        private void setInvite()
        {
            MailMessage msg = new MailMessage();
            //Now we have to set the value to Mail message properties

            //Note Please change it to correct mail-id to use this in your application
            msg.From = new MailAddress(mailFrom);
            msg.To.Add(new MailAddress(mailTo, "sunsiv"));
            msg.CC.Add(new MailAddress("reachsunsiva@gmail.com", "ss"));// it is optional, only if required
            msg.Subject = "Send mail with ICS file as an Attachment";
            msg.Body = "Please Attend the meeting with this schedule";

            // Now Contruct the ICS file using string builder
            StringBuilder str = new StringBuilder();
            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("PRODID:-//Schedule a Meeting");
            str.AppendLine("VERSION:2.0");
            str.AppendLine("METHOD:PUBLISH");
            str.AppendLine("BEGIN:VEVENT");
            str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", DateTime.Now.AddMinutes(+330)));
            str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
            str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", DateTime.Now.AddMinutes(+660)));
            str.AppendLine("LOCATION: " + "meeting room");
            str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
            str.AppendLine(string.Format("DESCRIPTION:{0}", msg.Body));
            str.AppendLine(string.Format("X-ALT-DESC;FMTTYPE=text/html:{0}", msg.Body));
            str.AppendLine(string.Format("SUMMARY:{0}", msg.Subject));
            str.AppendLine(string.Format("ORGANIZER:MAILTO:{0}", msg.From.Address));

            str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", msg.To[0].DisplayName, msg.To[0].Address));

            str.AppendLine("BEGIN:VALARM");
            str.AppendLine("TRIGGER:-PT15M");
            str.AppendLine("ACTION:DISPLAY");
            str.AppendLine("DESCRIPTION:Reminder");
            str.AppendLine("END:VALARM");
            str.AppendLine("END:VEVENT");
            str.AppendLine("END:VCALENDAR");

            //Now sending a mail with attachment ICS file.                     
            ContentType contype = new ContentType("text/calendar");
            contype.Parameters.Add("method", "PUBLISH");
            contype.Parameters.Add("name", "Meeting.ics");
            AlternateView avCal = AlternateView.CreateAlternateViewFromString(str.ToString(), contype);
            msg.AlternateViews.Add(avCal);
            
            var c = new SmtpClient();
            c.Send(msg);
        }
    }
}