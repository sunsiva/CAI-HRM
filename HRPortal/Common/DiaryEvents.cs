using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization; // << dont forget to add this for converting dates to localtime
using HRPortal.Helper;
using System.Web;

namespace HRPortal.Models
{
    public class DiaryEvent
    {

        public int ID;
        public string Title;
        public int KeyID;
        public string StartDateString;
        public string EndDateString;
        public string StatusString;
        public string StatusColor;
        public string ClassName;


        public List<DiaryEvent> LoadAllAppointmentsInDateRange(double start, double end)
        {
            var fromDate = ConvertFromUnixTimestamp(start);
            var toDate = ConvertFromUnixTimestamp(end);
            using (HRPortalEntities ent = new HRPortalEntities())
            {
                var rslt = ent.EVENTSCHEDULEs.Where(s => s.DATETIMESCHEDULED >= fromDate && System.Data.Entity.DbFunctions.AddMinutes(s.DATETIMESCHEDULED, s.APPOINTMENTLENGTH) <= toDate);

                List<DiaryEvent> result = new List<DiaryEvent>();
                foreach (var item in rslt)
                {
                    DiaryEvent rec = new DiaryEvent();
                    rec.ID = item.ID;
                    rec.KeyID = item.KEY;
                    rec.StartDateString = item.DATETIMESCHEDULED.ToString("s"); // "s" is a preset format that outputs as: "2009-02-27T12:12:22"
                    rec.EndDateString = item.DATETIMESCHEDULED.AddMinutes(item.APPOINTMENTLENGTH).ToString("s"); // field AppointmentLength is in minutes
                    rec.Title = item.TITLE + " - " + item.APPOINTMENTLENGTH.ToString() + " mins";
                    rec.StatusString = Enums.GetName((AppointmentStatus)item.STATUSENUM);
                    rec.StatusColor = Enums.GetEnumDescription<AppointmentStatus>(rec.StatusString);
                    string ColorCode = rec.StatusColor.Substring(0, rec.StatusColor.IndexOf(":"));
                    rec.ClassName = rec.StatusColor.Substring(rec.StatusColor.IndexOf(":")+1, rec.StatusColor.Length - ColorCode.Length-1);
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
                var rslt = ent.EVENTSCHEDULEs.Where(s => s.DATETIMESCHEDULED >= fromDate && System.Data.Entity.DbFunctions.AddMinutes(s.DATETIMESCHEDULED, s.APPOINTMENTLENGTH) <= toDate)
                                                        .GroupBy(s => System.Data.Entity.DbFunctions.TruncateTime(s.DATETIMESCHEDULED))
                                                        .Select(x => new { DateTimeScheduled = x.Key, Count = x.Count() });

                List<DiaryEvent> result = new List<DiaryEvent>();
                int i = 0;
                foreach (var item in rslt)
                {
                    DiaryEvent rec = new DiaryEvent();
                    rec.ID = i; //we dont link this back to anything as its a group summary but the fullcalendar needs unique IDs for each event item (unless its a repeating event)
                    rec.KeyID = -1;  
                    string StringDate = string.Format("{0:yyyy-MM-dd}", item.DateTimeScheduled);
                    rec.StartDateString = StringDate + "T00:00:00"; //ISO 8601 format
                    rec.EndDateString = StringDate +"T23:59:59";
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
            using (HRPortalEntities ent = new HRPortalEntities()) {
                var rec = ent.EVENTSCHEDULEs.FirstOrDefault(s => s.ID == id);
                if (rec != null)
                {
                    DateTime DateTimeStart = DateTime.Parse(NewEventStart, null, DateTimeStyles.RoundtripKind).ToLocalTime(); // and convert offset to localtime
                    rec.DATETIMESCHEDULED = DateTimeStart;
                    if (!String.IsNullOrEmpty(NewEventEnd)) { 
                        TimeSpan span = DateTime.Parse(NewEventEnd, null, DateTimeStyles.RoundtripKind).ToLocalTime() - DateTimeStart;
                        rec.APPOINTMENTLENGTH = Convert.ToInt32(span.TotalMinutes);
                        }
                    ent.SaveChanges();
                }
            }

        }


        private DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }


        public bool CreateNewEvent(string Title, string NewEventDate, string NewEventTime, string NewEventDuration)
        {
            try
            {
                HRPortalEntities ent = new HRPortalEntities();
                EVENTSCHEDULE rec = new EVENTSCHEDULE();
                Guid uid = HttpRuntime.Cache.Get(CacheKey.Uid.ToString()) == null ? Guid.NewGuid() : Guid.Parse(HttpRuntime.Cache.Get(CacheKey.Uid.ToString()).ToString());
                rec.TITLE = Title;
                rec.DATETIMESCHEDULED = DateTime.Parse(NewEventDate + " " + NewEventTime);//, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                rec.APPOINTMENTLENGTH = int.Parse(NewEventDuration);
                rec.STATUSENUM = 1;
                rec.ISACTIVE = true;
                rec.CREATED_BY = uid;
                rec.CREATED_ON = DateTime.Now;
                ent.EVENTSCHEDULEs.Add(rec);
                ent.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}