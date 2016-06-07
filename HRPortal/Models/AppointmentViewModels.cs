using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRPortal.Helper;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using HRPortal.Common;

namespace HRPortal.Models
{
    public class AppointmentViewModels
    {
        DiaryEvent dEv = new DiaryEvent();
        HRPortalEntities dbContext = new HRPortalEntities();

        public List<DiaryEvent> LoadAllAppointmentsInDateRange(double start, double end)
        {
            var fromDate = ConvertFromUnixTimestamp(start);
            TimeSpan ts = new TimeSpan(00, 01, 0);
            fromDate = fromDate.Date + ts;
            var toDate = ConvertFromUnixTimestamp(end);
            TimeSpan to = new TimeSpan(23, 59, 0);
            toDate = toDate.Date + to;
            using (HRPortalEntities ent = new HRPortalEntities())
            {
                var rslt = ent.EVENTSCHEDULEs.Where(s => s.DATETIMESCHEDULED >= 
                    fromDate && System.Data.Entity.DbFunctions.AddMinutes(
                    s.DATETIMESCHEDULED, s.APPOINTMENTLENGTH) <= toDate);
                List<DiaryEvent> result = new List<DiaryEvent>();
                foreach (var item in rslt)
                {
                    DiaryEvent rec = new DiaryEvent();
                    rec.ID = item.ID;
                    rec.KeyID = item.KEY;
                    rec.StartDateString = item.DATETIMESCHEDULED.ToString("s");
                    // "s" is a preset format that outputs as: "2009-02-27T12:12:22"

                    rec.EndDateString = item.DATETIMESCHEDULED.AddMinutes(item.APPOINTMENTLENGTH).ToString("s");
                    // field AppointmentLength is in minutes

                    rec.Title = item.TITLE + " - " + item.APPOINTMENTLENGTH.ToString() + " mins";
                    rec.StatusString = Enums.GetName((AppointmentStatus)item.STATUSENUM);
                    rec.StatusColor = Enums.GetEnumDescription<AppointmentStatus>(rec.StatusString);
                    string ColorCode = rec.StatusColor.Substring(0, rec.StatusColor.IndexOf(":"));
                    rec.ClassName = rec.StatusColor.Substring(rec.StatusColor.IndexOf(":") + 1,
                                    rec.StatusColor.Length - ColorCode.Length - 1);
                    rec.StatusColor = ColorCode;
                    result.Add(rec);
                }
                return result;
            }
        }

        public  List<DiaryEvent> LoadAppointmentSummaryInDateRange(double start, double end)
        {
            var fromDate = ConvertFromUnixTimestamp(start);
            var toDate = ConvertFromUnixTimestamp(end);
            using (HRPortalEntities ent = new HRPortalEntities())
            {
                var rslt = ent.EVENTSCHEDULEs.Where(
                   s => s.DATETIMESCHEDULED >= fromDate &&
                    System.Data.Entity.DbFunctions.AddMinutes(s.DATETIMESCHEDULED, s.APPOINTMENTLENGTH) <= toDate)
                   .GroupBy(s => System.Data.Entity.DbFunctions.TruncateTime(s.DATETIMESCHEDULED))
                   .Select(x => new { DateTimeScheduled = x.Key, Count = x.Count() });
                List<DiaryEvent> result = new List<DiaryEvent>();
                int i = 0;
                foreach (var item in rslt)
                {
                    DiaryEvent rec = new DiaryEvent();
                    rec.ID = i; //we dont link this back to anything as its a group summary
                                // but the fullcalendar needs unique IDs for each event item (unless its a repeating event)

                    rec.KeyID = -1;
                    string StringDate = string.Format("{0:yyyy-MM-dd}", item.DateTimeScheduled);
                    rec.StartDateString = StringDate + "T00:00:00"; //ISO 8601 format
                    rec.EndDateString = StringDate + "T23:59:59";
                    rec.Title = "Booked: " + item.Count.ToString();
                    result.Add(rec);
                    i++;
                }
                return result;
            }
        }

        public void UpdateDiaryEvent(int id, string NewEventStart, string NewEventEnd)
        {
            // EventStart comes ISO 8601 format, eg:  "2000-01-10T10:00:00Z" - need to convert to DateTime
            using (HRPortalEntities ent = new HRPortalEntities())
            {
                var rec = ent.EVENTSCHEDULEs.FirstOrDefault(s => s.ID == id);
                if (rec != null)
                {
                    DateTime DateTimeStart = DateTime.Parse(NewEventStart, null,
                       System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime(); // and convert offset to localtime
                    rec.DATETIMESCHEDULED = DateTimeStart;
                    if (!String.IsNullOrEmpty(NewEventEnd))
                    {
                        TimeSpan span = DateTime.Parse(NewEventEnd, null,
                           System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime() - DateTimeStart;
                        rec.APPOINTMENTLENGTH = Convert.ToInt32(span.TotalMinutes);
                    }
                    ent.SaveChanges();
                }
            }
        }

        public void UpdateEvent(int id, string NewEventStart, string NewEventEnd)
        {
            dEv.UpdateDiaryEvent(id, NewEventStart, NewEventEnd);
        }


        public bool SaveEvent(string Title, string NewEventDate, string NewEventTime, string NewEventDuration)
        {
            return dEv.CreateNewEvent(Title, NewEventDate, NewEventTime, NewEventDuration);
        }

        public JsonResult GetDiarySummary(double start, double end)
        {
            var ApptListForDate = dEv.LoadAppointmentSummaryInDateRange(start, end);
            var eventList = from e in ApptListForDate
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

        private JsonResult Json(object[] rows, JsonRequestBehavior allowGet)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetDiaryEvents(double start, double end)
        {
            var ApptListForDate = dEv.LoadAllAppointmentsInDateRange(start, end);
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
        
        public async Task SendInvite(string NewEventDate, string NewEventDuration, string sendTo, Guid canId)
        {
            try
            { 
                string serverPath = System.Configuration.ConfigurationManager.AppSettings["DocPathAppointment"];
                string filepath = Path.Combine(serverPath+@"ical\", "ical.test.ics");
                // use PUBLISH for appointments
                // use REQUEST for meeting requests
                const string METHOD = "REQUEST";
                DateTime evtSDate = DateTime.Parse(NewEventDate);
                DateTime evtEDate = evtSDate.AddMinutes(int.Parse(NewEventDuration));

                //Get data for selected candidate
                var canLst = dbContext.CANDIDATES.Where(x => x.CANDIDATE_ID == canId).FirstOrDefault();
                string canName = (canLst == null ? string.Empty : canLst.CANDIDATE_NAME);

                var job = dbContext.JOBPOSTINGs.Where(x => x.JOB_ID == canLst.JOB_ID).FirstOrDefault();
                var uid = HelperFuntions.HasValue(System.Web.HttpRuntime.Cache.Get(CacheKey.Uid.ToString()));
                var usr = dbContext.AspNetUsers.Where(x => x.Id == uid).FirstOrDefault();
                string UserName = "Admin";
                string UserEmail = "Naveen_Sankar@compaid.co.in";
                if(usr != null)
                {
                    UserName = usr.FirstName + " " + usr.LastName;
                    UserEmail = usr.Email;
                }
                // Properties of the meeting request
                // keep guid in sending program to modify or cancel the request later
                string strSubject = "Interview Scheduled for "+ canName;
                string toEmail = "Chandrashekhar_Yarashi@compaid.co.in";// "Mohan_Kumar@compaid.co.in";// "Chandrashekhar_Yarashi@compaid.co.in";// "Nagaraju_Chinnapalle@compaid.co.in";
                string bodyPlainText = "Hi, Interview has been scheduled for the possition of "+job.POSITION_NAME+". Please let me know if you any quries on this. Regards, "+UserName;
                string bodyHtml = "Hi, <br> Interview has been scheduled for the possition of <b>" + job.POSITION_NAME + "</b>. <br> Please let me know if you any quries on this. <br> Regards,<br><b>" + UserName +"</b>";
                string location = "Available";
                string organizerMail = UserEmail;
                string filename = "Test.txt";//--- Attachments
                int priority = 1;// 1: High; 5: Normal; 9: low
                //=====================================

                MailMessage message = new MailMessage();
                message.From = new MailAddress(HRPConst.PRIM_EMAIL_FROM, UserName);
                string[] mailToadrs = sendTo.Split(',');
                for (int i = 0; i < mailToadrs.Length; i++)
                    message.To.Add(new MailAddress(mailToadrs[i]));
                message.Bcc.Add(new MailAddress("reachsunsiva2015@gmail.com", UserName));
                message.Subject = strSubject;
                message.Body = bodyPlainText; // Plain Text Version

                // HTML Version
                string htmlBody = bodyHtml;
                AlternateView HTMLV = AlternateView.CreateAlternateViewFromString(htmlBody,
                  new System.Net.Mime.ContentType("text/html"));

                // iCal
                IICalendar iCal = new iCalendar();
                iCal.Method = METHOD;
                iCal.ProductID = "Meeting";

                // Create an event and attach it to the iCalendar.
                Event evt = iCal.Create<Event>();
                evt.UID = canId.ToString();
                evt.Class = "PUBLIC";
                evt.Created = new iCalDateTime(DateTime.Now);
                evt.DTStamp = new iCalDateTime(DateTime.Now);
                evt.Transparency = TransparencyType.Transparent;

                // Set the event start / end times
                evt.Start = new iCalDateTime(evtSDate.Year, evtSDate.Month, evtSDate.Day, evtSDate.Hour, evtSDate.Minute, evtSDate.Second);
                evt.End = new iCalDateTime(evtEDate.Year, evtEDate.Month, evtEDate.Day, evtEDate.Hour, evtEDate.Minute, evtEDate.Second);
                evt.Location = location;

                var organizer = new Organizer(organizerMail);
                evt.Organizer = organizer;
                evt.Description = bodyPlainText; // Set the longer description of the event, plain text

                // Event description HTML text
                // X-ALT-DESC;FMTTYPE=text/html
                var prop = new CalendarProperty("X-ALT-DESC");
                prop.AddParameter("FMTTYPE", "text/html");
                prop.AddValue(bodyHtml);
                evt.AddProperty(prop);

                // Set the one-line summary of the event
                evt.Summary = strSubject;
                evt.Priority = priority;

                //--- attendes are optional
                IAttendee at = new Attendee("mailto:"+organizerMail+"");
                at.ParticipationStatus = "NEEDS-ACTION";
                at.RSVP = true;
                at.Role = "REQ-PARTICIPANT";
                at.CommonName = UserName;
                evt.Attendees.Add(at);

                // Let’s also add an alarm on this event so we can be reminded of it later.
                Alarm alarm = new Alarm();

                // Display the alarm somewhere on the screen.
                alarm.Action = AlarmAction.Display;

                // This is the text that will be displayed for the alarm.
                alarm.Summary = "Upcoming meeting: " + strSubject;

                // The alarm is set to occur 15 minutes before the event
                alarm.Trigger = new Trigger(TimeSpan.FromMinutes(-15));

                // Add an attachment to this event
                filename = Path.Combine(serverPath, canLst.RESUME_FILE_PATH);
                if (!string.IsNullOrEmpty(filename)) { 
                    IAttachment attachment = new DDay.iCal.Attachment();
                    attachment.Data = ReadBinary(filename);
                    attachment.Parameters.Add("X-FILENAME", filename);
                    evt.Attachments.Add(attachment);
                }

                iCalendarSerializer serializer = new iCalendarSerializer();
                serializer.Serialize(iCal, filepath);

                // the .ics File as a string
                string iCalStr = serializer.SerializeToString(iCal);

                // .ics as AlternateView (used by Outlook)
                // text/calendar part: method=REQUEST
                System.Net.Mime.ContentType calendarType =
                  new System.Net.Mime.ContentType("text/calendar");
                calendarType.Parameters.Add("method", METHOD);
                AlternateView ICSview =
                  AlternateView.CreateAlternateViewFromString(iCalStr, calendarType);

                // Compose
                message.AlternateViews.Add(HTMLV);
                message.AlternateViews.Add(ICSview); // must be the last part

                // .ics as Attachment (used by mail clients other than Outlook)
                Byte[] bytes = System.Text.Encoding.ASCII.GetBytes(iCalStr);
                var ms = new System.IO.MemoryStream(bytes);
                var a = new System.Net.Mail.Attachment(ms,
                  "HROpsMeetingRequest.ics", "text/calendar");
                message.Attachments.Add(a);

                // Send Mail
                SmtpClient client = new SmtpClient();
                await client.SendMailAsync(message);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

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
        
        private DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

    }
}