using HomeReadingManager.Models;
using HomeReadingManager.MyClasses;
using HomeReadingManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

namespace HomeReadingManager.Controllers
{
    public class TitleSearchController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
       
        private ApplicationUserManager _userManager;

        public TitleSearchController()
        {
        }

        public TitleSearchController(ApplicationUserManager userManager)
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
        //private IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue)
        //{
        //    var items = new List<SelectListItem>();
        //    items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
        //    items.AddRange(list);
        //    return items;
        //}

        private ProductList GetSingleProduct(int id, int libraryId, int userId)
        {
           
            var query = (from p in db.Products
                         where p.Product_Id == id
                         select p).FirstOrDefault();

            ProductList product = new ProductList();
            if (query != null)
            {
                product.Product_Id = query.Product_Id;
                product.Title = query.Title;
                product.Isbn = query.Isbn;

                product.Description = (query.Annotations.Count() == 0) ? String.Empty : query.Annotations.FirstOrDefault().Description;
                product.Annotation_Id = (query.Annotations.Count() == 0) ? 0 : query.Annotations.FirstOrDefault().Annotation_Id;
                product.Inactive = (bool)query.Inactive;
                product.Authorised = (bool)query.Authorised;
                product.Levels_Id = (query.Levels_Id == null) ? 0 : Convert.ToInt32(query.Levels_Id);
                product.ReadLevel = (query.Levels_Id == null) ? String.Empty : query.Level.ReadLevel;
                product.Entered = (query.Entered == null) ? DateTime.Now.ToString("dd/MM/yyyy") : (Convert.ToDateTime(query.Entered)).ToString("dd/MM/yyyy");//(query.Entered == null) ? DateTime.Now : Convert.ToDateTime(query.Entered);
                product.Image_Id = (query.ProdImages.Count() == 0) ? 0 : query.ProdImages.FirstOrDefault().Image_Id;
                product.Jacket = (query.ProdImages.Count() == 0) ? null : query.ProdImages.FirstOrDefault().Jacket;
                product.Onhand = (query.ProdStocks.Where(c => c.Library_Id == libraryId).Count() == 0) ? 0 : Convert.ToInt32(query.ProdStocks.FirstOrDefault(c => c.Library_Id == libraryId).Onhand);
                
                List<Authors> authorList = new List<Authors>();
                foreach (var item in query.ProdAuthors)
                {
                    authorList.Add(new Authors(item.Author, item.OnixAuthorRole.Role_Id, item.OnixAuthorRole.Role));
                }
                product.Authors = authorList;

                int labelCount = (from p in db.PrintLabels
                              where p.Product_Id == id && p.UserId == userId
                              select p).Count();
                product.Labels = labelCount;
            }
            return product;
        }

        private int CheckIsbnUnique(string isbn)
        {
            int productId = 0;
            using (var dbContext = new HomeReadingEntities())
            {
                var query = (from p in dbContext.Products
                             where p.Isbn == isbn
                             select p).FirstOrDefault();
                if (query != null)
                {
                    productId = query.Product_Id;
                    //lbProduct.Text = query.Title;
                }
                return productId;
            }
        }

        private string ValidateBarcode(string barcode)
        {
            string first = barcode.Substring(0, 1).ToString();

            string baddies = "/='.,\\\"";
            if (baddies.Contains(first))
            {
                return "The barcode's first character is invalid.";
            }
            else if (barcode.Contains(";"))
            {
                return "The barcode may not contain a semicolon.";
            }
            else
                return string.Empty;
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

        public string GetLineBreaks(object Blurb)
        {
            return (Blurb.ToString().Replace(Environment.NewLine, "<br/>"));
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

        private int GoogleBooksSearch(string isbn, int existingId, bool labels, bool stock, int levels_Id, int libraryId, int userId, ref string message)
        {
            int productId = 0;

            GoogleBooks gb = new GoogleBooks();
            gb.isbn = isbn;
            //gb.errorMessage 
            gb.userId = userId;
            gb.libraryId = libraryId;
            gb.levelsId = levels_Id;
            gb.doLabels = labels; //chLabels
            gb.existingId = existingId;
            gb.addStock = stock; //chStock
            gb.physicalFolder = Server.MapPath("~/");
            productId = gb.GoogleSearch();
            if (productId == 0 && !string.IsNullOrEmpty(gb.errorMessage))
            {
                //CusVal.IsValid = false;
                //CusVal.ErrorMessage = gb.errorMessage;
                message = gb.errorMessage;
            }

            return productId;
        }

        private string ProcessBarcode(string barcode, bool incrementExisting, bool updateExisting, bool labels, bool stock, int levels_Id, int libraryId, int userId)
        {
            string message = ValidateBarcode(barcode);
            bool incremented = false;
            if (!string.IsNullOrEmpty(message))
            {
                //SetCustomValidator(message);
            }
            else
            {
                barcode = CheckCheckDigit(barcode);

                if (String.IsNullOrEmpty(barcode))
                {
                    //SetCustomValidator(tbBarcode.Text + " is not a valid ISBN.");
                    message = barcode + " is not a valid ISBN.";
                }
                else
                {
                    int productId = CheckIsbnUnique(barcode);
                    if (productId > 0 && incrementExisting)
                    {
                        if (DoIncrementStock(productId, false, labels, libraryId, userId))
                        {
                            //SetCustomValidator("Stock incremented by 1");
                            message = barcode + " - stock incremented by 1";
                            incremented = true;
                        }
                    }

                    if (productId == 0 || updateExisting)//(System.Web.Security.Roles.IsUserInRole(Page.User.Identity.Name, "Managers")
                    {
                        productId = GoogleBooksSearch(barcode, productId, labels, stock, levels_Id, libraryId, userId, ref message);
                        if (productId == 0)
                        {
                            // SetCustomValidator("ISBN " + isbn + " was not found on the website.");
                           // message = "ISBN " + barcode + " was not found on the website.";
                        }
                    }
                    else if (!incremented)
                    {
                        //SetCustomValidator(tbBarcode.Text + " is already in the database.");
                        message = barcode + " is already in the database.";
                    }
                }
            }
            return message;
         }

        private bool DoIncrementStock(int productId, bool minus, bool labels, int libraryId, int userId)
        {
            bool success = false;

            if (productId > 0)
            {
                bool bale = false;
                
                var query = (from ps in db.ProdStocks
                                where ps.Product_Id == productId && ps.Library_Id == libraryId
                                select ps).FirstOrDefault();

                if (minus)
                {
                    if (query != null && Convert.ToInt32(query.Onhand) > 0)
                        query.Onhand--;
                    else
                        bale = true;
                }
                else
                {
                    if (query != null)
                    {
                        query.Onhand++;
                    }
                    else
                    {
                        ProdStock ps2 = new ProdStock();
                        ps2.Product_Id = productId;
                        ps2.Library_Id = libraryId;
                        ps2.Location = string.Empty;
                        ps2.Shortage = false;
                        ps2.Onhand = 1;
                        ps2.StockCount = 0;
                        ps2.LastCount = 0;
                        db.ProdStocks.Add(ps2);
                    }
                    if (labels)
                        PrintLabel(productId, libraryId, userId);
                }
                if (!bale)
                {
                    try
                    {
                        db.SaveChanges();
                        success = true;

                    }
                    catch (Exception)
                    {
                        //SetCustomValidator("Failed to increment stock.");
                    }
                }
            }
            return success;
        }

        private void PrintLabel(int productId, int libraryId, int userId)
         {
             PrintLabel pl = new PrintLabel();
             pl.Product_Id = productId;
             pl.Library_Id = libraryId;
             pl.Qty = 1;
             pl.UserId = userId;
             pl.Entered = DateTime.Now;
             db.PrintLabels.Add(pl);
         }

        // GET: TitleSearch
        [Authorize(Roles = "Parent helper, Teacher, Supervisor")]
        public ActionResult Index(int? titleId, int? libraryId, int? userId, bool? Labels, bool? SetNewStock, bool? IncrementStock, int? SetLevels_Id, bool? DoUpdate, string barcode)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/TitleSearch/Index" });
                libraryId = user.LibraryId;
                userId = user.TeacherId;
            }
            TitleSearch model = new TitleSearch();

            model.Labels = (Labels == null) ? false : (bool)Labels;
            model.SetNewStock = (SetNewStock == null) ? false : (bool)SetNewStock;
            model.IncrementStock = (IncrementStock == null) ? false : (bool)IncrementStock;
            model.SetLevels_Id = (SetLevels_Id == null) ? 0 : (int)SetLevels_Id;
            model.DoUpdate = (DoUpdate == null) ? false : (bool)DoUpdate;
            model.LibraryId = (int)libraryId;
            model.UserId = (int)userId;
           
            if (!String.IsNullOrEmpty(barcode))
            {
                barcode = ProductClass.CheckCheckDigit(barcode.Trim());
                if (!string.IsNullOrEmpty(barcode))
                {
                    model.Message = ProcessBarcode(barcode, model.IncrementStock, model.DoUpdate, model.Labels, model.SetNewStock, model.SetLevels_Id, model.LibraryId, model.UserId);
                }
                else
                    model.Message = "Not a valid ISBN.";
            }
            else
                model.Message = "Please scan a barcode";

            IQueryable<ProductTitle> enteredList = (from p in db.Products
                                                    where p.UserId == (int)userId && System.Data.Entity.DbFunctions.TruncateTime(p.Entered.Value) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now)
                         orderby p.Entered descending
                         select new ProductTitle()
                         {
                             TitleId = p.Product_Id,
                             Title = p.Title,
                             Jacket = (p.ProdImages.Count() == 0) ? null : p.ProdImages.FirstOrDefault().Jacket
                         }).Take(5);
            model.ProductTitles = enteredList;

            if (titleId == null || titleId == 0)
                model.TitleId = (enteredList.Count() == 0) ? 0 : enteredList.First().TitleId;
            else
                model.TitleId = (int)titleId;
            
            IQueryable<LevelItem> levels = from l in db.Levels
                                           where l.Obsolete != true
                                           orderby l.ReadLevel
                                           select new LevelItem()
                                           {
                                               Id = l.Levels_Id,
                                               ReadLevel = l.ReadLevel
                                           };
            model.Levels = levels;

            return View(model);
        }

        public ActionResult TitleDetails(int? id, int libraryId, int userId)
        {
            ProductList product;
            if (id == null || id == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                product = new ProductList();
                product.Product_Id = 0;
                List<Authors> authorList = new List<Authors>() ;
                authorList.Add(new Authors("", 0, ""));
                product.Authors = authorList;
            }
            // Product product = db.Products.Find(id);
            else
            {
                product = GetSingleProduct((int)id, libraryId, userId);
                if (product == null)
                {
                    return HttpNotFound();
                }
            }
            IQueryable<LevelItem> levels = from l in db.Levels
                                           where l.Obsolete != true
                                           orderby l.ReadLevel
                                           select new LevelItem()
                                           {
                                               Id = l.Levels_Id,
                                               ReadLevel = l.ReadLevel
                                           };
            product.Levels = levels;

            return PartialView("_TitleDetails", product);
        }

        public ActionResult SetReadingLevel(int id, int levelId)
        {

            if (levelId > 0 && levelId < 32 && id > 0)
            {
                var productToUpdate = db.Products.Find(id);
                try
                {
                    productToUpdate.Levels_Id = levelId;
                    db.SaveChanges();
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Content("");
        }

        public ActionResult IncrementLabels(int id, int inc, int libraryId, int userId)
        {
            if (id > 0)
            {
                if (inc > 0)
                {
                    PrintLabel(id, libraryId, userId);
                }
                else
                {
                     var query = (from l in db.PrintLabels
                             where l.Product_Id == id && l.UserId == userId
                             select l).FirstOrDefault();
                    if (query != null)
                    {
                        db.PrintLabels.Remove(query);
                    }
                }
                try
                {
                    db.SaveChanges();
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Content("");
        }

        public ActionResult IncrementStock(int id, bool decrement, bool doLabels, int libraryId, int userId)
        {
            if (id > 0)
            {

                if (DoIncrementStock(id, decrement, doLabels, libraryId, userId) && decrement && doLabels)
                    IncrementLabels(id, -1, libraryId, userId);
            }
            return Content("");
        }

        public ActionResult NoDetails()
        {
            return PartialView("_NoDetails");
        }

        // GET: TitleSearch/Details/5
        public ActionResult Details(int id)
        {

            return View();
        }

        // GET: TitleSearch/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TitleSearch/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TitleSearch/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TitleSearch/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TitleSearch/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TitleSearch/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
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
    }
}
