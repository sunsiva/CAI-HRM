using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRPortal.Common
{
    public class Context:IContext
    {
        public Context(HttpContextBase httpContextBase)
        {
            HttpContextBase = httpContextBase;
        }

        public HttpContextBase HttpContextBase
        {
            get;
            private set;
        }

        public HttpContext HttpContext
        {
            get {
                if (null == HttpContextBase || null == HttpContextBase.ApplicationInstance)
                    return null;

                return HttpContextBase.ApplicationInstance.Context;
            }
        }

    }
}