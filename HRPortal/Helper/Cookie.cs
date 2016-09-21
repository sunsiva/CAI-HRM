using System;
using System.Web;

namespace HRPortal.Helper
{
    public class CookieStore
    {
        public static void SetCookie(string key, string value, TimeSpan expires)
        {
            //HttpCookie encodedCookie = HttpSecureCookie.Encode(new HttpCookie(key, value));
            HttpCookie encodedCookie = new HttpCookie(key, value);

            if (HttpContext.Current.Request.Cookies[key] != null)
            {
                var cookieOld = HttpContext.Current.Request.Cookies[key];
                cookieOld.Expires = HelperFuntions.GetDateTime().Add(expires);
                cookieOld.Value = encodedCookie.Value;
                HttpContext.Current.Response.Cookies.Add(cookieOld);
            }
            else
            {
                encodedCookie.Expires = HelperFuntions.GetDateTime().Add(expires);
                HttpContext.Current.Response.Cookies.Add(encodedCookie);
            }
        }

        public static string GetCookie(string key)
        {
            string value = string.Empty;
            HttpCookie cookie = HttpContext.Current.Request.Cookies[key];

            if (cookie != null)
            {
                // For security purpose, we need to encrypt the value.
                //HttpCookie decodedCookie = HttpSecureCookie.Decode(cookie);
                //value = decodedCookie.Value;
                value = cookie.Value;
            }
            return value;
        }

        /// <summary>
        /// Clears the cookie value by the key provided...
        /// </summary>
        /// <param name="key"></param>
        public static void ClearCookie(string key)
        {
            var cookie = HttpContext.Current.Request.Cookies[key];
            if(cookie!=null)
            {
                cookie.Expires = HelperFuntions.GetDateTime().AddYears(-1);
                cookie.Value = string.Empty;
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }
    }
}