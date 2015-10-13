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
using HomeReadingManager.MyClasses;
using HomeReadingManager.ViewModels;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace HomeReadingManager.Controllers
{
    public class LoansController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();

        private ApplicationUserManager _userManager;

        public LoansController()
        {
        }

        public LoansController(ApplicationUserManager userManager)
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

        protected bool ProcessBarcode(string isbn, bool returns, int studentId, int libraryId)
        {
            if (returns)
            {
                return ReturnItem(studentId, isbn);
            }
            else
            {
                if (IsbnInDatabase(isbn) || GoogleBooksSearch(isbn, libraryId))
                {
                    return CreateLoan(studentId, isbn);
                }
                else
                    System.Media.SystemSounds.Hand.Play();
            }

            return false;
        }

        private bool ReturnItem(int studentId, string isbn)
        {
            var query = (from loan in db.Loans
                         where loan.Product.Isbn == isbn && !loan.ReturnDate.HasValue && loan.Student_Id == studentId
                         select loan).SingleOrDefault();

            if (query != null)
            {
                query.ReturnDate = DateTime.Now;
                if (String.IsNullOrEmpty(query.Loans_Id.ToString()))
                {
                    var queryp = (from p in db.Products
                                  where p.Isbn == isbn
                                  select p).SingleOrDefault();

                    //CusValRight.IsValid = false;
                    if (queryp == null || String.IsNullOrEmpty(queryp.Product_Id.ToString()))
                    {
                        //CusValRight.ErrorMessage = "Invalid barcode.";
                    }

                    else
                    {
                        //CusValRight.ErrorMessage = "Item not on loan to this student.";

                    }
                    //tbBarcode.Text = "";


                }
                else
                {
                    try
                    {
                        db.SaveChanges();
                        System.Media.SystemSounds.Asterisk.Play();
                        return true;
                        //if (!LoadOutstandingLoans(studentId, true))
                        //{
                        //ShowLoans(studentId, true);
                        //}
                    }

                    catch (Exception)
                    {
                        //CusValRight.IsValid = false;
                        // CusValRight.ErrorMessage = "Failed to write to file.";
                    }
                    finally
                    {
                        //tbBarcode.Text = "";
                        //tbBarcode.Focus();
                    }
                }
            }
            System.Media.SystemSounds.Hand.Play();
            return false;
        }

        private bool CreateLoan(int studentId, string isbn)
        {
            var count = (from loan in db.Loans
                         where loan.Product.Isbn == isbn && loan.Student_Id == studentId && !loan.ReturnDate.HasValue
                         select loan).Count();

            if (count > 0)
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = "This student already has this book on loan.";
                System.Media.SystemSounds.Hand.Play();
                return false;
            }
            else
            {
                var queryp = (from p in db.Products
                              where p.Isbn == isbn
                              select p).SingleOrDefault();


                if (queryp == null || String.IsNullOrEmpty(queryp.Product_Id.ToString()))
                {
                    //CusValRight.IsValid = false;
                    //CusValRight.ErrorMessage = "Invalid barcode.";
                    //tbBarcode.Text = "";

                }
                else
                {
                    int productId = queryp.Product_Id;

                    Loan l = new Loan();
                    l.Product_Id = productId;
                    l.Student_Id = studentId;
                    l.BorrowDate = DateTime.Now;
                    db.Loans.Add(l);

                    try
                    {
                        db.SaveChanges();
                        System.Media.SystemSounds.Exclamation.Play();
                        return true;
                    }

                    catch (Exception)
                    {
                        //CusValRight.IsValid = false;
                        //CusValRight.ErrorMessage = "Failed to write to file.";

                    }
                }
                System.Media.SystemSounds.Hand.Play();
                return false;
            }
        }

        private bool CreateLoan(int studentId, int productId)
        {
            var count = (from loan in db.Loans
                         where loan.Product_Id == productId && loan.Student_Id == studentId && !loan.ReturnDate.HasValue
                         select loan).Count();

            if (count > 0)
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = "This student already has this book on loan.";
                System.Media.SystemSounds.Hand.Play();
            }
            else
            {
                Loan l = new Loan();
                l.Product_Id = productId;
                l.Student_Id = studentId;
                l.BorrowDate = DateTime.Now;
                db.Loans.Add(l);

                try
                {
                    db.SaveChanges();
                    System.Media.SystemSounds.Exclamation.Play();
                    return true;
                }

                catch (Exception)
                {
                    //CusValRight.IsValid = false;
                    //CusValRight.ErrorMessage = "Failed to write to file.";

                }
            }
            System.Media.SystemSounds.Hand.Play();
            return false;
        }

        private bool IsbnInDatabase(string isbn)
        {
            int count = (from p in db.Products
                         where p.Isbn == isbn
                         select p).Count();

            return (count > 0);
        }

        private bool GoogleBooksSearch(string isbn, int libraryId)
        {
            GoogleBooks gb = new GoogleBooks();
            gb.isbn = isbn;
            gb.userId = 0;
            gb.libraryId = libraryId;
            gb.levelsId = 0;
            gb.doLabels = false;
            gb.existingId = 0;
            gb.addStock = true;
            gb.physicalFolder = Server.MapPath("~/");
            int productId = gb.GoogleSearch();

            return productId > 0;
        }

        private IQueryable<StudentHR> GetHasReturnsList(string sortOrder, bool ascending, int libraryId)
        {
            IQueryable<StudentHR> students = from l in db.Loans.Where(l => l.Student.Library_id == libraryId && !l.ReturnDate.HasValue).GroupBy(x => x.Student_Id).Select(g => g.FirstOrDefault())

                                             select new StudentHR()
                                             {
                                                 Id = l.Student_Id,
                                                 FirstName = l.Student.FirstName,
                                                 LastName = l.Student.LastName,
                                                 ClassId = (l.Student.Classes_Id == null) ? 0 : (int)l.Student.Classes_Id,
                                                 ClassName = (!l.Student.Classes_Id.HasValue) ? String.Empty : l.Student.Class.ClassDesc,
                                                 LevelId = (!l.Student.Levels_Id.HasValue) ? 0 : (int)l.Student.Level.Levels_Id,
                                                 ReadLevel = (!l.Student.Levels_Id.HasValue) ? String.Empty : l.Student.Level.ReadLevel,
                                                 Gender = l.Student.Gender,
                                                 Name = l.Student.FirstName.Trim() + " " + l.Student.LastName.Trim()
                                             };

            students = SortStudentList(students, sortOrder, ascending);

            return students;
        }

        private IQueryable<StudentHR> GetClassList(int classId, string sortOrder, bool ascending)
        {
            IQueryable<StudentHR> students = from s in db.Students
                                             where s.Classes_Id == classId && s.Inactive == false
                                             select new StudentHR()
                                             {
                                                 Id = s.Student_Id,
                                                 FirstName = s.FirstName,
                                                 LastName = s.LastName,
                                                 ClassId = (s.Classes_Id == null) ? 0 : (int)s.Classes_Id,
                                                 ClassName = (s.Classes_Id == null) ? String.Empty : s.Class.ClassDesc,
                                                 LevelId = (s.Levels_Id == null) ? 0 : (int)s.Levels_Id,
                                                 ReadLevel = (s.Levels_Id == null) ? String.Empty : s.Level.ReadLevel,
                                                 Gender = s.Gender,
                                                 Name = s.FirstName.Trim() + " " + s.LastName.Trim()
                                             };

            students = SortStudentList(students, sortOrder, ascending);

            return students;
        }

        private IQueryable<StudentHR> SortStudentList(IQueryable<StudentHR> students, string sortOrder, bool ascending)
        {
            switch (sortOrder)
            {
                case "Gender":
                    if (ascending)
                        students = students.OrderBy(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Level":
                    if (ascending)
                        students = students.OrderBy(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                default:
                    if (ascending)
                        students = students.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }
            return students;
        }

        private IQueryable<StudentHR> GetSearchList(string search, int libraryId, string sortOrder, bool ascending)
        {

            IQueryable<StudentHR> students = from s in db.Students
                                             where (s.FirstName.StartsWith(search) || s.LastName.StartsWith(search)) && s.Inactive == false && s.Library_id == libraryId
                                             select new StudentHR()
                                             {
                                                 Id = s.Student_Id,
                                                 FirstName = s.FirstName,
                                                 LastName = s.LastName,
                                                 ClassId = (s.Classes_Id == null) ? 0 : (int)s.Classes_Id,
                                                 ClassName = (s.Classes_Id == null) ? String.Empty : s.Class.ClassDesc,
                                                 LevelId = (s.Levels_Id == null) ? 0 : (int)s.Levels_Id,
                                                 ReadLevel = (s.Levels_Id == null) ? String.Empty : s.Level.ReadLevel,
                                                 Gender = s.Gender,
                                                 Name = s.FirstName.Trim() + " " + s.LastName
                                             };

            students = SortStudentList(students, sortOrder, ascending);

            return students;
        }

        private IQueryable<StudentHR> GetSearchListBook(string isbn, int libraryId, string sortOrder, bool ascending)
        {

            IQueryable<StudentHR> students = from l in db.Loans
                                             where l.Product.Isbn == isbn && l.ReturnDate == null && l.Student.Library_id == libraryId
                                             select new StudentHR()
                                             {
                                                 Id = l.Student_Id,
                                                 FirstName = l.Student.FirstName,
                                                 LastName = l.Student.LastName,
                                                 ClassId = (l.Student.Classes_Id == null) ? 0 : (int)l.Student.Classes_Id,
                                                 ClassName = (l.Student.Classes_Id == null) ? String.Empty : l.Student.Class.ClassDesc,
                                                 LevelId = (l.Student.Levels_Id == null) ? 0 : (int)l.Student.Levels_Id,
                                                 ReadLevel = (l.Student.Levels_Id == null) ? String.Empty : l.Student.Level.ReadLevel,
                                                 Gender = l.Student.Gender,
                                                 Name = l.Student.FirstName.Trim() + " " + l.Student.LastName
                                             };

            students = SortStudentList(students, sortOrder, ascending);

            return students;
        }

        public ActionResult PleaseSelect(int? libraryId)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Loans/StudentLoans" });
                libraryId = user.LibraryId;
            }

            StudentSearch model = new StudentSearch();
            model.Student = "";
            model.StudentId = 0;
            model.ClassId = null; //0 baf 300715
            model.ClassName = "";
            model.Search = "";
            model.LibraryId = (int)libraryId;

            IEnumerable<ClassItem> classes = from c in db.Classes
                                             where c.Library_Id == libraryId && !c.Obsolete
                                             orderby c.ClassDesc
                                             select new ClassItem()
                                             {
                                                 Id = c.Classes_Id,
                                                 ClassName = c.ClassDesc
                                             };
            model.Classes = classes;

            return PartialView("_StudentSearch", model);
        }

        [Authorize(Roles = "Parent helper, Teacher, Supervisor")]
        public ActionResult StudentLoans(string sortOrder, string newOrder, string className, string search, int? classId, int? libraryId, int? page, bool asc = true)
        {

            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Loans/StudentLoans" });
                libraryId = user.LibraryId;
            }

            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "FirstName";
            }
            else
            {
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                {
                    sortOrder = newOrder;
                }
            }

            HomeReading model = new HomeReading();

            model.StudentId = 0;
            model.Student = "";
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Search = search;
            model.LibraryId = (int)libraryId;
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            model.Page = pageNumber;
            if (classId != null && (int)classId > 0)
            {
                model.StudentHRs = GetClassList((int)classId, sortOrder, asc).ToPagedList(pageNumber, pageSize);
                model.ClassId = (int)classId;
                if (String.IsNullOrEmpty(className))
                    model.FilterName = "Class " + db.Classes.Find((int)classId).ClassDesc.Trim();
                else
                    model.FilterName = "Class " + className;

            }
            else if (!String.IsNullOrEmpty(search))
            {
                string isbn = ProductClass.CheckCheckDigit(search.Trim());
                if (!string.IsNullOrEmpty(isbn))
                {
                    model.StudentHRs = GetSearchListBook(isbn, model.LibraryId, sortOrder, asc).ToPagedList(pageNumber, pageSize);
                    model.FilterName = "Students with " + search + " on loan";
                }
                else
                {
                    model.StudentHRs = GetSearchList(search, model.LibraryId, sortOrder, asc).ToPagedList(pageNumber, pageSize);
                    model.FilterName = "Students names starting '" + search + "'";
                }
            }
            else
            {
                model.StudentHRs = GetHasReturnsList(sortOrder, asc, model.LibraryId).ToPagedList(pageNumber, pageSize);
                model.FilterName = "Students with outstanding loans";
            }

            if (Request.IsAjaxRequest())
                return PartialView("_StudentList", model);
            else
                return View("StudentLoans", model);

        }

        // GET: Loans/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Loan loan = await db.Loans.FindAsync(id);
            if (loan == null)
            {
                return HttpNotFound();
            }
            return View(loan);
        }

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Loan loan = await db.Loans.FindAsync(id);
            db.Loans.Remove(loan);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult HomeReading(int studentId)
        {
            if (studentId == 0)
            {
                return RedirectToAction("PleaseSelect");
            }
            // string student = "";
            //var temp = db.Students.Where(m => m.Student_Id == studentId).FirstOrDefault();
            //if (temp != null)
            //     student = temp.FirstName.Trim() + " " + temp.LastName.Trim();
            DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            DateTime overdueDate = DateTime.Now.AddDays(-7);
            try
            {
                var students = (from s in db.Students.Where(s => s.Student_Id == studentId)

                                join l2 in
                                    (from l in db.Loans.Where(d => d.BorrowDate >= startDate && d.Student_Id == studentId)
                                     group l by l.Student_Id into grp
                                     let count = grp.Count()
                                     select new { Student_Id = grp.Key, count }) on s.Student_Id equals l2.Student_Id into l3
                                from z in l3.DefaultIfEmpty()

                                join l4 in
                                    (from l in db.Loans.Where(d => d.ReturnDate == null && d.Student_Id == studentId)// && d.BorrowDate < overdueDate)
                                     group l by l.Student_Id into grp
                                     let maxDate = (DateTime?)grp.OrderByDescending(g => g.BorrowDate).FirstOrDefault().BorrowDate //as DateTime?
                                     let count = grp.Count()
                                     select new { Student_Id = grp.Key, maxDate, count }) on s.Student_Id equals l4.Student_Id into l5
                                from z2 in l5.DefaultIfEmpty()

                                join l8 in
                                    (from l in db.Loans.Where(d => d.ReturnDate == null && d.Student_Id == studentId && d.BorrowDate < overdueDate)
                                     group l by l.Student_Id into grp
                                     let maxDate = (DateTime?)grp.OrderByDescending(g => g.BorrowDate).FirstOrDefault().BorrowDate //as DateTime?
                                    // let count = grp.Count()
                                     select new { Student_Id = grp.Key, maxDate }) on s.Student_Id equals l8.Student_Id into l9
                                from z3 in l9.DefaultIfEmpty()
                                select new
                                {
                                    Id = s.Student_Id,
                                    Student = s.FirstName.Trim() + " " + s.LastName.Trim(),
                                    ReadLevel = "Reading level: " + ((s.Levels_Id == null) ? "Not set" : s.Level.ReadLevel),
                                    ClassName = (s.Classes_Id == null) ? "" : "Class: " + s.Class.ClassDesc.Trim(),
                                    BooksRead = "Books read: " + ((int?)z.count ?? 0).ToString(),
                                    OnLoan = "Current loans: " + ((int?)z2.count ?? 0).ToString(),
                                    MaxDate = z3.maxDate
                                }).FirstOrDefault();

                if (students.Id == 0)
                    return Content(""); //baf temp

                StudentLoans model = new StudentLoans();
                model.StudentId = studentId;
                model.Student = students.Student;
                model.ReadLevel = students.ReadLevel;
                model.ClassName = students.ClassName;
                model.BooksRead = students.BooksRead;
                model.OnLoan = students.OnLoan;
                model.Overdue = ((students.MaxDate == null) ? "" : "Overdue: " + ((DateTime)students.MaxDate).ToString("dd MMM yyyy"));
                model.Loans = GetCurrentLoans(studentId);
                return PartialView("_SelectedStudent", model);
            }
            catch (Exception ex)
            {
                var bf = ex;
            }
            return RedirectToAction("PleaseSelect");
        }

        private IQueryable<OnLoan> GetCurrentLoans(int studentId)
        {
            IQueryable<OnLoan> loans = from l in db.Loans
                                       // where l.Student_Id == studentId && !l.ReturnDate.HasValue && System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) != System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now)
                                       where l.Student_Id == studentId && (!l.ReturnDate.HasValue || System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now)
                                                                        || System.Data.Entity.DbFunctions.TruncateTime(l.ReturnDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now))

                                       select new OnLoan
                                       {
                                           LoanId = l.Loans_Id,
                                           ProductId = l.Product_Id,
                                           Title = l.Product.Title,
                                           Isbn = l.Product.Isbn,
                                           ReadLevel = l.Product.Level.ReadLevel == null ? "" : l.Product.Level.ReadLevel,
                                           ImageId = (l.Product.ProdImages.Count() == 0) ? 0 : l.Product.ProdImages.FirstOrDefault().Image_Id,
                                           Jacket = (l.Product.ProdImages.Count() == 0) ? null : l.Product.ProdImages.FirstOrDefault().Jacket,
                                           BorrowDate = l.BorrowDate,
                                           ReturnDate = l.ReturnDate,
                                           TodaysLoan = System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now),
                                           DisplayDate = (System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now)) ? "NEW LOAN" :
                                                            (l.ReturnDate != null) ? "RETURNED" : System.Data.Entity.DbFunctions.DiffDays(l.BorrowDate, DateTime.Now).ToString() + " DAYS"
                                       };
            return loans;
        }

        public ActionResult BookSearch(int studentId, string search, int? page)
        {
            if (search == null || studentId == 0)
            {
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
            IQueryable<ProductSearch> productList = from p in db.Products
                                                    where p.Title.Contains(search)
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
                int pageSize = 5;
                int pageNumber = (page ?? 1);
                BookSearch model = new BookSearch();
                model.StudentId = studentId;
                model.Search = search;
                model.Page = pageNumber;
                model.Titles = productList.ToPagedList(pageNumber, pageSize);

                return PartialView("_BookSearch", model);

            }
            else
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = barcode + " is not a valid ISBN.";
            }
            return Content("");
        }

        public ActionResult ShowCurrentLoansView(int studentId)
        {
            IQueryable<OnLoan> model = GetCurrentLoans(studentId);
            return PartialView("_Loans", model);
        }
    }
}
