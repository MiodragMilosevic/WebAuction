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

        [HubMethodName("Send")]
        public void Send(string name, string auctionID)
        {
            if (String.IsNullOrEmpty(name)) return;
            int aID = Convert.ToInt32(auctionID);

            var user = db.Users.Where(m => m.Email == name).First();
            var auctionToBid = db.Auctions.Where(m => m.Id == aID).First();
            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();

            if (DateTime.Now > auctionToBid.CompletedOn
                || auctionToBid.State != "OPENED")
                return;

            if (!String.IsNullOrEmpty(auctionToBid.FullName)
                && user.Id.Equals(auctionToBid.User))
            {
                if (user.TokensNumber + auctionToBid.CurrentPrice / last.TokensPrice < auctionToBid.CurrentPrice / last.TokensPrice + 1)
                    return;
            }
            else if (user.TokensNumber < auctionToBid.CurrentPrice + 1)
                return;

            Bid bid = new Bid();
            bid.Auction = aID;
            bid.Auction1 = auctionToBid;
            bid.User = user;
            bid.Bidder = user.Id;
            bid.BidOn = DateTime.Now;
            bid.Amount = auctionToBid.CurrentPrice;
            bid.Currency = last.Currency;

            if (auctionToBid.User != null)
            {
                giveBackMoneyToUser(auctionToBid.User, auctionToBid.CurrentPrice/last.TokensPrice);
            }
            auctionToBid.CurrentPrice += last.TokensPrice;

            user.TokensNumber -= (decimal)auctionToBid.CurrentPrice / last.TokensPrice;
            auctionToBid.User = user.Id;
            auctionToBid.FullName = user.FirstName + " " + user.LastName;

            auctionToBid.Bids.Add(bid);

            saveContextManual();
          
            Clients.All.addNewMessageToPage(user.FirstName + " " + user.LastName, auctionID, bid.Id,
                bid.User.FirstName, bid.User.LastName, bid.Auction, bid.BidOn, bid.Amount, bid.Currency); 
        }

        private void giveBackMoneyToUser(int? Id, decimal? amount)
        {
            if (Id == null || amount == null) return;
            var user = db.Users.Where(u => u.Id == Id).FirstOrDefault();
            user.TokensNumber += (decimal)amount;

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