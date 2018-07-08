using Owin;
using Microsoft.Owin;

[assembly:OwinStartup(typeof(WebAuction.Startup))]
namespace WebAuction
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
