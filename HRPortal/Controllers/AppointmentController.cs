using System;
using System.IO;
using System.Net.Mail;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using System.Web.Mvc;
using System.Net.Mime;

namespace HRPortal.Controllers
{
    public class AppointmentController : Controller
    {
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
        string location = "Meeting room 101";
        string strOrganizerMail = "sivaprakasams@gmail.com";
        string mailTo = "sivaprakasam_Sundaram@compaid.co.in";
        string mailFrom = "sivaprakasam_Sundaram@compaid.co.in";
        // 1: High
        // 5: Normal
        // 9: low
        int priority = 1;

        // GET: Appointment
        public ActionResult Index()
        {
            //=====================================
            MailMessage message = new MailMessage();
            message.From = new MailAddress(mailFrom);
            message.To.Add(new MailAddress(mailTo));
            message.Subject = "[Candidate Schedule] " + VisBetreff;
            message.Body = bodyPlainText; // Plain Text Version

            // HTML Version
            string htmlBody = bodyHtml;
            AlternateView HTMLV = AlternateView.CreateAlternateViewFromString(htmlBody, new ContentType("text/html"));

            // iCal
            IICalendar iCal = new iCalendar();
            iCal.Method = METHOD;
            iCal.ProductID = "My Metting Product";

            // Create an event and attach it to the iCalendar.
            Event evt = iCal.Create<Event>();
            evt.UID = uid.ToString();
            evt.Class = "PUBLIC";
            // Needed by Outlook
            evt.Created = new iCalDateTime(DateTime.Now);
            evt.DTStamp = new iCalDateTime(DateTime.Now);
            evt.Transparency = TransparencyType.Transparent;

            // Set the event start / end times
            evt.Start = new iCalDateTime(2016, 5, 10, 13, 30, 0);
            evt.End = new iCalDateTime(2016, 5, 10, 13, 45, 0);
            evt.Location = location;

            var organizer = new Organizer(strOrganizerMail);
            evt.Organizer = organizer;

            // Set the longer description of the event, plain text
            evt.Description = bodyPlainText;

            // Event description HTML text
            // X-ALT-DESC;FMTTYPE=text/html
            var prop = new CalendarProperty("X-ALT-DESC");
            prop.AddParameter("FMTTYPE", "text/html");
            prop.AddValue(bodyHtml);
            evt.AddProperty(prop);

            // Set the one-line summary of the event
            evt.Summary = VisBetreff;
            evt.Priority = priority;

            //--- attendees are optional
            IAttendee at = new Attendee("mailto:sivaprakasams@gmail.com");
            at.ParticipationStatus = "NEEDS-ACTION";
            at.RSVP = true;
            at.Role = "REQ-PARTICIPANT";
            evt.Attendees.Add(at);

            // Let’s also add an alarm on this event so we can be reminded of it later.
            Alarm alarm = new Alarm();
            alarm.Action = AlarmAction.Display; // Display the alarm somewhere on the screen.
            alarm.Summary = "Upcoming meeting: " + VisBetreff; // This is the text that will be displayed for the alarm.
            alarm.Trigger = new Trigger(TimeSpan.FromMinutes(-15)); // The alarm is set to occur 30 minutes before the event

            // Add an attachment to this event
            //string filename = "Test.docx";
            //IAttachment attachment = new DDay.iCal.Attachment();
            //attachment.Data = ReadBinary(@"C:\temp\Test.docx");
            //attachment.Parameters.Add("X-FILENAME", filename);
            //evt.Attachments.Add(attachment);

            iCalendarSerializer serializer = new iCalendarSerializer();
            serializer.Serialize(iCal, filepath);

            // the .ics File as a string
            string iCalStr = serializer.SerializeToString(iCal);

            // .ics as AlternateView (used by Outlook)
            // text/calendar part: method=REQUEST
            ContentType calendarType = new ContentType("text/calendar");
            calendarType.Parameters.Add("method", METHOD);
            AlternateView ICSview = AlternateView.CreateAlternateViewFromString(iCalStr, calendarType);

            // Compose
            message.AlternateViews.Add(HTMLV);
            message.AlternateViews.Add(ICSview); // must be the last part

            // .ics as Attachment (used by mail clients other than Outlook)
            Byte[] bytes = System.Text.Encoding.ASCII.GetBytes(iCalStr);
            var ms = new MemoryStream(bytes);
            var a = new System.Net.Mail.Attachment(ms, "CandidateSchedule.ics", "text/calendar");
            message.Attachments.Add(a);

            // Send Mail
            SmtpClient client = new SmtpClient();
            client.Send(message);

            return RedirectToAction("Index","Dashboard");
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
    }
}