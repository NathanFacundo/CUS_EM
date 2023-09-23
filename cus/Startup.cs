using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CUS.Startup))]
namespace CUS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
