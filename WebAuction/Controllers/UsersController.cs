using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebAuction.Models;

namespace WebAuction.Controllers
{

    public class UsersController : Controller
    {
        private ModelBaze db = new ModelBaze();

        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        public ActionResult Register()
        {
            if (Session["User"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "Id,FirstName,LastName,Email,Address,Password,TokensNumber")] User user)
        {
            if (ModelState.IsValid)
            {
                var exists = db.Users.Any(x => x.Email == user.Email);
                if (!exists)
                {
                    user.TokensNumber = 0;
                    user.Password = MD5Hash(user.Password);
                    db.Users.Add(user);
                    db.SaveChanges();
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Message = "User with this Email exists!";
                    return View("Register");
                }
            }
            return View(user);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login([Bind(Include = "Id,FirstName,LastName,Email,Address,Password,TokensNumber")] User user)
        {
            user.Password = MD5Hash(user.Password);
            var exists = db.Users.Any(x => x.Email == user.Email && x.Password == user.Password);
            if (exists)
            {
                var users = db.Users.First(x => x.Email == user.Email && x.Password == user.Password);
                Session["User"] = users; 
                if (user.Email == "miodragmilosevic96@gmail.com")
                {
                    Session["Admin"] = true;
                }
                return RedirectToAction("Index", "Auctions");
            }
            else
            {
                ViewBag.Message = "User with this Email and Password doesn't exists!";
                return View("Login");
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Auctions");
        }

        public ActionResult Manage()
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = ((User)Session["User"]).Id;

            User user = db.Users.Find(id);
            if (user == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null || Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,FirstName,LastName,Email,Address,Password,TokensNumber")] User user)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                 return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if(ModelState.IsValid && !db.Users.Any(x => x.Email == user.Email && x.Id != user.Id))
            {
                db.Entry(user).State = EntityState.Modified;
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

                var auction = from a in db.Auctions
                              where a.User == user.Id
                              select a;
                foreach (var auc in auction)
                {
                    auc.FullName = user.FirstName + " " + user.LastName;
                }
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                }
                Session["User"] = user;
                return RedirectToAction("Index", "Auctions");
            }
            ViewBag.Message = "User with this mail exists!";
            return View(user);

        }

        public ActionResult BuyTokens()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuysTokens([Bind(Include = "PackageType,PhoneNumber,UserID")] BuyTokens tokens)
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var findAllParameters = from sp in db.SystemParameters
                                    select sp;

            var last = findAllParameters.ToList().Last();

            var tokenOrder = new TokenOder();
            tokenOrder.Buyer = ((User)Session["User"]).Id;
            tokenOrder.Currency = last.Currency;
            switch (tokens.PackageType)
            {
                case "SILVER":
                    {
                        tokenOrder.TokensAmount = last.SilverPackage;
                        break;
                    }
                case "GOLD":
                    {
                        tokenOrder.TokensAmount = last.GoldPackage;
                        break;
                    }
                case "PLATINUM":
                    {
                        tokenOrder.TokensAmount = last.PlatinumPackage;
                        break;
                    }
            }
            tokenOrder.Price = last.TokensPrice * tokenOrder.TokensAmount;
            tokenOrder.Status = "SUBMITTED";
            db.TokenOders.Add(tokenOrder);
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
            var baseURL = "http://stage.centili.com/payment/widget?apikey=5b0007f1fb8f6516d69e1e93cd99cc63&country=rs";
            int p;
            if (tokens.PackageType == "SILVER") p = 1;
            else if (tokens.PackageType == "GOLD") p = 2;
            else p = 3;
            var ret = "&clientId=" + ((User)Session["User"]).Id + "&phone=" +tokens.PhoneNumber + "&package=" + p + "&packagelock=true";
            return Redirect(baseURL + ret);
        }

        public ActionResult AllTokenOrders()
        {
            if (Session["User"] == null || Session["Admin"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var userId = ((User)Session["User"]).Id;
            var orders = db.TokenOders.Where(o => o.Buyer == userId).ToList();

            return View(orders);
        }

        public ActionResult ChangeParameters()
        {
            if (Session["User"] != null && Session["Admin"] != null)
            {
                var findAllParameters = from sp in db.SystemParameters
                                        select sp;

                var last = findAllParameters.ToList().Last();
                return View(last);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        public ActionResult EditParameters(int? id)
        {
            if (Session["User"] != null && Session["Admin"] != null && id != null)
            {
                SystemParameter param = db.SystemParameters.Find(id);
                if (param == null)
                {
                    return HttpNotFound();
                }
                return View(param);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditParameters([Bind(Include = "Id,RecentAuction,DefaultAuctionTime,SilverPackage,GoldPackage,PlatinumPackage,Currency,TokensPrice")]  SystemParameter parameter)
        {
            if (Session["User"] == null || Session["Admin"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (ModelState.IsValid)
            {
                if (parameter.RecentAuction <= 0 || parameter.DefaultAuctionTime <= 0 || parameter.SilverPackage <= 0
                    || parameter.SilverPackage >= parameter.GoldPackage || parameter.GoldPackage >= parameter.PlatinumPackage
                    || parameter.TokensPrice <= 0)
                {
                    ViewBag.Message = "Some parameters is not good!";
                    return View(parameter);
                }
                db.Entry(parameter).State = EntityState.Modified;
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

                return RedirectToAction("Index", "Auctions");
            }
            return View(parameter);
        }

    }
}
