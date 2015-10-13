using HomeReadingManager.Models;
using HomeReadingManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Data.Entity;
using System.Data;
using HomeReadingManager.MyClasses;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

namespace HomeReadingManager.Controllers
{
    public class BarcodeLabelsController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;

        public BarcodeLabelsController()
        {
        }

        public BarcodeLabelsController(ApplicationUserManager userManager)
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
             var query = (from p in db.Products
                         where p.Isbn == search 
                         select p).FirstOrDefault();

            if (query != null)
            {
                return query.Product_Id;
            }
            else
            {
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
            
            return productId;
        }

        private bool AddToList(string isbn, int thisId, int libraryId, int userId)
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
                PrintLabel l = new PrintLabel();
                l.Product_Id = productId;
                l.Qty = 1;
                l.Library_Id = libraryId;
                l.UserId = userId;
                l.Entered = DateTime.Now;
                db.PrintLabels.Add(l);

                try
                {
                    db.SaveChanges();
                    System.Media.SystemSounds.Exclamation.Play(); 
                    return true;
                }

                catch (Exception)
                {
                    //CusValRight.ErrorMessage = "Failed to write to file.";
                    //CusValRight.IsValid = false;
                }
            }
            return false;
        }

        // GET: BarcodeLabels
        //[Authorize(Roles = "Parent helper, Teacher, Supervisor")]
        public ActionResult Index(string searchString, string sortOrder, string newOrder, int? page, int? productId, bool searchReturn = false, bool asc = true)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/BarcodeLabels/Index" });
            int libraryId = user.LibraryId;
            int teacherId = user.TeacherId;
            var query = (from u in db.UserSettings
                         where u.UserId == teacherId//user.TeacherId
                         select u).FirstOrDefault();
           
            BarcodeLabels labels = new BarcodeLabels();
            labels.LibraryId = libraryId;
            labels.UserId = teacherId;// user.TeacherId;
            if (query == null)
            {
                labels.UserSettings_Id = 0;
                labels.LabelsPerCol = 10;
                labels.ColsPerPage = 3;
                labels.LabelsTop = 0;
                labels.LabelsBottom = 0;
                labels.LabelsLeft = 0;
                labels.LabelsRight = 0;
            }
            else
            {
                labels.UserSettings_Id = query.UserSettings_Id;
                labels.LabelsPerCol = (query.LabelsPerCol == null) ? 10 : (int)query.LabelsPerCol;
                labels.ColsPerPage = (query.ColsPerPage == null) ? 3 : (int)query.ColsPerPage;
                labels.LabelsTop = (query.LabelsTop == null) ? 0 : (int)query.LabelsTop;
                labels.LabelsBottom = (query.LabelsBottom == null) ? 0 : (int)query.LabelsBottom;
                labels.LabelsLeft = (query.LabelsLeft == null) ? 0 : (int)query.LabelsLeft;
                labels.LabelsRight = (query.LabelsRight == null) ? 0 : (int)query.LabelsRight;
            }
            
            int pageSize = labels.LabelsPerCol * labels.ColsPerPage;
            labels.SortOrder = sortOrder;
            labels.Ascending  = asc;
            labels.Page = page ?? 1;

            if (productId != null && productId > 0)
            {
                //if (! AddToList("", (int)productId, libraryId, user.TeacherId))
                if (!AddToList("", (int)productId, libraryId, teacherId))
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
                        // if (!AddToList("", id, libraryId, user.TeacherId))
                        if (!AddToList("", id, libraryId, teacherId))
                        {
                            //baf error stuff here
                            System.Media.SystemSounds.Hand.Play(); 
                        }
                    }
                    else
                    {
                        // id = GoogleBooksSearch(isbn, 0, libraryId, user.TeacherId);
                        id = GoogleBooksSearch(isbn, 0, libraryId, teacherId);
                        if (id > 0)
                        {
                            //if (!AddToList("", id, libraryId, user.TeacherId))
                            if (!AddToList("", id, libraryId, teacherId))
                            {
                                //baf error stuff here
                                System.Media.SystemSounds.Hand.Play(); 
                            }
                        }
                    }
                }
                else //its a title search
                {
                    labels.TitleSearch = searchString;
                }
            }
            return View(labels);
        }

        public ActionResult LabelList(string sortOrder, string newOrder, int userId, int? page, int pageSize = 30, bool asc = true)
        {
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "Entered";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }
            IQueryable<Label> labelList = from l in db.PrintLabels
                                          where l.UserId == userId
                                          select new Label()
                                          {
                                              Print_Id = l.Print_Id,
                                              Product_Id = l.Product_Id,
                                              Isbn = l.Product.Isbn,
                                              Title = l.Product.Title,
                                              ReadLevel = l.Product.Level.ReadLevel,
                                              Entered = (DateTime)l.Entered
                                          };

            if (labelList == null)
                 return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            else if (labelList.Count() > 0)
            { 
                switch (sortOrder)
                {
                    default:
                        labelList = asc ? labelList.OrderBy(x => x.Entered) : labelList.OrderByDescending(x => x.Entered);
                        break;
                    case "Title":
                        labelList = asc ? labelList.OrderBy(x => x.Title) : labelList.OrderByDescending(x => x.Title);
                        break;
                    case "Isbn":
                        labelList = asc ? labelList.OrderBy(x => x.Isbn) : labelList.OrderByDescending(x => x.Isbn);
                        break;
                    case "ReadLevel":
                        labelList = asc ? labelList.OrderBy(x => x.ReadLevel) : labelList.OrderByDescending(x => x.ReadLevel);
                        break;
                }
                int pageNumber = (page ?? 1);
                LabelList model = new LabelList();
                model.Page = pageNumber;
                model.SortOrder = sortOrder;
                model.Ascending = asc;
                model.Labels = labelList.ToPagedList(pageNumber, pageSize);
               
                return PartialView("_LabelsList", model);
            }
            return PartialView("_NoRecords");
         }

        public ActionResult TitleSearch(string search, int? page)
        {
            if (String.IsNullOrEmpty(search))
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return Content("");
            }
            IQueryable<ProductSearch> productList = from p in db.Products
                                                    where p.Title.Contains(search)
                                                    orderby p.Title
                                                    select new ProductSearch()
                                                    {
                                                        Product_Id = p.Product_Id,
                                                        Title = p.Title,
                                                        Isbn = p.Isbn,
                                                        ReadLevel = p.Level.ReadLevel,
                                                        Image_Id = (p.ProdImages.Count() == 0) ? 0 : p.ProdImages.FirstOrDefault().Image_Id,
                                                        Jacket = (p.ProdImages.Count() == 0) ? null : p.ProdImages.FirstOrDefault().Jacket,
                                                    };
            if (productList.Count() > 0)
            {
                int pageSize = 7;
                int pageNumber = (page ?? 1);
                
                LabelTitleList model = new LabelTitleList();
                model.Page = pageNumber;
                model.SearchString = search;
                model.Titles = productList.ToPagedList(pageNumber, pageSize);
               
                return PartialView("_TitleSearch", model);

            }
            else
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = barcode + " is not a valid ISBN.";
            }
            return Content("");
        }

        public ActionResult PrintLabels(string sortOrder, int libraryId, int userId, int? page, int? pageSize, int id = 0)
        {
            bool ascending = true;
            string school = "";
            var query = (from u in db.Libraries
                         where u.Library_Id == libraryId
                         select u).FirstOrDefault();
            school = query.Licensee.Trim();
            if (sortOrder != null && sortOrder.Substring(0,1) == "%")
            {
                ascending = sortOrder == "Entered";
                sortOrder = sortOrder.Substring(1, sortOrder.Length - 1);
            }
            float topMargin = 0f; float bottomMargin = 0f; float leftMargin = 0f; float rightMargin = 0f;
            int perCol = 0; int cols = 0;
            if (id == 0)
            {
                perCol = 10;
                cols =  3;
            }
            else
            {
                UserSetting model = db.UserSettings.Find(id);
                perCol = (int)model.LabelsPerCol;
                cols =  (int)model.ColsPerPage;
                topMargin = (float)model.LabelsTop;
                bottomMargin = (float)model.LabelsBottom;
                leftMargin = (float)model.LabelsLeft;
                rightMargin = (float)model.LabelsRight;
            }
            
            string path = Server.MapPath("");
            float postScriptPointsPerMilimeter = 2.834645669f;
            

            float labelWidth = 210f / cols;
            float labelHeight = (296f - topMargin - bottomMargin) / perCol;
            float barcodeIndent = 210f / cols;
            float height1 = labelHeight * 9 / 37;
            float height3 = labelHeight * 19 / 37;

            Document doc = new Document(PageSize.A4);
            MemoryStream stream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(doc, stream);

            try
            {
                doc.SetMargins(topMargin, bottomMargin, leftMargin, rightMargin);
                doc.Open();

                Paragraph p = new Paragraph();
                p.Leading = 0;
                p.SpacingBefore = 0;
                p.SpacingAfter = 0;
                p.SetLeading(0f, 0f);
                p.Add("");
                doc.Add(p);

                var cb = writer.DirectContent;
                PdfPTable table = new PdfPTable(cols);
                table.TotalWidth = 210f * postScriptPointsPerMilimeter;
                table.LockedWidth = true;
                table.SpacingAfter = 0f;
                table.SpacingBefore = 0f;

                IQueryable<Label> reportList = from l in db.PrintLabels
                                              where l.UserId == userId
                                              select new Label()
                                              {
                                                  Print_Id = l.Print_Id,
                                                  Product_Id = l.Product_Id,
                                                  Isbn = l.Product.Isbn,
                                                  Title = l.Product.Title,
                                                  ReadLevel = l.Product.Level.ReadLevel,
                                                  Entered = (DateTime)l.Entered
                                              };

                if (reportList != null)
                 
                    switch (sortOrder)
                    {
                        default:
                            reportList = ascending ? reportList.OrderBy(x => x.Entered) : reportList.OrderByDescending(x => x.Entered);
                            break;
                        case "Title":
                            reportList = ascending ? reportList.OrderBy(x => x.Title) : reportList.OrderByDescending(x => x.Title);
                            break;
                        case "Isbn":
                            reportList = ascending ? reportList.OrderBy(x => x.Isbn) : reportList.OrderByDescending(x => x.Isbn);
                            break;
                        case "ReadLevel":
                            reportList = ascending ? reportList.OrderBy(x => x.ReadLevel) : reportList.OrderByDescending(x => x.ReadLevel);
                            break;
                    }
                    
                    if (page != null)
                    {
                        //int skip = (dpLabels.StartRowIndex / dpLabels.MaximumRows);// +1;
                        int skip = ((int)page - 1) * perCol * cols;// +1;
                        reportList = reportList.Skip(skip * perCol).Take(perCol * cols);
                    }

                    Font times10R = FontFactory.GetFont("Times Roman");
                    times10R.Size = 10;
                    times10R.SetStyle("Italic");
                    times10R.SetColor(255, 0, 0);

                    Font times10B = FontFactory.GetFont("Times Roman");
                    times10B.Size = 10;
                    times10B.SetColor(0, 0, 255);

                    float[] widths = new float[] { 3f, 1f };

                    foreach (var c in reportList)
                    {
                        PdfPTable label = new PdfPTable(2);
                        label.TotalWidth = labelWidth * postScriptPointsPerMilimeter;
                        label.SetWidths(widths);
                        label.LockedWidth = true;
                        label.HorizontalAlignment = 1;
                        label.SpacingAfter = 0f;
                        label.SpacingBefore = 0f;

                        PdfPCell cell1 = new PdfPCell();
                        cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell1.Border = 0;
                        cell1.HorizontalAlignment = 0;
                        cell1.Indent = 1;
                        cell1.PaddingTop = 1;
                        cell1.Phrase = new Phrase(school, times10R);
                        label.AddCell(cell1);

                        PdfPCell cell2 = new PdfPCell();
                        cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell2.Border = 0;
                        cell2.HorizontalAlignment = 2;
                        cell2.Indent = 1;
                        cell1.PaddingTop = 1;
                        if (c.ReadLevel != null)
                            cell2.Phrase = new Phrase("Level: " + c.ReadLevel.ToString(), times10B);
                        label.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.Colspan = 2;
                        cell3.FixedHeight = height3 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = 1;
                        BarcodeEAN ean13 = new BarcodeEAN();
                        ean13.CodeType = BarcodeEAN.EAN13;
                        ean13.Code = c.Isbn.ToString();
                        ean13.BarHeight = 12.0f * postScriptPointsPerMilimeter;
                        iTextSharp.text.Image image = ean13.CreateImageWithBarcode(cb, null, null);
                        image.ScalePercent(90);
                        cell3.AddElement(image);
                        cell3.PaddingLeft = (((210f / cols) - 30) / 2) * postScriptPointsPerMilimeter;
                        label.AddCell(cell3);

                        PdfPCell cell5 = new PdfPCell();
                        cell5.Colspan = 2;
                        cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell5.Border = 0;
                        cell5.HorizontalAlignment = 1;
                        cell1.PaddingBottom = 1;
                        cell5.Phrase = new Phrase(c.Title, times10B);
                        label.AddCell(cell5);

                        PdfPCell nesthousing = new PdfPCell(label);
                        nesthousing.Padding = 0f;
                        nesthousing.Border = 0;
                        table.AddCell(nesthousing);
                    }

                    int records = reportList.Count();
                    while (records % cols != 0)
                    {
                        PdfPTable label = new PdfPTable(2);
                        label.TotalWidth = labelWidth * postScriptPointsPerMilimeter;
                        label.SetWidths(widths);
                        label.LockedWidth = true;
                        label.HorizontalAlignment = 1;
                        label.SpacingAfter = 0f;
                        label.SpacingBefore = 0f;

                        PdfPCell nesthousing = new PdfPCell(label);
                        nesthousing.Padding = 0f;
                        nesthousing.Border = 0;
                        table.AddCell(nesthousing);
                        records++;
                    }

                    doc.Add(table);
                    doc.Close();
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=BarcodeLabels.pdf");

                    return File(output, "application/pdf");
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

        public ActionResult DeleteLabels(string sortOrder, int? page, int? pageSize, int userId, int id = 0, bool asc = true)
        {
            int perCol = 0; int cols = 0;
            if (id == 0)
            {
                perCol = 10;
                cols = 3;
            }
            else
            {
                UserSetting model = db.UserSettings.Find(id);
                perCol = (int)model.LabelsPerCol;
                cols = (int)model.ColsPerPage;
            }

            IQueryable<Label> reportList = from l in db.PrintLabels
                                            where l.UserId == userId
                                            select new Label()
                                            {
                                                Print_Id = l.Print_Id,
                                                Product_Id = l.Product_Id,
                                                Isbn = l.Product.Isbn,
                                                Title = l.Product.Title,
                                                ReadLevel = l.Product.Level.ReadLevel,
                                                Entered = (DateTime)l.Entered
                                            };
            if (reportList != null)
            {
                switch (sortOrder)
                {
                    default:
                        reportList = asc  ? reportList.OrderBy(x => x.Entered) : reportList.OrderByDescending(x => x.Entered);
                        break;
                    case "Title":
                        reportList = asc ? reportList.OrderBy(x => x.Title) : reportList.OrderByDescending(x => x.Title);
                        break;
                    case "Isbn":
                        reportList = asc ? reportList.OrderBy(x => x.Isbn) : reportList.OrderByDescending(x => x.Isbn);
                        break;
                    case "ReadLevel":
                        reportList = asc ? reportList.OrderBy(x => x.ReadLevel) : reportList.OrderByDescending(x => x.ReadLevel);
                        break;
                }

                if (page != null)
                {
                    int skip = ((int)page - 1) * perCol * cols;// +1;
                    reportList = reportList.Skip(skip * perCol).Take(perCol * cols);
                }

                foreach (var c in reportList)
                {
                    PrintLabel label = db.PrintLabels.Find(c.Print_Id);
                    db.PrintLabels.Remove(label);
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception )
                {
                    //CusValRight.IsValid = false;
                    //CusValRight.ErrorMessage = dex.Message;
                }
            }
            return RedirectToAction("Index", new { sortOrder = sortOrder, page = page });
        }

       public ActionResult Edit(int? id, string sortOrder, int userId, int? page, bool asc = true)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
           
            BarcodeLabels model = new BarcodeLabels();
            if (id == 0)
            {
                model.UserSettings_Id = 0;
                model.LabelsPerCol = 10;
                model.ColsPerPage = 3;
                model.LabelsTop = 0;
                model.LabelsBottom = 0;
                model.LabelsLeft = 0;
                model.LabelsRight = 0;
                model.UserId = userId;
             
            }
            else
            {
                 var u= db.UserSettings.Find(id);
                if (u != null)
                {
                    model.UserSettings_Id = u.UserSettings_Id;
                    model.LabelsPerCol = u.LabelsPerCol ?? 10;
                    model.ColsPerPage = u.ColsPerPage ?? 3;
                    model.LabelsTop = u.LabelsTop ?? 0;
                    model.LabelsBottom = u.LabelsBottom ?? 0;
                    model.LabelsLeft = u.LabelsLeft ?? 0;
                    model.LabelsRight = u.LabelsRight ?? 0;
                }
            }
            model.Page = page ?? 1;
            model.SortOrder = sortOrder;
            model.Ascending = asc;

            return View(model);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(BarcodeLabels model, string sortOrder, int userId, int? page)
        {
            int id = model.UserSettings_Id;
            if (id == 0)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        UserSetting us = new UserSetting();
                        us.UserId = userId;
                        us.LabelsPerCol = model.LabelsPerCol;
                        us.ColsPerPage = model.ColsPerPage;
                        us.LabelsTop = model.LabelsTop;
                        us.LabelsBottom = model.LabelsBottom;
                        us.LabelsLeft = model.LabelsLeft;
                        us.LabelsRight = model.LabelsRight;

                        db.UserSettings.Add(us);
                        db.SaveChanges();
                     }
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            else
            { 
                var recordToUpdate = db.UserSettings.Find(id);
           
                if (TryUpdateModel(recordToUpdate, "", new string[] { "LabelsPerCol", "ColsPerPage", "LabelsTop", "LabelsBottom", "LabelsLeft", "LabelsRight" }))
                {
                    try
                    {
                        db.Entry(recordToUpdate).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    catch (DataException /* dex */)
                    {
                        //Log the error (uncomment dex variable name and add a line here to write a log.
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    }
                }
            }
            return RedirectToAction("Index", new {sortOrder = sortOrder, page = page});
        }
       
        public ActionResult Delete(int id, string sortOrder, int? page)
        {
            PrintLabel label = db.PrintLabels.Find(id);
            db.PrintLabels.Remove(label);
            db.SaveChanges();
            return RedirectToAction("Index", new { sortOrder = sortOrder, page = page });
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