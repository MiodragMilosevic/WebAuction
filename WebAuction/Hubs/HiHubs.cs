using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace WebAuction.Controllers
{
    [HubName("HiHub")]
    public class HiHub : Hub
    {
        [HubMethodName("Hello")]
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}