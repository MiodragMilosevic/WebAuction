using Owin;
using Microsoft.Owin;
using System;
using Hangfire;


[assembly:OwinStartup(typeof(WebAuction.Startup))]
namespace WebAuction
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            GlobalConfiguration.Configuration.UseSqlServerStorage("Server=tcp:miki123.database.windows.net,1433;Initial Catalog=baza;Persist Security Info=False;User ID=miki123;Password=Gospodin13!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=True;");
        }
    }
}
