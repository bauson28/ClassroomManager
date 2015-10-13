using HomeReadingManager.Models;
using HomeReadingManager.MyClasses;
using HomeReadingManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf;
using System.Net;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

namespace HomeReadingManager.Controllers
{
    public class StocktakeController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;

        public StocktakeController()
        {
        }

        public StocktakeController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        
        private IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue)
        {
            var items = new List<SelectListItem>();
            items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
            items.AddRange(list);
            return items;
        }

        private string GetBarcode(string isbn)
        {
            string cShort;
            int temp = 0, total = 0;
            if (isbn.Trim().Length == 10)
                cShort = "978" + isbn.Substring(0, 9);
            else
                cShort = isbn.Substring(0, 12);

            for (int i = 2; i < 13; i += 2)
            {
                total += Convert.ToInt32(cShort.Substring(i - 1, 1)) * 3;
                total += Convert.ToInt32(cShort.Substring(i - 2, 1));
            }

            temp = 10 - (total % 10);
            if (temp == 10)
                temp = 0;

            return (cShort + temp.ToString());
        }

        private string CheckCheckDigit(string isbn)
        {
            if (isbn.Length == 10)
            {
                isbn = isbn.ToUpper();//bf catch isbns ending in X
                if (isbn == Isbncheck(isbn))
                    isbn = GetBarcode(isbn);
                else
                    isbn = string.Empty;
            }
            else if (isbn.Length == 13)// && (isbn.StartsWith("978") || isbn.StartsWith("979"))) //bf 201113 just need valid EAN
            {
                string calculated = string.Empty;
                calculated = GetBarcode(isbn);
                if (isbn != calculated)
                    isbn = string.Empty;
            }
            else
                isbn = string.Empty;
            return isbn;
        }

        private string Isbncheck(string isbn)
        {
            int nTotal = 0;
            int nMod = 0;
            string check = string.Empty;

            if (isbn.StartsWith("978"))
                isbn = isbn.Substring(4, 10);
            else if (isbn.Trim().Length != 10)
                return isbn;

            isbn = isbn.Substring(0, 9);
            for (int i = 0; i < 9; i++)
            {
                nTotal += Convert.ToInt32(isbn.Substring(i, 1)) * (11 - (i + 1));
            }
            nMod = nTotal % 11;

            switch (nMod)
            {
                case 10:
                    check = "1";
                    break;
                case 0:
                    check = "0";
                    break;
                case 1:
                    check = "X";
                    break;
                default:
                    check = (11 - nMod).ToString();
                    break;
            }
            return isbn + check;
        }

        private int IsbnSearch(string search)
        {
            //HomeReadingEntities dbContext = new HomeReadingEntities();

            var query = (from p in db.Products
                         where p.Isbn == search
                         select p).FirstOrDefault();

            if (query != null)
            {
                //lbProduct.Text = query.Title;
                return query.Product_Id;
            }
            else
            {
                //lbProduct.Text = "Please select a title";
                //ShowProductForm(0);
                return 0;
            }
        }

        private int GoogleBooksSearch(string isbn, int existingId, int libraryId)
        {
            GoogleBooks gb = new GoogleBooks();
            gb.isbn = isbn;
            //gb.errorMessage 
            gb.userId = 0;
            gb.libraryId = libraryId;
            gb.levelsId = 0;
            gb.doLabels = false;
            gb.existingId = existingId;
            gb.addStock = false;
            gb.physicalFolder = Server.MapPath("~/");
            int productId = gb.GoogleSearch();
            //if (productId > 0)
            //{
            //    success = true;
            //    ShowProductForm(productId);
            //    tbIsbnTitle.Text = string.Empty;
            //}
            //tbIsbnTitle.Focus();
            return productId;
        }

        private bool AddToCount(string isbn, int thisId)
        {
            int productId = 0;

            if (thisId > 0)
            {
                productId = thisId;
            }
            else
            {
                var queryp = (from p in db.Products
                              where p.Isbn == isbn
                              select p).SingleOrDefault();


                if (queryp != null)
                {
                    productId = queryp.Product_Id;
                }
            }

            if (productId == 0)
            {
                // CusValRight.ErrorMessage = "Invalid barcode.";
                //CusValRight.IsValid = false;
            }
            else
            {
                int sessionId = Convert.ToInt32(Session["sessionId"]);
                int lastId = Convert.ToInt32(Session["lastId"]);
                int lastMapping = Convert.ToInt32(Session["lastmappingId"]);

                if (lastId == productId && lastMapping > 0)
                {
                    int mappingId = Convert.ToInt32(Session["lastmappingId"]);
                    var query = (from c in db.STCounts
                                 where c.STCount_Id == mappingId
                                 select c).SingleOrDefault();

                    var query2 = (from c in db.ProdStocks
                                  where c.Product_Id == query.Product_Id
                                  select c).FirstOrDefault();
                    try
                    {
                        query2.StockCount += 1;
                        query.Qty += 1;
                        query.CountDate = DateTime.Now;
                        db.SaveChanges();
                        System.Media.SystemSounds.Exclamation.Play(); 
                        return true;
                    }
                    catch (Exception)
                    {
                        //CusValRight.ErrorMessage = "Failed to write to file.";
                        // CusValRight.IsValid = false;
                    }
                    finally
                    {
                        //tbBarcode.Text = "";
                        //tbBarcode.Focus();
                    }
                }
                else
                {
                    STCount c = new STCount();
                    c.Product_Id = productId;
                    c.StSessions_Id = sessionId;
                    c.Barcode = isbn;
                    c.Qty = 1;
                    c.CountDate = DateTime.Now;
                    db.STCounts.Add(c);

                    var query3 = (from ps in db.ProdStocks
                                  where ps.Product_Id == productId
                                  select ps).FirstOrDefault();
                    try
                    {
                        query3.StockCount += 1;
                        db.SaveChanges();
                        Session["lastId"] = productId.ToString();
                        Session["lastmappingId"] = c.STCount_Id.ToString();
                        System.Media.SystemSounds.Exclamation.Play(); 
                        return true;
                    }

                    catch (Exception)
                    {
                        //CusValRight.ErrorMessage = "Failed to write to file.";
                        //CusValRight.IsValid = false;
                    }
                    finally
                    {
                        //tbBarcode.Text = "";
                        //tbBarcode.Focus();
                    }
                }

            }
            
            return false;
        }

        private string GetSessionName(int id)
        {
            return "";
        }

        private IQueryable<CountList> GetCountList(int sessionId, int libraryId)
        {

            IQueryable<CountList> countList = from c in db.STCounts
                                              join ps in db.ProdStocks.Where(x=>x.Library_Id == libraryId) on c.Product.Product_Id equals ps.Product_Id into pqc
                                              from z in pqc.DefaultIfEmpty()
                                              where c.StSession.Library_Id == libraryId && c.StSessions_Id == sessionId 
                                              orderby c.CountDate descending
                                              select new CountList()
                                              {
                                                  STCount_Id = c.STCount_Id,
                                                  CountDate = c.CountDate,
                                                  Product_Id = c.Product_Id,
                                                  Isbn = c.Product.Isbn,
                                                  Title = c.Product.Title,
                                                  ReadLevel = c.Product.Level.ReadLevel,
                                                  Count = c.Qty,
                                                  Total = (z.StockCount == null) ? 0 : (int)z.StockCount
                                              };

            return countList;
        }

        // GET: Stocktake
        public ActionResult Index(string message, string userAction, string searchString, string sessionName, string newSession, string sortOrder, string lastOrder, 
                                int? userId, int? libraryId, int? productId, int? sessionId, int? reportType,
                                    bool counting = false, int page = 1, bool searchReturn = false)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Stocktake/Index" });
                libraryId = user.LibraryId;
                userId = user.TeacherId;
            }

            var query = (from l in db.Libraries
                         where l.Library_Id == libraryId
                         select l).FirstOrDefault();

            if (query == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            else
            {
                Stocktake model = new Stocktake();
                model.LibraryId = (int)libraryId;
                model.UserId = (int)userId;
                model.Flag = (query.StocktakeFlag == null) ? false : (bool)query.StocktakeFlag;
                //model.StocktakeDate = query.StocktakeDate;
               // model.LastSTDate = query.LastSTDate;
                model.ReportType = (reportType == null) ? 0 : (int)reportType;
                model.Page = page;
                model.Message = message;

                model.HasSessions = false;
                if (model.Flag)
                {
                    model.LastStocktake = "Stocktake commenced:";
                    model.ShowDate = ((DateTime)query.StocktakeDate).ToLongDateString();
                    model.HasPrevious = true;
                }
                else if (query.LastSTDate == null)
                    model.LastStocktake = "No previous stocktake";
                else 
                {
                    model.LastStocktake = "Last stocktake:";
                    model.ShowDate = ((DateTime)query.LastSTDate).ToLongDateString();
                    model.HasPrevious = true;
                }

                if (userAction == "P")
                {
                    Session["lastId"] = null;
                    Session["lastmappingId"] = null;
                    Session["sessionId"] = null;
                    Session["sessionName"] = null;
                    model.UserAction = "P";
                }
               
                else if (productId != null && productId > 0)
                {
                    if (!AddToCount("", (int)productId))
                    {
                        //baf error stuff here
                        System.Media.SystemSounds.Hand.Play(); 
                    }
                }
                else if (!String.IsNullOrEmpty(searchString) && !searchReturn)
                {
                    searchString = searchString.Trim();
                    string isbn = CheckCheckDigit(searchString);
                    if (!string.IsNullOrEmpty(isbn))
                    {
                        int id = IsbnSearch(isbn);
                        if (id > 0)
                        {
                            if (!AddToCount("", id))
                            {
                                //baf error stuff here
                                System.Media.SystemSounds.Hand.Play(); 
                            }
                        }
                        else
                        {
                            id = GoogleBooksSearch(isbn, 0, model.LibraryId);
                            if (id > 0)
                            {
                                if (!AddToCount("", id))
                                {
                                    //baf error stuff here
                                    System.Media.SystemSounds.Hand.Play(); 
                                }
                            }
                        }
                    }
                    else //its a title search
                    {
                        model.TitleSearch = searchString;
                        model.UserAction = "T";
                    }
                }

                else if (reportType != null && reportType > 0)
                {
                    model.ReportName = GetReportName((int)reportType);
                    ViewBag.SortOrder = sortOrder;
                    ViewBag.LastOrder = lastOrder;
                    ViewBag.Page = page;
                    sessionId = 0;
                    sessionName = "";
                    Session["sessionId"] = null;
                }

                else if (!String.IsNullOrEmpty(newSession))
                {
                    if (ValidateSession(newSession, model.LibraryId))
                    {
                        int id = CreateSession(newSession, model.LibraryId);
                        if (id > 0)
                        {
                            sessionId = id;
                            sessionName = newSession;
                            Session["lastId"] = '0';
                            Session["lastmappingId"] = '0';
                        }
                    }
                    else
                    {
                        //duplicate names
                    }
                }
                if (sessionId != null && sessionId > 0)
                {
                    int id = Convert.ToInt32(Session["sessionId"]);
                    if (id != (int)sessionId)
                    {
                        Session["lastId"] = '0';
                        Session["lastmappingId"] = '0';
                    }
                    model.Session_Id = (int)sessionId;
                    model.SessionName = sessionName;
                    Session["sessionId"] = model.Session_Id.ToString();
                    Session["sessionName"] = model.SessionName;
                    //ViewBag.SessionName = "Counting session: " + model.SessionName;
                    if (String.IsNullOrEmpty(model.UserAction))
                        model.UserAction = "C";

                }

                else if (Session["sessionId"] != null)
                {
                    model.Session_Id = Convert.ToInt32(Session["sessionId"].ToString());
                    if (Session["sessionName"] != null)
                        model.SessionName = Session["sessionName"].ToString();
                    model.UserAction = "C";
                }
                else
                {
                    model.Session_Id = 0;
                    model.SessionName = "";
                    Session["sessionId"] = null;
                    Session["sessionName"] = null;
                }

                if (model.Flag)
                {
                    if (userAction == "N")
                        model.UserAction = "N";
                }

                var SessionQry = from c in db.StSessions
                                    where c.Library_Id == model.LibraryId
                                    orderby c.BatchName
                                    select new
                                    {
                                        c.StSessions_Id,
                                        c.BatchName
                                    };

                if (SessionQry != null)
                    model.HasSessions = SessionQry.Count() > 0;

                ViewBag.SessionId = new SelectList(SessionQry, "StSessions_Id", "BatchName");
                ViewBag.SessionId = AddDefaultOption(ViewBag.SessionId, "Select session", "0");
                //ViewBag.SessionName = (Session["sessionName"] == null) ? "" : "Counting session: " + Session["sessionName"].ToString();

               
                ViewBag.Page = page;
                return View(model);
            }
        }

        // POST: Stocktake/Edit/5, ActionName("StartStocktake")
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult StartStocktake(int? libraryId, int? userId)
        {
            //var user = UserManager.FindById(User.Identity.GetUserId());
            //if (user == null)
            //    return RedirectToAction("Login", "Account", new { returnUrl = "~/Stocktake/Index" });
            //int libraryId = user.LibraryId;
            string message = "";
            if (User.IsInRole("Administrator")) 
            {
                var query = (from l in db.Libraries
                             where l.Library_Id == libraryId
                             select l).FirstOrDefault();

                if (query != null)
                {
                    query.StocktakeFlag = true;
                    query.StocktakeDate = DateTime.Now;
                    try
                    {
                        db.SaveChanges();
                    }
                    catch
                    {
                        message = "Failed to write to file. Please try again.";
                    }
                }
            }
            else
            {
               // baf xxx handle this in javascript
                message = "You are not authorised to initiate stocktakes.";
            }
            return RedirectToAction("Index", new { libraryId = libraryId, userId = userId, message = message });
        }

        private bool ValidateSession(string batchName, int libraryId)
        {
            var queryv = (from ss in db.StSessions
                          where ss.BatchName.Equals(batchName, StringComparison.OrdinalIgnoreCase) && ss.Library_Id == libraryId
                          select ss).FirstOrDefault();

            return (queryv == null);
        }

        private int CreateSession(string batchName, int libraryId)
        {
            int id = 0;
            if (!String.IsNullOrEmpty(batchName))
            {
                StSession session = new StSession();
                session.BatchName = batchName;
                session.Library_Id = libraryId;
                session.BatchDate = DateTime.Now;
                db.StSessions.Add(session);
                db.SaveChanges();
                id = session.StSessions_Id;
            }
            return id;
        }

        // GET: Stocktake/Edit/5
        public ActionResult NewSession(Stocktake model)
        {
            return PartialView("_NewSession", model);
        }

        [HttpPost]
        public ActionResult Counting(string sessionName, int? libraryId, int? userId, int? sessionId, string searchString, bool searchReturn = false)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Stocktake/Index" });
                libraryId = user.LibraryId;
                userId = user.TeacherId;
            }

            if (!String.IsNullOrEmpty(searchString) && !searchReturn)
            {
                string isbn = CheckCheckDigit(searchString.Trim());

                if (!string.IsNullOrEmpty(isbn))
                {
                    int id = IsbnSearch(isbn);
                    if (id > 0)
                    {
                        if (!AddToCount("", id))
                        {
                            //baf error stuff here
                            System.Media.SystemSounds.Hand.Play(); 
                        }

                    }
                    else
                    {
                        id = GoogleBooksSearch(isbn, 0, (int)libraryId);
                        if (id > 0)
                        {
                            if (!AddToCount("", id))
                            {
                                //baf error stuff here
                                System.Media.SystemSounds.Hand.Play(); 
                            }
                        }
                    }
                }
            }

            return RedirectToAction("Index", new { sessionName = sessionName, sessionId = sessionId, libraryId = libraryId, userId = userId });
        }

        public ActionResult CountingList(int page, int libraryId, int userId)
        {
            int id = Convert.ToInt32(Session["sessionId"]);
            if (id > 0)
            {
                int pageNumber = page;
                int pageSize = 10;
                CountModel model = new CountModel();
                model.LibraryId = libraryId;
                model.UserId = userId;
                model.Page = pageNumber;
                IQueryable<CountList> countList = GetCountList(id, libraryId);
                model.CountLists = countList.ToPagedList(pageNumber, pageSize);
                
                //var countListPaged = countList.ToPagedList(pageNumber, pageSize);
                //ViewBag.CountList = countListPaged;
                return PartialView("_CountingList", model);
            }

            return PartialView("_SelectSession");
        }

        public ActionResult TitleSearch(string search, string sessionName, int sessionId, int? page)
        {
            if (String.IsNullOrEmpty(search))
            {
                return Content("");
            }
            IQueryable<ProductSearch> productList = from p in db.Products
                                                    where p.Title.Contains(search)// && (prod.Library_Id == _libraryId || prod.Authorised == true)
                                                    orderby p.Title
                                                    select new ProductSearch()
                                                    {
                                                        Product_Id = p.Product_Id,
                                                        Title = p.Title,
                                                        Isbn = p.Isbn,
                                                        MainAuthor = (p.ProdAuthors.Count() == 0) ? String.Empty : p.ProdAuthors.FirstOrDefault().Author,
                                                        ReadLevel = p.Level.ReadLevel,
                                                        Image_Id = (p.ProdImages.Count() == 0) ? 0 : p.ProdImages.FirstOrDefault().Image_Id,
                                                        Jacket = (p.ProdImages.Count() == 0) ? null : p.ProdImages.FirstOrDefault().Jacket,
                                                    };
            if (productList.Count() > 0)
            {
                int pageSize = 7;
                int pageNumber = (page ?? 1);
                ViewBag.Page = page;

                var productListPaged = productList.ToPagedList(pageNumber, pageSize);
                ViewBag.ProductList = productListPaged;
                ViewBag.SearchString = search;
                ViewBag.SessionName = sessionName;
                ViewBag.SessionId = sessionId;
                return PartialView("_TitleSearch");

            }
            else
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = barcode + " is not a valid ISBN.";
            }
            return Content("");
        }

        // GET: Stocktake/Delete/5
        public ActionResult Decrement(int id, int page, int libraryId, int userId)
        {
            var query = (from l in db.STCounts
                         where l.STCount_Id == id
                         select l);
            foreach (var stCount in query)
            {
                if (stCount.Qty > 1)
                    stCount.Qty -= 1;
                else
                    db.STCounts.Remove(stCount);
            }
            try
            {
                db.SaveChanges();
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch
            {
                System.Media.SystemSounds.Hand.Play();
            }
            return RedirectToAction("Index", new { page = page, libraryId = libraryId, userId = userId });
        }

        // POST: Stocktake/Delete/5
        [HttpPost]
        public ActionResult Decrement(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        private string GetReportName(int reportType)
        {
            switch (reportType)
            {
                default:
                    return "Excess Stock";

                case 2:
                    return "Missing/Uncounted Stock";

                case 3:
                    return "Stocktake List";

                case 4:
                    return "Stock List";
            }
        }

        public ActionResult FinaliseStocktake()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Stocktake/Index" });

            string message = "";
            if (User.IsInRole("Manager") || User.IsInRole("Administrator"))
            {
                int libraryId = user.LibraryId;
                var query = from s in db.ProdStocks
                            where s.Library_Id == libraryId
                            select s;

                foreach (var c in query)
                {
                    c.Onhand = (c.StockCount == null) ? 0 : c.StockCount;
                }

                try
                {
                    db.SaveChanges();
                    ScrapStocktake(false, libraryId);
                }
                catch (Exception)
                {
                    message = "Failed to write to file. Please try again.";
                }

            }
            else
            {
                message = "You are not authorised to initiate stocktakes.";
             }
            return RedirectToAction("Index", new { libraryId = user.LibraryId, userId = user.TeacherId, message = message });
        }

        public ActionResult ScrapStocktake()
        {
            string message = "";
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Stocktake/Index" });


            if (User.IsInRole("Manager") || User.IsInRole("Administrator"))
            {
                if (ScrapStocktake(true, user.LibraryId))
                    return RedirectToAction("Index", new { libraryId = user.LibraryId, userId = user.TeacherId });
                else
                    message = "Failed to write to file. Please try again.";
            }
            else
            {
                message = "You are not authorised to process stocktakes.";
             }

            return RedirectToAction("Index", new { libraryId = user.LibraryId, userId = user.TeacherId, message = message });
        }

        private bool ScrapStocktake(bool doStock, int libraryId)
        {
            if (doStock)
            {
                var query1 = from c in db.STCounts
                             where c.StSession.Library_Id == libraryId
                             select c;
                foreach (var c in query1)
                {
                    db.STCounts.Remove(c);
                }

                var query2 = from s in db.StSessions
                             where s.Library_Id == libraryId
                             select s;
                foreach (var s in query2)
                {
                    db.StSessions.Remove(s);
                }
            }
            var query3 = (from sv in db.Libraries
                          where sv.Library_Id == libraryId
                          select sv).SingleOrDefault();

            if (query3 != null)
            {
                string bf = query3.StocktakeDate.ToString();
                query3.StocktakeFlag = false;
                query3.StocktakeDate = null;
                if (!doStock)
                    query3.LastSTDate = DateTime.Now;

                if (doStock)
                {
                    var query4 = from ps in db.ProdStocks
                                 where ps.Library_Id == libraryId
                                 select ps;
                    foreach (var ps in query4)
                    {
                        ps.StockCount = 0;
                    }
                }

                try
                {
                    db.SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    //CusValButton.ErrorMessage = "Failed to write to file.";
                    //CusValButton.IsValid = false;
                }
            }
            return false;
        }

        public ActionResult ShowReport(int reportType, int libraryId, string lastOrder, string sortOrder, int? page)
        {
            bool ascending = reportType != 1;
            if (String.IsNullOrEmpty(sortOrder))
            {
                if (String.IsNullOrEmpty(lastOrder))
                    ViewBag.SortColumn = sortOrder = ((reportType == 4) ? "Title" : "Diff");

                else if (lastOrder.Substring(0, 1) == "%")
                {
                    ascending = !ascending; ;
                    ViewBag.SortColumn = lastOrder;
                    sortOrder = lastOrder.Substring(1, lastOrder.Length - 1);
                }
                else
                    ViewBag.SortColumn = sortOrder = lastOrder;
            }
            else
            {
                page = 1;
                if (sortOrder == lastOrder)
                {
                    ascending = !ascending; ;
                    ViewBag.SortColumn = "%" + sortOrder;
                }
               // else if (sortOrder == "%" + lastOrder)
                //    ViewBag.SortColumn = sortOrder;
                else
                    ViewBag.SortColumn = sortOrder;
            }

            IQueryable<StockReportList> reportList = from ps in db.ProdStocks
                                                     where ps.Library_Id == libraryId && (
                                                                 (reportType == 1) ? ps.StockCount - ps.Onhand > 0 : true
                                                             || (reportType == 2) ? ps.StockCount - ps.Onhand < 0 : true
                                                             || (reportType == 3) ? (ps.StockCount != 0 || ps.Onhand != 0) : true
                                                             || (reportType == 4) ? ps.Onhand > 0 : true)
                                                     select new StockReportList()
                                                     {
                                                         Title = ps.Product.Title,
                                                         Isbn = ps.Product.Isbn,
                                                         ReadLevel = ps.Product.Level.ReadLevel,
                                                         Stock = (ps.Onhand == null || ps.Onhand < 0) ? 0 : (int)ps.Onhand,
                                                         Count = (ps.StockCount == null) ? 0 : (int)ps.StockCount,
                                                         //Diff = ((ps.Onhand == null || ps.Onhand < 0) ? 0 : (int)ps.Onhand) - ((ps.StockCount == null) ? 0 : (int)ps.StockCount),
                                                         Diff = (reportType == 4) ? (int)ps.Onhand : (((ps.StockCount == null) ? 0 : (int)ps.StockCount) - ((ps.Onhand == null) ? 0 : (int)ps.Onhand))
                                                     };
            if (reportList != null)
            {
                switch (sortOrder)
                {
                    case "Title":
                        if (ascending)
                            reportList = reportList.OrderBy(s => s.Title);
                        else
                            reportList = reportList.OrderByDescending(s => s.Title);
                        break;

                    case "ReadLevel":
                        if (ascending)
                            reportList = reportList.OrderBy(s => s.ReadLevel);
                        else
                            reportList = reportList.OrderByDescending(s => s.ReadLevel);
                        break;
                    default:
                        if (ascending)
                            reportList = reportList.OrderBy(s => s.Diff);
                        else
                            reportList = reportList.OrderByDescending(s => s.Diff);
                        break;
                }

                int pageSize = 16;
                int pageNumber = (page ?? 1);
                ViewBag.Page = page;

                var reportListPaged = reportList.ToPagedList(pageNumber, pageSize);
                ViewBag.ReportList = reportListPaged;
                ViewBag.ReportType = reportType;
                StockReportList myModel = new StockReportList();
                return PartialView("_Report", myModel);

            }
            else
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = barcode + " is not a valid ISBN.";
            }
            return PartialView("_SelectReport");
        }

        public ActionResult PrintReport(int reportType, string reportTitle, string sortOrder, int libraryId)
        {
            string schoolName = "Mt Druitt Public School";
            // ProfileBase userProfile = ProfileBase.Create(User.Identity.Name, true);  ///baf XXXX
            // schoolName = userProfile.GetPropertyValue("LibraryName").ToString(); //baf XXXX
            bool ascending = (reportType != 1);
            if (sortOrder != null && sortOrder == "%")
            {
                ascending = !ascending;
                sortOrder = sortOrder.Substring(1, sortOrder.Length - 1);
            }

            float postScriptPointsPerMilimeter = 2.834645669f;
            Document doc = new Document(PageSize.A4);
            MemoryStream stream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(doc, stream);

            try
            {
                byte[] buffer = null;

                var query2 = (from j in db.LibImages
                              where j.Library_Id == libraryId
                              select new
                              {
                                  j.Image_Id,
                                  j.Image
                              }).FirstOrDefault();


                iTextSharp.text.Image crest = null;
                if (query2.Image_Id > 0)
                {
                    buffer = (byte[])query2.Image;
                    crest = iTextSharp.text.Image.GetInstance(buffer);
                    crest.ScaleToFit(200f, 240f);
                    crest.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_RIGHT;
                    crest.SetAbsolutePosition(200f, 400f);
                    writer.PageEvent = new PDFWriterEvents(crest);
                }

                doc.Open();

                DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
                string year = DateTime.Now.Year.ToString();

                IQueryable<StockReportList> reportList = from ps in db.ProdStocks
                                                         where ps.Library_Id == libraryId && (
                                                                     (reportType == 1) ? ps.StockCount - ps.Onhand > 0 : true
                                                                 || (reportType == 2) ? ps.StockCount - ps.Onhand < 0 : true
                                                                 || (reportType == 3) ? (ps.StockCount != 0 || ps.Onhand != 0) : true
                                                                 || (reportType == 4) ? ps.Onhand > 0 : true)
                                                         select new StockReportList()
                                                         {
                                                             Title = ps.Product.Title,
                                                             Isbn = ps.Product.Isbn,
                                                             ReadLevel = ps.Product.Level.ReadLevel,
                                                             Stock = (ps.Onhand == null) ? 0 : (int)ps.Onhand,
                                                             Count = (ps.StockCount == null) ? 0 : (int)ps.StockCount,
                                                             Diff = (reportType == 4) ? (int)ps.Onhand : (((ps.StockCount == null) ? 0 : (int)ps.StockCount) - ((ps.Onhand == null) ? 0 : (int)ps.Onhand))
                                                         };

                if (reportList != null)
                {
                    switch (sortOrder)
                    {
                        case "Title":
                            if (ascending)
                                reportList = reportList.OrderBy(s => s.Title);
                            else
                                reportList = reportList.OrderByDescending(s => s.Title);
                            break;

                        case "ReadLevel":
                            if (ascending)
                                reportList = reportList.OrderBy(s => s.ReadLevel);
                            else
                                reportList = reportList.OrderByDescending(s => s.ReadLevel);
                            break;
                        default:
                            if (ascending)
                                reportList = reportList.OrderBy(s => s.Diff);
                            else
                                reportList = reportList.OrderByDescending(s => s.Diff);
                            break;
                    }
                    Font times10 = FontFactory.GetFont("Times Roman");
                    times10.Size = 10;
                    //times10.SetStyle("Italic");
                    Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                    Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);

                    int cols = 6;
                    float[] widths = new float[] { 3.5f, 2.5f, 1.5f, 1.5f, 1.5f, 1.5f };
                    if (reportType == 4)
                    {
                        widths = new float[] { 5f, 3f, 2f, 2f };
                        cols = 4;
                    }

                    int count = 0;
                    int pageSize = 30;
                    int pageNo = 0;

                    float height1 = 7f;
                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable books = new PdfPTable(cols);
                    books.TotalWidth = 150f * postScriptPointsPerMilimeter;
                    books.SetWidths(widths);
                    books.LockedWidth = true;
                    books.HorizontalAlignment = 1;
                    books.SpacingAfter = 15f;
                    books.SpacingBefore = 15f;

                    foreach (var c in reportList)
                    {
                        if (count == pageSize || count == 0)
                        {
                            Paragraph para = new Paragraph();

                            if (pageNo > 0)
                            {
                                doc.Add(books);
                                books.DeleteBodyRows();
                                para.Add("Page " + pageNo.ToString());
                                doc.Add(para);
                                doc.NewPage();
                            }
                            pageNo += 1;
                            count = 0;

                            para.Clear();
                            para.SpacingAfter = 5f;
                            para.Add(schoolName + " Home Reading");
                            para.Font = times12;
                            para.Alignment = Element.ALIGN_CENTER;
                            doc.Add(para);

                            para.Clear();
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Add(reportTitle);
                            para.Font = times12;
                            doc.Add(para);

                            para.Clear();
                            para.SpacingAfter = 10f;
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Font = times10;
                            para.Add("Stocktake: " + DateTime.Now.ToShortDateString());
                            doc.Add(para);

                            PdfPCell cellH1 = new PdfPCell();
                            cellH1.Border = 0;
                            cellH1.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH1.Indent = 1;
                            cellH1.Phrase = new Phrase("Title", times11);
                            books.AddCell(cellH1);

                            PdfPCell cellH2 = new PdfPCell();
                            //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH2.Border = 0;
                            cellH2.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH2.Indent = 1;
                            cellH2.Phrase = new Phrase("Barcode", times11);
                            books.AddCell(cellH2);

                            PdfPCell cellH3 = new PdfPCell();
                            //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH3.Border = 0;
                            cellH3.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH3.Indent = 1;
                            cellH3.Phrase = new Phrase("Level", times11);
                            books.AddCell(cellH3);

                            PdfPCell cellH4 = new PdfPCell();
                            //cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH4.Border = 0;
                            cellH4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH4.Indent = 1;
                            cellH4.Phrase = new Phrase("Stock", times11);
                            books.AddCell(cellH4);

                            if (reportType != 4)
                            {
                                PdfPCell cellH5 = new PdfPCell();
                                //cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH5.Border = 0;
                                cellH5.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellH5.Indent = 1;
                                cellH5.Phrase = new Phrase("Count", times11);
                                books.AddCell(cellH5);

                                PdfPCell cellH6 = new PdfPCell();
                                //cell6.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH6.Border = 0;
                                cellH6.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellH6.Indent = 1;
                                cellH6.Phrase = new Phrase("Diff", times11);
                                books.AddCell(cellH6);
                            }
                        }

                        PdfPCell cell1 = new PdfPCell();
                        cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell1.Border = 0;
                        cell1.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell1.Indent = 1;
                        //cell1.NoWrap = true;
                        cell1.Phrase = new Phrase(c.Title, times10);
                        books.AddCell(cell1);

                        PdfPCell cell2 = new PdfPCell();
                        cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell2.Border = 0;
                        cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell2.Indent = 1;
                        //cell2.NoWrap = true;
                        cell2.Phrase = new Phrase(c.Isbn, times10);
                        books.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell3.Indent = 1;
                        //cell3.NoWrap = true;
                        cell3.Phrase = new Phrase(c.ReadLevel, times10);
                        books.AddCell(cell3);

                        PdfPCell cell4 = new PdfPCell();
                        cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell4.Border = 0;
                        cell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell4.Indent = 1;
                        cell4.Phrase = new Phrase(c.Stock.ToString(), times10);
                        books.AddCell(cell4);

                        if (reportType != 4)
                        {
                            PdfPCell cell5 = new PdfPCell();
                            cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell5.Border = 0;
                            cell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cell5.Indent = 1;
                            cell5.Phrase = new Phrase(c.Count.ToString(), times10);
                            books.AddCell(cell5);

                            PdfPCell cell6 = new PdfPCell();
                            cell6.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell6.Border = 0;
                            cell6.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cell6.Indent = 1;
                            cell6.Phrase = new Phrase(c.Diff.ToString(), times10);
                            books.AddCell(cell6);
                        }
                        count++;
                    }
                    if (count > 0)
                    {
                        Paragraph para = new Paragraph();
                        doc.Add(books);
                        para.Add("Page " + pageNo.ToString());
                        doc.Add(para);
                    }
                    doc.Close();
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=SchoolSummaryReport.pdf");

                    return File(output, "application/pdf");
                }
            }
            catch (DocumentException dex)
            {
                string bf = dex.Message;
                //CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = dex.Message;
            }

            catch (IOException ioex)
            {
                string bf = ioex.Message;
                //CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = ioex.Message;
            }

            finally
            {
                doc.Close();
            }

            return Content("");
        }
    }
}
