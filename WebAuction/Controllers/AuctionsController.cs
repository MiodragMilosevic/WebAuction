using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebAuction.Models;
using System.Data.Entity.Infrastructure;
using Hangfire;
using Hangfire.SqlServer;
using System.Threading;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Net.Mail;
using System.Text;
using System.Web.Hosting;
using System.Threading.Tasks;

namespace WebAuction.Controllers
{

    public static class PredicateBuilder
    {
        /// <summary>    
        /// Creates a predicate that evaluates to true.    
        /// </summary>    
        public static Expression<Func<T, bool>> True<T>() { return param => true; }

        /// <summary>    
        /// Creates a predicate that evaluates to false.    
        /// </summary>    
        public static Expression<Func<T, bool>> False<T>() { return param => false; }

        /// <summary>    
        /// Creates a predicate expression from the specified lambda expression.    
        /// </summary>    
        public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate) { return predicate; }

        /// <summary>    
        /// Combines the first predicate with the second using the logical "and".    
        /// </summary>    
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>    
        /// Combines the first predicate with the second using the logical "or".    
        /// </summary>    
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        /// <summary>    
        /// Negates the predicate.    
        /// </summary>    
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
        {
            var negated = Expression.Not(expression.Body);
            return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
        }

        /// <summary>    
        /// Combines the first expression with the second using the specified merge function.    
        /// </summary>    
        static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // zip parameters (map from parameters of second to parameters of first)    
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with the parameters in the first    
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // create a merged lambda expression with parameters from the first expression    
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        class ParameterRebinder : ExpressionVisitor
        {
            readonly Dictionary<ParameterExpression, ParameterExpression> map;

            ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if (map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }
    }

    public class AuctionsController : Controller
    {

        const double USDEUR = 1.20;
        const double RSDUSD = 100;
        const double RSDEUR = 120;

        private static object sync = new object();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static int Guid2Int(Guid value)
        {
            byte[] b = value.ToByteArray();
            int bint = BitConverter.ToInt32(b, 0);
            return bint;
        }

        private ModelBaze db = new ModelBaze();

        // GET: Auctions
        public ActionResult Index(string productName, decimal? minPrice, decimal? maxPrice, string state)
        {
            tryCompleted();
            SearchAuction searchAuction = new SearchAuction();
            searchAuction.ProductName = productName;
            searchAuction.MinPrice = minPrice;
            searchAuction.MaxPrice = maxPrice;
            searchAuction.State = state;

            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();

            var auction = from a in db.Auctions
                          select a;
            auction = auction.OrderByDescending(x => x.OpenedOn);
            if (!String.IsNullOrEmpty(productName))
            {
                string[] words = productName.Split(null); 
                var predicate = PredicateBuilder.False<Auction>();

                foreach (var word in words)
                {
                    string temp = word;
                    predicate = predicate.Or(a => a.Name.ToLower().Contains(temp.ToLower()));
                }
                auction = auction.Where(predicate);
            }

            if (minPrice != null)
            {
                auction = auction.Where(s => s.CurrentPrice != null && s.StartPrice >= minPrice);
            }

            if (maxPrice != null)
            {
                auction = auction.Where(s => s.CurrentPrice != null && s.StartPrice <= maxPrice);
            }

            if (!String.IsNullOrEmpty(state))
            {
                auction = auction.Where(s => s.State == state);
            }

            var AuctionsAndParameters = new SearchAuctionContainer();
            AuctionsAndParameters.searchAuction = searchAuction;
            AuctionsAndParameters.auctionList = auction.ToList().Take(last.RecentAuction);
            ViewBag.last = last;
            return View(AuctionsAndParameters);
        }

        // GET: Auctions/Details/5
        public ActionResult Details(int? id)
        {
            tryCompleted();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            var bids = from bid in db.Bids
                       select bid;

            bids = bids.Where(s => s.Auction == id);
            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();
            ViewBag.last = last;
            ViewBag.Bids = bids.OrderByDescending(x => x.BidOn);
            return View(auction);
        }

        // GET: Auctions/Create
        public ActionResult Create()
        {
            if (Session["User"] != null && Session["Admin"] == null)
            {
                tryCompleted();
                return View();
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        // POST: Auctions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,AuctionTime,CreatedOn,OpenedOn,CompletedOn,StartPrice,CurrentPrice,Currency,State,Img,Who")] Auction auction)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tryCompleted();
            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();

            auction.CreatedOn = DateTime.Now;
            auction.State = "READY";
            auction.Currency = last.Currency;
            auction.CurrentPrice = auction.StartPrice;
            auction.Who = ((User)Session["User"]).Id;
            if (auction.AuctionTime <= 0) auction.AuctionTime = last.DefaultAuctionTime;
            db.Auctions.Add(auction);
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
            return RedirectToAction("Index");
        }

        public ActionResult BuyTokens()
        {
            tryCompleted();
            return View();
        }

        public ActionResult CentiliReply(int clientid, string status)
        {
            lock (sync)
            {
                var findAllParameters = from sp in db.SystemParameters
                                        select sp;
                tryCompleted();
                var last = findAllParameters.ToList().Last();
                var order = db.TokenOders.Where(i => i.Status.Equals("SUBMITTED")).OrderByDescending(m => m.Id).FirstOrDefault();
                switch (status)
                {
                    case "success":
                        order.Status = "COMPLETED";
                        break;
                    case "canceled":
                        order.Status = "CANCELED";
                        break;
                    default:
                        order.Status = "CANCELED";
                        break;
                }
                var user = db.Users.Find(clientid);//Guid2Int(clientid)); 
                if (status.Equals("success"))
                {
                    user.TokensNumber += order.TokensAmount;          
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        try { db.SaveChanges(); }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            saveFailed = true;
                            var entry = ex.Entries.Single();
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                    } while (saveFailed);
                    using (SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", 587))
                    {
                        var basicCredential = new NetworkCredential("azure_f218ba7c42403230b08c85a599d307d3@azure.com", "Gospodin13!");
                        using (MailMessage message = new MailMessage())
                        {
                            MailAddress fromAddress = new MailAddress("miodragmilosevic96@gmail.com");

                            //smtpClient.Host = "mail.mydomain.com";
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.Credentials = basicCredential;

                            message.From = fromAddress;
                            message.Subject = "Kupovina tokena";
                            // Set IsBodyHtml to true means you can send HTML email.
                            message.IsBodyHtml = true;
                            message.Body = "<h1>Uspesno ste kupili tokene</h1> <p>Kupili ste tacno " + order.TokensAmount + " tokena po ceni od " + order.Price + " " + last.Currency +"</p>";
                            message.To.Add(user.Email);

                            try
                            {
                                smtpClient.Send(message);
                            }
                            catch (Exception ex)
                            {
                                //Error, could not send the message
                                Response.Write(ex.Message);
                            }
                        }
                    }
                }
            }
            
            return RedirectToAction("AllTokenOrders", "Users");
        }

        public ActionResult OpenAuction(int? id)
        {
            if (Session["User"] == null || Session["Admin"] == null || id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tryCompleted();
            logger.Info("Auction open " + id);
            var auction = db.Auctions.Find(id);

            if (auction.State != "READY")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            auction.OpenedOn = DateTime.Now;
            auction.State = "OPENED";
            bool saveFailed;
            do
            {
                saveFailed = false;
                try { db.SaveChanges(); }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    var entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                }
            } while (saveFailed);
            //BackgroundJob.Schedule(() => closeAuction(auction.Id), new TimeSpan(0, 0, auction.AuctionTime));
            
            return RedirectToAction("Index");
        }

        public void tryCompleted()
        {
            var auctions = from a in db.Auctions
                           select a;
            auctions = auctions.Where(x => x.State.Equals("OPENED"));

            LinkedList<int> list = new LinkedList<int>();
            foreach (var a in auctions)
            {
                DateTime opened = (DateTime)a.OpenedOn;
                opened = opened.AddSeconds(a.AuctionTime);
              //  opened = opened.AddHours(2);
                if (opened <= DateTime.Now) list.AddLast(a.Id);
            }

            foreach(var el in list)
            {
                closeAuction(el);
            }
            bool saveFailed;
            do
            {
                saveFailed = false;
                try { db.SaveChanges(); }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    var entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                }
            } while (saveFailed);
        }

        public void closeAuction(int id)
        {
            var auction = db.Auctions.Find(id);
            auction.CompletedOn = DateTime.Now;
            auction.State = "COMPLETED";

            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();
            var idB = auction.Who;
            var prodavac = db.Users.Find(idB);
            var query = from a in db.Bids
                        where a.Bidder == auction.User
                        select a;
            var bids = query.Max(x => x.Amount);
            auction.CurrentPrice = (decimal)bids;
            var value = (double)auction.CurrentPrice / (double)last.TokensPrice;
            switch (last.Currency)
            {
                case "USD":
                    {
                        if (auction.Currency == "RSD") { value /= RSDUSD; }
                        else if (auction.Currency == "EUR") { value *= USDEUR; }
                        break;
                    }
                case "RSD":
                    {
                        if (auction.Currency == "USD") { value *= RSDUSD; }
                        else if (auction.Currency == "EUR") { value *= RSDEUR; }
                        break;
                    }
                case "EUR":
                    {
                        if (auction.Currency == "RSD") { value /= RSDUSD; }
                        else if (auction.Currency == "USD") { value /= USDEUR; }
                        break;
                    }
            }
            prodavac.TokensNumber += value;
        }

        public ActionResult AuctionsWon(string productName, decimal? minPrice, decimal? maxPrice, string state)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tryCompleted();
            SearchAuction searchAuction = new SearchAuction();
            searchAuction.ProductName = productName;
            searchAuction.MinPrice = minPrice;
            searchAuction.MaxPrice = maxPrice;
            searchAuction.State = state;

            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();

            var auction = from a in db.Auctions
                          select a;

            var id = ((User)Session["User"]).Id;
            auction = auction.Where(predicate: x => x.User == id && x.State == "COMPLETED");

            if (!String.IsNullOrEmpty(productName))
            {
                string[] words = productName.Split(null);
                var predicate = PredicateBuilder.False<Auction>();

                foreach (var word in words)
                {
                    string temp = word;
                    predicate = predicate.Or(a => a.Name.ToLower().Contains(temp.ToLower()));
                }
                auction = auction.Where(predicate);
            }

            if (minPrice != null)
            {
                auction = auction.Where(s => s.CurrentPrice != null && s.StartPrice >= minPrice);
            }

            if (maxPrice != null)
            {
                auction = auction.Where(s => s.CurrentPrice != null && s.StartPrice <= maxPrice);
            }

            if (!String.IsNullOrEmpty(state))
            {
                auction = auction.Where(s => s.State == state);
            }

            var AuctionsAndParameters = new SearchAuctionContainer();
            AuctionsAndParameters.searchAuction = searchAuction;
            AuctionsAndParameters.auctionList = auction.ToList().Take(last.RecentAuction);
            ViewBag.last = last;
            return View(AuctionsAndParameters);
        }

    }
}
