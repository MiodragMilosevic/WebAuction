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
        private ModelBaze db = new ModelBaze();

        // GET: Auctions
        public ActionResult Index(string productName, decimal? minPrice, decimal? maxPrice, string state)
        {
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
        public ActionResult Create([Bind(Include = "Id,Name,AuctionTime,CreatedOn,OpenedOn,CompletedOn,StartPrice,CurrentPrice,Currency,State,Img")] Auction auction)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
              
            if (ModelState.IsValid)
            {
                auction.CreatedOn = DateTime.Now;
                auction.State = "READY";
                auction.CurrentPrice = auction.StartPrice;
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

            return View(auction);
        }

        public ActionResult BuyTokens()
        {
            return View();
        }

        public ActionResult GetCentiliDetails(string id, int? amount, string status)
        {
            var order = db.TokenOders.Where(i => i.Status.Equals("SUBMITTED")).OrderByDescending(m => m.Id).FirstOrDefault();
            switch (status)
            {
                case "success":
                    order.Status = "COMPLETED"; 
                    break;
                case "canceled":
                    order.Status = "CANCELED";
                    break;
            }
            var user = db.Users.Find(Int32.Parse(id));
            user.TokensNumber = order.TokensAmount;
            return RedirectToAction("AllTokenOrders", "Users");
        }

        public ActionResult OpenAuction(int? id)
        {
            if (Session["User"] == null || Session["Admin"] == null || id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
            Thread t = new Thread(() =>
            {
                Thread.Sleep(auction.AuctionTime * 1000);
                auction.CompletedOn = DateTime.Now;
                auction.State = "COMPLETED";
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
            });
            t.IsBackground = true;
            t.Start();
            //BackgroundJob.Schedule(() => closeAuction(auction), TimeSpan.FromSeconds(auction.AuctionTime));
            return RedirectToAction("Index");
        }

        public ActionResult AuctionsWon(string productName, decimal? minPrice, decimal? maxPrice, string state)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
