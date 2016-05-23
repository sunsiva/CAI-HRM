using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Helper
{
    public class Utils
    {
        private static HRPortalEntities db = new HRPortalEntities();

        //public static bool InitialiseDiary()
        //{
        //    try
        //    {
        //        for (int i = 0; i < 30; i++)
        //        {
        //            AppointmentViewModels item = new AppointmentViewModels();
        //            // record ID is auto generated
        //            item.TITLE = "Appt: " + i.ToString();
        //            item.SOMEIMPORTANTKEY = i;
        //            item.STATUSENUM = GetRandomValue(0, 3); // random is exclusive - we have three status enums
        //            if (i <= 5) // create a few appointments for todays date
        //            {
        //                item.DATETIMESCHEDULED = GetRandomAppointmentTime(false, true);
        //            }
        //            else {  // rest of appointments on previous and future dates
        //                if (i % 2 == 0)
        //                    item.DATETIMESCHEDULED = GetRandomAppointmentTime(true, false); // flip/flop between date ahead of today and behind today
        //                else item.DATETIMESCHEDULED = GetRandomAppointmentTime(false, false);
        //            }
        //            item.APPOINTMENTLENGTH = GetRandomValue(1, 5) * 15; // appoiment length in blocks of fifteen minutes in this demo
        //            db.EVENTSCHEDULEs.Add(item);
        //            db.SaveChanges();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }

        //    return ent.AppointmentDiary.Count() > 0;        
        //}

        public static int GetRandomValue(int LowerBound, int UpperBound) {
            Random rnd = new Random();
            return rnd.Next(LowerBound, UpperBound); 
        }

        /// <summary>
        /// sends back a date/time +/- 15 days from todays date
        /// </summary>
        public static DateTime GetRandomAppointmentTime(bool GoBackwards, bool Today) {
            Random rnd = new Random(Environment.TickCount); // set a new random seed each call
            var baseDate = DateTime.Today;
            if (Today)
                return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, rnd.Next(9, 18), rnd.Next(1, 6)*5, 0);
            else
            {
                int rndDays = rnd.Next(1, 16);
                if (GoBackwards)
                    rndDays = rndDays * -1; // make into negative number
                return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, rnd.Next(9, 18), rnd.Next(1, 6)*5, 0).AddDays(rndDays);             
            }
        }

    }
}