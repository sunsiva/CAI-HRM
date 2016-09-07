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
using System.Web;
using System.Net;

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

        public List<DiaryEvent> LoadAppointmentSummaryInDateRange(double start, double end)
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

        /// <summary>
        /// To trigger e-mail to profile owner when the status changing to "To Be Scheduled".
        /// </summary>
        /// <param name="canId"></param>
        /// <param name="comments"></param>
        /// <returns></returns>
        public async Task<string> sendMailTBS(Guid canId, string comments, string date, string length)
        {
            try
            {
                var canLst = dbContext.CANDIDATES.Where(x => x.CANDIDATE_ID == canId).FirstOrDefault();
                string canName = (canLst == null ? string.Empty : canLst.CANDIDATE_NAME);
                var job = dbContext.JOBPOSTINGs.Where(x => x.JOB_ID == canLst.JOB_ID).FirstOrDefault();
                var uid = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                List<AspNetUser> lstUsrs = dbContext.AspNetUsers.ToList();
                var profOwner = lstUsrs.Where(x => x.Id == canLst.CREATED_BY).FirstOrDefault();
                var vid = lstUsrs.Where(u => u.Id == profOwner.Id).Select(v => v.Vendor_Id).FirstOrDefault().ToString();
                var lstSuperuser = (from u in lstUsrs
                                    join ur in dbContext.UserXRoles on Guid.Parse(u.Id) equals ur.UserId
                                    join r in dbContext.AspNetRoles on ur.RoleId equals Guid.Parse(r.Id)
                                    where (r.Name.ToUpper().Contains("SUPERUSER") && u.IsActive == true && u.Vendor_Id == Guid.Parse(vid)
                                     && u.Email != profOwner.Email)
                                    select u.Email).ToList();

                string UserName = CookieStore.GetCookie(CacheKey.UserName.ToString());
                string bccs = System.Configuration.ConfigurationManager.AppSettings["BCCMailIdForMonitor"];
                string strSubject = "HROps-Interview To Be Scheduled For " + canName;
                string bodyHtml = "Hi " + profOwner.FirstName + ", <br><br> FYI - Interview to be scheduled for the candidate <b>" + canName + " </b> on <b>" + date + "</b> for " + length + " minutes and for the position of <b>" + job.POSITION_NAME +
                    "</b>.<br><br>USER COMMENTS: " + comments + " <br> <br> <br> Regards,<br><b>" + UserName + "</b>. <br><br><small>NOTE:- This is a system generated e-mail(www.caihrops.in). Please do not reply to this e-mail.</small>";
                //=====================================

                MailMessage message = new MailMessage();
                message.From = new MailAddress(HRPConst.PRIM_EMAIL_FROM, UserName);
                message.To.Add(new MailAddress(profOwner.Email));

                foreach (var item in lstSuperuser)
                    message.CC.Add(new MailAddress(item));

                string[] mailBccs = bccs.Split(',');
                for (int i = 0; i < mailBccs.Length; i++)
                    message.Bcc.Add(new MailAddress(mailBccs[i]));
                message.Subject = strSubject;
                message.Body = bodyHtml;

                // HTML Version
                AlternateView HTMLV = AlternateView.CreateAlternateViewFromString(bodyHtml,
                  new System.Net.Mime.ContentType("text/html"));
                message.AlternateViews.Add(HTMLV);

                // Send Mail
                SmtpClient client = new SmtpClient();
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "Mail Sent";
        }


        /// <summary>
        /// To trigger e-mail to all in the system when creating new job.
        /// </summary>
        /// <param name="canId"></param>
        /// <returns></returns>
        //public async Task<string> sendMailForNewJob(JOBPOSTING jobposting, bool isNew, string[] lstPartner)
        public async Task<string> sendMailForNewJob(JOBPOSTING jobposting, bool isNew)
        {
            //NOTE:- No Email should go in "To Address" - all email should be in BCC.
            try
            {
                var mailBCCs = dbContext.AspNetUsers.Where(u => u.IsActive == true).Select(u => u.Email).ToList();
                //For specific vendor to send out email
                //var mailBCCs = (from u in dbContext.AspNetUsers
                //                where (lstPartner.Contains(u.Vendor_Id.ToString()))
                //                select (u.Email)).ToList();

                string UserName = CookieStore.GetCookie(CacheKey.UserName.ToString());
                string strSubject = (isNew ? "HROps-New Job Posted - " : "HROps-Job Modified - ") + jobposting.POSITION_NAME;
                string bodyHtml = "Hi, <br><br> A " + (isNew ? "new" : "modified") + " position is posted with a job code <b>" + jobposting.JOB_CODE + " </b>. Below is the position detail, <br><br> <b>Position Name:</b> " + jobposting.POSITION_NAME +
                    "</b><br><br><b>Position Description:</b> " + jobposting.JOB_DESCRIPTION + " <br> <br> <br> Regards,<br><b> Admin </b>. <br><br><small>NOTE:- This is a system generated e-mail(www.caihrops.in). Please do not reply to this e-mail.</small>";
                //===================================================================================

                MailMessage message = new MailMessage();
                message.From = new MailAddress(HRPConst.PRIM_EMAIL_FROM, "CAI Admin");
                foreach (var item in mailBCCs)
                    message.Bcc.Add(new MailAddress(item));

                message.Subject = strSubject;
                message.Body = bodyHtml;

                // HTML Version
                AlternateView HTMLV = AlternateView.CreateAlternateViewFromString(bodyHtml,
                  new System.Net.Mime.ContentType("text/html"));
                message.AlternateViews.Add(HTMLV);

                // Add an attachment to email
                string filename = jobposting.JD_FILE_PATH;
                if (!string.IsNullOrEmpty(filename))
                {
                    try
                    {
                        message.Attachments.Add(new System.Net.Mail.Attachment(filename));
                    }
                    catch (Exception ex)
                    { }
                }

                // Send Mail
                SmtpClient client = new SmtpClient();
                await client.SendMailAsync(message);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "Mail Sent";
        }

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

        public async Task SendInvite(string NewEventDate, string NewEventDuration, string sendTo, Guid canId, string comments,bool isReSchedul)
        {
            try
            {
                string serverPath = HttpContext.Current.Server.MapPath("~/UploadDocument/");
                string filepath = Path.Combine(serverPath + @"ical\", "ical.test.ics");
                // use PUBLISH for appointments
                // use REQUEST for meeting requests
                const string METHOD = "REQUEST";
                DateTime evtSDate = DateTime.Parse(NewEventDate);
                DateTime evtEDate = evtSDate.AddMinutes(int.Parse(NewEventDuration));

                //Get data for selected candidate
                var canLst = dbContext.CANDIDATES.Where(x => x.CANDIDATE_ID == canId).FirstOrDefault();
                string canName = (canLst == null ? string.Empty : canLst.CANDIDATE_NAME);

                var job = dbContext.JOBPOSTINGs.Where(x => x.JOB_ID == canLst.JOB_ID).FirstOrDefault();
                var uid = HelperFuntions.HasValue(CookieStore.GetCookie(CacheKey.Uid.ToString()));
                var usr = dbContext.AspNetUsers.Where(x => x.Id == uid).FirstOrDefault();
                string UserName = "CAI HROps-Admin";
                string UserEmail = HRPConst.PRIM_EMAIL_FROM;
                string bccs = System.Configuration.ConfigurationManager.AppSettings["BCCMailIdForMonitor"];
                if (usr != null)
                {
                    UserName = usr.FirstName + " " + usr.LastName;
                    UserEmail = usr.Email;
                }
                // Properties of the meeting request
                // keep guid in sending program to modify or cancel the request later
                string strSubject = "HROps-Interview Scheduled For -" + canName;
                string bodyPlainText = "Hi, Interview has been scheduled for the position of " + job.POSITION_NAME + ". Regards, " + UserName + ".";
                if(isReSchedul)
                {
                    strSubject = "HROps-Interview Re-Scheduled For -" + canName;
                    bodyPlainText = "Hi, Interview has been re-scheduled for the position of " + job.POSITION_NAME + ". Regards, " + UserName + ".";
                }

                string bodyHtml = "Hi, <br><br> Interview has been scheduled for the position of <b>" + job.POSITION_NAME +
                    "</b>. Attached candidate profile for your reference. <br> <br>USER COMMENTS: " + comments + "<br> <br> Regards,<br><b>" + UserName + "</b>. <br><br><small>NOTE:- This is a system generated e-mail(www.caihrops.in). Please do not reply to this e-mail.</small>";
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

                message.CC.Add(new MailAddress(organizerMail));

                string[] mailBccs = bccs.Split(',');
                for (int i = 0; i < mailBccs.Length; i++)
                    message.Bcc.Add(new MailAddress(mailBccs[i]));

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
                IAttendee at = new Attendee("mailto:" + organizerMail + "");
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
                if (!string.IsNullOrEmpty(filename))
                {
                    try
                    {
                        // Add an attachment to this event
                        //IAttachment attachment = new DDay.iCal.Attachment();
                        //attachment.Data = ReadBinary(filename);
                        //attachment.Parameters.Add("X-FILENAME", filename);
                        //evt.Attachments.Add(attachment);

                        // Add an attachment to email
                        message.Attachments.Add(new System.Net.Mail.Attachment(filename));
                    }
                    catch (Exception ex)
                    { }
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
                  "HROpsInvite.ics", "text/calendar");
                message.Attachments.Add(a);

                // Send Mail
                SmtpClient client = new SmtpClient();
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get the candidates schedules for the particular day
        /// </summary>
        /// <param name="scheduleDt"></param>
        /// <returns></returns>
        public List<CandidateViewModels> GetCandidateSchedules(DateTime scheduleDt)
        {
            try
            {
                var schedules = dbContext.getCandidatesSchedules(scheduleDt.ToString("dd/MM/yyyy")).ToList();
                if(schedules.Count()>0)
                {
                    var lstOfSchedules = schedules.Select(i => new CandidateViewModels
                    {
                        CANDIDATE_ID = i.CANDIDATE_ID,
                        CANDIDATE_NAME = i.CANDIDATE_NAME,
                        POSITION = i.POSITION,
                        RESUME_FILE_PATH = string.IsNullOrEmpty(i.RESUME_FILE_PATH) ? string.Empty : Path.Combine("/UploadDocument/", i.RESUME_FILE_PATH),
                        NOTICE_PERIOD = i.NOTICE_PERIOD,
                        YEARS_OF_EXP_TOTAL = i.YEARS_OF_EXP_TOTAL,
                        LAST_WORKING_DATE = i.LAST_WORKING_DATE,
                        VENDOR_NAME = i.VENDOR_NAME,
                        STATUS = i.STATUS,
                        STATUS_ID = i.STATUS_ID.ToString(),
                        CREATED_ON = i.CREATED_ON,
                        SCHEDULED_TO = i.SCHEDULED_ON,
                        SCHEDULED_LENGTH = i.SCHEDULED_LENGTH.ToString(),
                        MODIFIED_BY=i.MODIFIED_BY
                    }).ToList();
                    return lstOfSchedules;
                }
                return  new List<CandidateViewModels>();
            }
            catch (Exception ex)
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