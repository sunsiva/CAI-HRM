using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRPortal.Helper;

namespace HRPortal.Models
{
    public class AppointmentViewModels
    {
        DiaryEvent dEv = new DiaryEvent();
        public List<DiaryEvent> LoadAllAppointmentsInDateRange(double start, double end)
        {
            var fromDate = ConvertFromUnixTimestamp(start);
            var toDate = ConvertFromUnixTimestamp(end);
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

        private JsonResult Json(object[] rows, JsonRequestBehavior allowGet)
        {
            throw new NotImplementedException();
        }

        private DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

    }
}