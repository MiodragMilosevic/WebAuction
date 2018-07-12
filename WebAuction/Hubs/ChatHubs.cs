using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using WebAuction.Models;
using Hangfire;

namespace WebAuction.Controllers
{
    [HubName("ChatHub")]
    public class ChatHub : Hub
    {
        private ModelBaze db = new ModelBaze();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static object sync = new object();

        const double USDEUR = 1.20;
        const double RSDUSD = 100;
        const double RSDEUR = 120;

        [HubMethodName("Send")]
        public void Send(string name, string auctionID, string val)
        {
            lock (sync)
            {
                if (String.IsNullOrEmpty(name)) return;
                int aID = Convert.ToInt32(auctionID);
                var value = Double.Parse(val);
                var user = db.Users.Where(m => m.Email == name).First();
                var auctionToBid = db.Auctions.Where(m => m.Id == aID).First();
                var findAllParameters = from sp in db.SystemParameters
                                        select sp;

                var last = findAllParameters.ToList().Last();

                var max = (double) auctionToBid.CurrentPrice / (double)last.TokensPrice;
                switch (last.Currency)
                {
                    case "USD":
                        {
                            if (auctionToBid.Currency == "RSD") { max /= RSDUSD; }
                            else if (auctionToBid.Currency == "EUR") { max *= USDEUR; }
                            break;
                        }
                    case "RSD":
                        {
                            if (auctionToBid.Currency == "USD") { max *= RSDUSD; }
                            else if (auctionToBid.Currency == "EUR") { max *= RSDEUR; }
                            break;
                        }
                    case "EUR":
                        {
                            if (auctionToBid.Currency == "RSD") { max /= RSDUSD; }
                            else if (auctionToBid.Currency == "USD") { max /= USDEUR; }
                            break;
                        }
                }
                if (value <= max) return;
                if (DateTime.Now > auctionToBid.CompletedOn
                    || auctionToBid.State != "OPENED")
                    return;

                if (!String.IsNullOrEmpty(auctionToBid.FullName)
                    && user.Id.Equals(auctionToBid.User))
                {
                    if (user.TokensNumber + (double)auctionToBid.CurrentPrice / (double)last.TokensPrice < value)
                        return;
                }
                else if (user.TokensNumber < value)
                    return;

                Bid bid = new Bid();
                bid.Auction = aID;
                bid.Auction1 = auctionToBid;
                bid.User = user;
                bid.Bidder = user.Id;
                bid.BidOn = DateTime.Now;
                bid.Amount = (double)auctionToBid.CurrentPrice;
                bid.Currency = last.Currency;

                if (auctionToBid.User != null)
                {
                    giveBackMoneyToUser(auctionToBid.User,(double)auctionToBid.CurrentPrice / (double)last.TokensPrice);
                }
                auctionToBid.CurrentPrice = last.TokensPrice * (decimal)value;

                user.TokensNumber -= value;
                auctionToBid.User = user.Id;
                auctionToBid.FullName = user.FirstName + " " + user.LastName;

                auctionToBid.Bids.Add(bid);

                saveContextManual();
                logger.Info("Client BID. FullName:" + user.FirstName + " " + user.LastName + " auctionID: " + auctionID);
                Clients.All.addNewMessageToPage(user.FirstName + " " + user.LastName, auctionID, bid.Id,
                    bid.User.FirstName, bid.User.LastName, bid.Auction, bid.BidOn, bid.Amount, bid.Currency, value);
            }
        }

        private void giveBackMoneyToUser(int? Id, double? amount)
        {
            if (Id == null || amount == null) return;
            var user = db.Users.Where(u => u.Id == Id).FirstOrDefault();
            user.TokensNumber += amount;

            saveContextManual();
        }

        private void saveContextManual()
        {
            bool saveFailed;
            do
            {
                saveFailed = false;
                try { db.SaveChanges(); }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    // Update original values from the database 
                    var entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                }
            } while (saveFailed);
        }

    }
}