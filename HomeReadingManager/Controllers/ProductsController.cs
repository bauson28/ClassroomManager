using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HomeReadingManager.Models;
using PagedList;
using System.IO;
using HomeReadingManager.ViewModels;
using HomeReadingManager.MyClasses;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

namespace HomeReadingManager.Controllers
{
    public class ProductsController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;

        public ProductsController()
        {

        }

        public ProductsController(ApplicationUserManager userManager)
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
         
        private ProductList GetSingleProduct(int id, int libraryId)
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
                product.LibraryId = libraryId;
                product.MainAuthor = (query.ProdAuthors.Count() == 0) ? String.Empty : query.ProdAuthors.FirstOrDefault().Author;
                product.Description = (query.Annotations.Count() == 0) ? String.Empty : query.Annotations.FirstOrDefault().Description;
                product.Annotation_Id = (query.Annotations.Count() == 0) ? 0 : query.Annotations.FirstOrDefault().Annotation_Id;
                product.Inactive = (bool)query.Inactive;
                product.Authorised = (bool)query.Authorised;
                product.Levels_Id = (query.Levels_Id == null) ? 0 : Convert.ToInt32(query.Levels_Id);
                product.ReadLevel = (query.Levels_Id == null) ? String.Empty : query.Level.ReadLevel;
                product.Entered = (query.Entered == null) ? DateTime.Now.ToString("dd/MM/yyyy") : (Convert.ToDateTime(query.Entered)).ToString("dd/MM/yyyy");
                product.Image_Id = (query.ProdImages.Count() == 0) ? 0 : query.ProdImages.FirstOrDefault().Image_Id;
                product.Jacket = (query.ProdImages.Count() == 0) ? null : query.ProdImages.FirstOrDefault().Jacket;
                product.Onhand = (query.ProdStocks.Where(c => c.Library_Id == libraryId).Count() == 0) ? 0 : Convert.ToInt32(query.ProdStocks.FirstOrDefault(c => c.Library_Id == libraryId).Onhand);
                //product.Product_Id = query.ProdStocks.FirstOrDefault().LastCount;
                //product.Product_Id = query.ProdStocks.FirstOrDefault().LastStock;
                product.OnLoan = query.Loans.Count(l => l.ReturnDate == null && l.Student.Library_id == libraryId);
                product.Available = Math.Max(product.Onhand - product.OnLoan, 0);
                List<ProdLoans> loanList = new List<ProdLoans>();
                foreach (var item in query.Loans.Where(l => l.Student.Library_id == libraryId))
                {
                    loanList.Add(new ProdLoans(item.Student.FirstName.Trim() + " " + item.Student.LastName.Trim(), item.BorrowDate.ToShortDateString(), (item.ReturnDate == null) ? String.Empty : Convert.ToDateTime(item.ReturnDate).ToShortDateString()));
                }
                product.ProdLoans = loanList;

                loanList.Clear();
                foreach (var item in query.Loans.Where(l => l.Student.Library_id == libraryId && l.ReturnDate == null))
                {
                    loanList.Add(new ProdLoans(item.Student.FirstName.Trim() + " " + item.Student.LastName.Trim(), item.BorrowDate.ToShortDateString(), string.Empty));
                }
                product.CurrentLoans = loanList;

                List<Authors> authorList = new List<Authors>();
                foreach (var item in query.ProdAuthors)
                {
                    authorList.Add(new Authors(item.Author, item.OnixAuthorRole.Role_Id, item.OnixAuthorRole.Role));
                }
                product.Authors = authorList;

                int thisYear = DateTime.Now.Year;
                List<YearLoans> yearList = new List<YearLoans>();
                for (int i = thisYear; i > thisYear - 3; i--)
                {
                    yearList.Add(new YearLoans(i, query.Loans.Count(l => l.Student.Library_id == libraryId && l.BorrowDate.Year == i)));
                }
                product.YearLoans = yearList;
            }
            return product;
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

        private int IsbnSearch(string search)
        {
            //HomeReadingEntities dbContext = new HomeReadingEntities();

            var query = (from p in db.Products
                         where (p.Isbn == search)
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

        private int GoogleBooksSearch(string isbn, int existingId, int libraryId, int userId)
        {
            GoogleBooks gb = new GoogleBooks();
            gb.isbn = isbn;
            //gb.errorMessage 
            gb.userId = userId;
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

        // GET: Products
        //[Authorize(Roles = "Parent helper, Teacher, Supervisor")]
        public ActionResult Index(string searchString, string sortOrder, string newOrder, int? libraryId, int? page, bool searchReturn = false, bool asc = true)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Products/Index" });
                libraryId = user.LibraryId;
            }

            if (!String.IsNullOrEmpty(searchString) && !searchReturn)
            {
                string isbn = CheckCheckDigit(searchString.Trim());

                if (!string.IsNullOrEmpty(isbn))
                {
                    int id = IsbnSearch(isbn);
                    if (id > 0)
                    {
                        return RedirectToAction("Details", new { id = id, searchReturn = true });
                    }
                    else
                    {
                        id = GoogleBooksSearch(isbn, 0, 0, 0);
                        if (id > 0)
                        {
                            return RedirectToAction("Details", new { id = id, searchReturn = true });
                        }
                    }
                }
            }

            bool ascending = true;
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "Days";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }

            IQueryable<ProductList> products = from p in db.Products//.Include(p => p.Level).Include(p => p.Library).Include(p => p.ProdAvailability)
                                               where (String.IsNullOrEmpty(searchString) ? true : p.Title.Contains(searchString))
                                               select new ProductList() 
                                               { 
                                                    Product_Id = p.Product_Id,
                                                    Title = p.Title,
                                                    Isbn = p.Isbn,
                                                    MainAuthor = p.ProdAuthors.FirstOrDefault().Author,
                                                    ReadLevel = p.Level.ReadLevel,
                                                    Image_Id = p.ProdImages.Any() ? p.ProdImages.FirstOrDefault().Image_Id : 0,
                                                    Jacket = p.ProdImages.Any() ? p.ProdImages.FirstOrDefault().Jacket : null,
                                               };

            switch (sortOrder)
            {
                case "Isbn":
                    if (ascending)
                        products = products.OrderBy(s => s.Isbn);
                    else
                        products = products.OrderByDescending(s => s.Isbn);
                    break;

                case "ReadLevel":
                    if (ascending)
                        products = products.OrderBy(s => s.ReadLevel);
                    else
                        products = products.OrderByDescending(s => s.ReadLevel);
                    break;

                default:
                    if (ascending)
                        products = products.OrderBy(s => s.Title);
                    else
                        products = products.OrderByDescending(s => s.Title);
                    break;
            }
            int pageSize = 9;
            int pageNumber = (page ?? 1);
            if (pageNumber < 1) //baf xxx temp fix
                pageNumber = 1;
            TitlesModel model = new TitlesModel();
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = pageNumber;
            model.SearchString = searchString;
            model.ShowDelete = User.IsInRole("Manager");
            model.LibraryId = (int)libraryId;
            model.TitlesList = products.ToPagedList(pageNumber, pageSize);

            return View(model);
         }

        [HttpPost]
        public JsonResult IsIsbnUnique(string isbn)
        {
            try
            {
                var title = db.Products.Single(m => m.Isbn == isbn);
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsValidIsbn(string Isbn)
        {
            if (String.IsNullOrEmpty(Isbn))
                return Json(true, JsonRequestBehavior.AllowGet);

            else
            {
                Isbn = Isbn.Trim();
                if (Isbn == CheckCheckDigit(Isbn))
                    return Json(true, JsonRequestBehavior.AllowGet);
                else
                    return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Products/Details/5
        //public async Task<ActionResult> Details(int? id, string levelFilter, string searchString, string lastOrder, int? page, bool searchReturn = false)
        public ActionResult Details(int? id, int libraryId, string levelFilter, string searchString, string lastOrder, int? page, bool searchReturn = false)
        {
            ViewBag.LevelFilter = levelFilter;
            ViewBag.SearchString = searchString;
            ViewBag.SortColumn = lastOrder;
            ViewBag.SearchReturn = searchReturn;
            ViewBag.Page = page;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Product product = db.Products.Find(id);
            ProductList product = GetSingleProduct((int)id, libraryId);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create(string searchString, string sortOrder, int? libraryId, int? page, bool asc = true)
        {
            ProductList model = new ProductList();
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;
            model.SearchString = searchString;
            model.Role_Id = 1;
            model.LibraryId = (int)libraryId;

            IQueryable<LevelItem> levels = from l in db.Levels
                               where l.Obsolete != true
                               orderby l.ReadLevel
                               select new LevelItem()
                               {
                                    Id = l.Levels_Id,
                                    ReadLevel = l.ReadLevel
                               };
            model.Levels = levels;
            IQueryable<AuthorRole> roles = from r in db.OnixAuthorRoles
                                           orderby r.RoleCode
                                           select new AuthorRole
                                           {
                                               Role_Id = r.Role_Id,
                                               Role = r.Role
                                           };
            model.Roles= roles;
            
            return View(model);
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductList model)
        {
            if (ModelState.IsValid)
            {
                Product p = new Product();
                p.Isbn = model.Isbn;
                p.Title = model.Title;
                if (model.Levels_Id > 0)
                    p.Levels_Id = model.Levels_Id;
                p.Library_Id = model.LibraryId;
                p.Authorised = true;
                p.Inactive = false;
                p.Entered = DateTime.Now;
                db.Products.Add(p);

                try
                {
                    db.SaveChanges();
                    int id = p.Product_Id;
                    if (model.MainAuthor != null)
                    {
                        ProdAuthor pa = new ProdAuthor();
                        pa.Product_Id = id;
                        pa.Author = model.MainAuthor;
                        pa.Role_Id = model.Role_Id;
                        db.ProdAuthors.Add(pa);
                    }
                    if (model.Description != null)
                    {
                        Annotation a = new Annotation();
                        a.Product_Id = id;
                        a.AnnotType = "1";
                        a.Description = model.Description;
                        db.Annotations.Add(a);
                    }
                    if (model.Onhand > 0)
                    {
                        ProdStock s = new ProdStock();
                        s.Product_Id = id;
                        s.Onhand = model.Onhand;
                        s.LastCount = 0;
                        s.LastStock = 0;
                        s.Library_Id = model.LibraryId;
                        db.ProdStocks.Add(s);
                    }
                    
                    db.SaveChanges();

                    return RedirectToAction("Index", new { searchString = model.SearchString, sortOrder = model.SortOrder, asc = model.Ascending, page = model.Page, libraryId = model.LibraryId });
                }
                catch (DataException  /* dex */ )
                {
                    //var bf = dex;
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(model);
        }

        // GET: Products/Edit/5
       // public async Task<ActionResult> Edit(int? id)
        public ActionResult Edit(int? id, int libraryId, string searchString, string sortOrder, int? page, bool asc = true)
        {
            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductList model = GetSingleProduct((int)id, libraryId);
           
            if (model == null)
            {
                return HttpNotFound();
            }
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;
            model.SearchString = searchString;

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
        
        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit([Bind(Include = "Product_Id,Isbn,Catalog,Title,Imprint_Id,Availability_Id,Format_Id,Retail,AvrCost,NoStkControl,Comments,Entered,EditDate,OldIsbn,Inactive,Levels_Id,Authorised,Library_Id,Old_Id,UserName")] Product product)
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(ProductList model)
        {
            int id = model.Product_Id;
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
                                 
            var productToUpdate = db.Products.Find(id);
            var stockToUpdate = db.ProdStocks.Find(id, model.LibraryId);
            var blurbToUpdate = db.Annotations.Find(model.Annotation_Id);
                       
            if (TryUpdateModel(productToUpdate, "", new string[] { "Isbn", "Title", "Levels_Id", "Inactive" }) 
                && (stockToUpdate == null || TryUpdateModel(stockToUpdate, "", new string[] { "Onhand" }))
                && (blurbToUpdate == null || TryUpdateModel(blurbToUpdate, "", new string[] { "Description" })))
            {
                try
                {
                    db.Entry(productToUpdate).State = EntityState.Modified;
                    if (productToUpdate.Levels_Id == 0)
                        productToUpdate.Levels_Id = null;

                    if (stockToUpdate != null)
                        db.Entry(stockToUpdate).State = EntityState.Modified;
                    else if (model.Onhand > 0)
                    {
                        ProdStock ps = new ProdStock();
                        ps.Product_Id = productToUpdate.Product_Id;
                        ps.Library_Id = model.LibraryId;
                        ps.Onhand = model.Onhand;
                        ps.LastCount = 0;
                        ps.LastStock = 0;
                        db.ProdStocks.Add(ps);
                    }

                    if (blurbToUpdate != null)
                        db.Entry(blurbToUpdate).State = EntityState.Modified;
                    else if (model.Description != null)
                    {
                        Annotation a = new Annotation();
                        a.Product_Id = model.Product_Id;
                        a.AnnotType = "1";
                        a.Description = model.Description;
                        db.Annotations.Add(a);
                    }
                   // HttpPostedFileBase jacket = Request.Files["jacket"];

                    if (Request.Files["jacket"] != null && Request.Files["jacket"].ContentLength > 0)
                    {
                        if (Request.Files["jacket"].ContentLength < 1024 * 1024 && Request.Files["jacket"].ContentType == "image/jpeg")
                        {
                            byte[] Image;
                            using (var binaryReader = new BinaryReader(Request.Files["jacket"].InputStream))
                            {
                                Image = binaryReader.ReadBytes(Request.Files["jacket"].ContentLength);
                            }
                            if (Image != null)
                                if (productToUpdate.ProdImages.Count() == 0)
                                {
                                    ProdImage pi = new ProdImage();
                                    pi.Product_Id = productToUpdate.Product_Id;
                                    pi.ImageSize = "S";
                                    pi.Jacket = Image;
                                    db.ProdImages.Add(pi);
                                }
                                else
                                {
                                    productToUpdate.ProdImages.First().Jacket = Image;
                                }
                           
                        }
                    }
                    db.SaveChanges();

                    return RedirectToAction("Index", new { sortOrder = model.SortOrder, asc = model.Ascending, page = model.Page, libraryId = model.LibraryId });

                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(productToUpdate);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id, string searchString, string lastOrder, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }
                
        // POST: Products/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteConfirmed(int id)
        //{
        //    Product product = await db.Products.FindAsync(id);
        //    db.Products.Remove(product);
        //    await db.SaveChangesAsync();
        //    return RedirectToAction("Index");
        //}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);

            var query1 = from pa in db.Annotations
                         where pa.Product_Id == id
                         select pa;

            foreach (var Annotation in query1)
            {
                db.Annotations.Remove(Annotation);
            }

            var query2 = from pq in db.ProdStocks
                         where pq.Product_Id == id
                         select pq;
            foreach (var ProdStock in query2)
            {
                db.ProdStocks.Remove(ProdStock);
            }

            var query3 = from a in db.ProdAuthors
                         where a.Product_Id == id
                         select a;
            foreach (var ProdAuthor in query3)
            {
                db.ProdAuthors.Remove(ProdAuthor);
            }

            var query4 = from pi in db.ProdImages
                         where pi.Product_Id == id
                         select pi;
            foreach (var ProdImage in query4)
            {
                db.ProdImages.Remove(ProdImage);
            }
            db.SaveChanges();
            return RedirectToAction("Index"); //baf xxxxx need parameters
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
