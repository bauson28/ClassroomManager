using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HomeReadingManager.Startup))]
namespace HomeReadingManager
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
