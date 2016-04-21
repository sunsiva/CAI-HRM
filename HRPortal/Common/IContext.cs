using System.Web;

namespace HRPortal.Common
{
    public interface IContext
    {
        HttpContextBase HttpContextBase { get; }
        HttpContext HttpContext { get; }
    }
}
