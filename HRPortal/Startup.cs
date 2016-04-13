using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HRPortal.Startup))]
namespace HRPortal
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
