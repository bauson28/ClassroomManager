using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HomeReadingManager.Models;
using PagedList;
using HomeReadingManager.ViewModels;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text.html;
using HomeReadingManager.MyClasses;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

//using HomeReadingManager.MyClasses;

namespace HomeReadingManager.Controllers
{
    public class StudentsController : Controller
    {
        //System.Media.SystemSounds.Beep.Play(); 
        //System.Media.SystemSounds.Asterisk.Play(); 
        //System.Media.SystemSounds.Exclamation.Play(); 
        //System.Media.SystemSounds.Question.Play(); 
        //System.Media.SystemSounds.Hand.Play(); 
        // private SchoolContext db = new SchoolContext()
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;
        // protected BaseFont font_ascii = iTextSharp.text.pdf.BaseFont.CreateFont(@"C:/Windows/Fonts/ARIAL.TTF", BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.EMBEDDED);
        //BaseFont.CreateFont("C:\Windows\Fonts\Ariel.ttf",BaseFont.IDENTITY_H,BaseFont.N‌​OT_EMBEDDED)
        public StudentsController()
        {
        }

        public StudentsController(ApplicationUserManager userManager)
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

        private float postScriptPointsPerMilimeter = 2.834645669f;

        //protected Font body_ascii
        //{
        //    get
        //    {
        //        return new iTextSharp.text.Font(font_ascii, 12, Font.NORMAL, new BaseColor(0, 0, 0));
        //    }
        //}

        private IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue, bool addLine)
        {
            var items = new List<SelectListItem>();
            if (addLine)
                items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
            items.AddRange(list);
            return items;
        }

        private IQueryable<StudentVM> GetStudentList(int classId, string sortOrder, bool ascending)
        {
            IQueryable<StudentVM> students = from s in db.Students
                                             where s.Classes_Id == classId && s.Inactive == false
                                             select new StudentVM()
                                             {
                                                 Id = s.Student_Id,
                                                 FirstName = s.FirstName,
                                                 LastName = s.LastName,
                                                 ClassId = (s.Classes_Id == null) ? 0 : (int)s.Classes_Id,
                                                 ClassName = (s.Classes_Id == null) ? String.Empty : s.Class.ClassDesc,
                                                 Inactive = (s.Inactive == null) ? false : (bool)s.Inactive,
                                                 LevelId = s.Levels_Id,//(s.Levels_Id == null) ? 0 : (int)s.Levels_Id,
                                                 ReadLevel = (s.Levels_Id == null) ? String.Empty : s.Level.ReadLevel,
                                                 Gender = s.Gender,
                                                 GradeId = (s.GradeId == null) ? 0 : (int)s.GradeId,
                                                 Grade = (s.GradeId == null) ? String.Empty : s.Grade.Name,
                                                 Name = s.FirstName.Trim() + " " + s.LastName
                                             };

            switch (sortOrder)
            {
                case "Gender":
                    if (ascending)
                        students = students.OrderBy(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Grade":
                    if (ascending)
                        students = students.OrderBy(s => s.Grade).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Grade).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
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

        // GET: Students

        public ActionResult SelectClass(int? classId, string className, string message, string search, int? libraryId)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Students/SelectClass" });
                libraryId = user.LibraryId;
            }

            if (!string.IsNullOrEmpty(search))
            {
                return RedirectToAction("FindStudent", new { search = search, libraryId = libraryId });
            }

            if (classId != null)
            {
                return RedirectToAction("MyClass", new { classId = classId, className = className });
            }

            ClassSelection model = new ClassSelection();

            IQueryable<CurrentClass> classList = from c in db.Classes
                                                 where c.Library_Id == libraryId && !c.Obsolete
                                                 orderby c.ClassDesc ascending
                                                 select new CurrentClass()
                                                 {
                                                     Id = c.Classes_Id,
                                                     Name = c.ClassDesc,
                                                     Stage = (c.Stage == null) ? "0" : c.Stage
                                                 };
            model.ClassList = classList;
            model.LibraryId = (int)libraryId;
            model.Message = message;
            model.Search = "";

            return View(model);
        }
        
        public ActionResult FindStudent(string sortOrder, string newOrder, string search, int? studentId, int? libraryId, int? page, bool asc = true)
        {

            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Students/SelectClass" });
                libraryId = user.LibraryId;
            }

            if (studentId != null)
            {
                int? classId = db.Students.Find((int)studentId).Classes_Id;
                if (classId != null)
                    return RedirectToAction("MyClass", new { selectedId = studentId, classId = classId, libraryId = libraryId });
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
            model.StudentHRs = GetSearchList(search, sortOrder, asc, model.LibraryId).ToPagedList(pageNumber, pageSize);
            model.FilterName = "Students names starting '" + search + "'";

            return View("FindStudent", model);

        }

        private IQueryable<StudentHR> GetSearchList(string search, string sortOrder, bool ascending, int libraryId)
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
                                                 //LevelId = (s.Levels_Id == null) ? 0 : (int)s.Levels_Id,
                                                 //ReadLevel = (s.Levels_Id == null) ? String.Empty : s.Level.ReadLevel,
                                                 Gender = s.Gender,
                                                 Name = s.FirstName.Trim() + " " + s.LastName
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

                case "ClassName":
                    if (ascending)
                        students = students.OrderBy(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
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
        // [Authorize(Roles = "Teacher, Supervisor")]
        public ActionResult MyClass(int? classId, int? libraryId, int? selectedId, string sortOrder, string newOrder, bool? asc, string Activity, string className)
        {

            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Students/SelectClass" });
                if (User.IsInRole("Parent helper"))
                    return RedirectToAction("SelectClass", new { message = "Only teachers can access the classroom area." });
                libraryId = user.LibraryId;
            }
            //if(selectedId != null)
            //{
            //    int? thisClass =  db.Students.Find((int)selectedId).Classes_Id;
            //    if (thisClass != null)
            //        classId = (int)thisClass;
            //}
            if (classId == null)
                return RedirectToAction("SelectClass");

            int id = (int)classId;
            bool ascending = (asc == null) ? true : (bool)asc;

            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "FirstName";
            }
            else
            {
                if (sortOrder == newOrder)
                    ascending = !ascending;
                else
                {
                    ascending = true;
                    sortOrder = newOrder;
                }
            }

            MyClass model = new MyClass();
            model.ClassId = id;
            if (String.IsNullOrEmpty(className))
                model.ClassName = db.Classes.Find(id).ClassDesc.Trim();
            else
                model.ClassName = className;
            model.StudentVMs = GetStudentList(id, sortOrder, ascending);
            if (model.StudentVMs.Any())
            {
                if (selectedId == null)
                    model.StudentId = model.StudentVMs.FirstOrDefault().Id;
                else
                    model.StudentId = (int)selectedId;

                model.Student = model.StudentVMs.FirstOrDefault().Name;
                model.GradeId = model.StudentVMs.FirstOrDefault().GradeId;
            }
            if (Activity == null)
                model.Activity = "Select";
            else
                model.Activity = Activity;

            model.SortOrder = sortOrder;
            model.Ascending = ascending;
            model.LibraryId = (int)libraryId;

            //if (model.Activity == "School Reports")
            //{
                IQueryable<ReadyReport> reportList = from g in db.GradeReports
                                                     where g.ReportSchool.Library_Id == model.LibraryId && g.GradeId == model.GradeId && g.Ready
                                                     orderby g.ReportSchool.Status ascending, g.ReportSchool.SemesterId descending
                                                     select new ReadyReport
                                                     {
                                                         Id = g.SchoolReportId,
                                                         ReportName = g.ReportSchool.Semester.Year + " Semester " + g.ReportSchool.Semester.Number.ToString(),   //+ " " + g.Grade.FullName
                                                     };

                model.ReadyReports = reportList;
            //}
            //model.Refresh = false;

            return View(model);
        }

        public ActionResult PleaseSelect()
        {
            NoData noData = new NoData();
            noData.Student = "";
            noData.Heading = "Welcome"; //baf xxx ad the teacher name here
            noData.Message = "Please  click one of the activity buttons above";
            return PartialView("_NoData", noData);
        }

        private int CreateLoan(int studentId, string isbn, ref string message)
        {
            int productId = 0;
            var query = (from p in db.Products
                         where p.Isbn == isbn
                         select p).SingleOrDefault();

            if (query != null)
            {
                productId = query.Product_Id;

                Loan l = new Loan();
                l.Product_Id = productId;
                l.Student_Id = studentId;
                l.BorrowDate = DateTime.Now;
                db.Loans.Add(l);

                try
                {
                    db.SaveChanges();
                    System.Media.SystemSounds.Exclamation.Play();
                    return l.Loans_Id;
                }

                catch (Exception)
                {
                    message = "Failed to write to file.";
                }
            }
            System.Media.SystemSounds.Hand.Play();
            return 0;
        }

        private int CreateLoan(int studentId, int productId, ref string message)
        {
            var count = (from loan in db.Loans
                         where loan.Product_Id == productId && loan.Student_Id == studentId && !loan.ReturnDate.HasValue
                         select loan).Count();

            if (count > 0)
            {
                message = "This student already has this book on loan.";
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
                    return l.Loans_Id;
                }

                catch (Exception)
                {
                    message = "Failed to write to file.";
                }
            }
            System.Media.SystemSounds.Hand.Play();
            return 0;
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

        public JsonResult BookSelection(int studentId, int productId)
        {
            string message = "";
            int loanId = 0;

            //check if on loan or just borrowed
            var query = (from l in db.Loans
                         where l.Student_Id == studentId && l.Product_Id == productId && (!l.ReturnDate.HasValue
                         || System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now))
                         select l).FirstOrDefault();
            if (query != null && query.Loans_Id > 0)
            {
                if (!query.ReturnDate.HasValue)
                {
                    if (query.BorrowDate.Date != DateTime.Now.Date)
                    {
                        query.ReturnDate = DateTime.Now;
                        loanId = query.Loans_Id;
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception)
                        {
                            message = "Failed to write to file. Please try again";
                        }
                    }
                    else
                    {
                        message = "This student already has this book on loan.";
                    }
                }
                else
                    message = "You have already processed this loan";
            }
            else
                loanId = CreateLoan(studentId, productId, ref message);

            return Json(new { Success = true, Result = message }, JsonRequestBehavior.AllowGet);
            // return PartialView("_StudentLoans", model);
            // return RedirectToAction("ShowCurrentLoansView", new { studentId = studentId, message = message });
        }

        public ActionResult BarcodeScan(int studentId, string barcode, int libraryId)
        {

            if (!String.IsNullOrEmpty(barcode))
            {
                string message = "";
                int loanId = 0;
                barcode = barcode.Trim();
                string isbn = ProductClass.CheckCheckDigit(barcode);
                if (!string.IsNullOrEmpty(isbn))
                {
                    //check if on loan or just borrowed
                    var query = (from l in db.Loans
                                 where l.Student_Id == studentId && l.Product.Isbn == isbn && (!l.ReturnDate.HasValue || System.Data.Entity.DbFunctions.TruncateTime(l.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(DateTime.Now))

                                 select l).FirstOrDefault();
                    if (query != null && query.Loans_Id > 0)
                    {
                        if (!query.ReturnDate.HasValue)
                        {
                            if (query.BorrowDate.Date != DateTime.Now.Date)
                            {
                                query.ReturnDate = DateTime.Now;
                                loanId = query.Loans_Id;
                                try
                                {
                                    db.SaveChanges();
                                    return Json(new { Success = true, Result = loanId }, JsonRequestBehavior.AllowGet);
                                }

                                catch (Exception)
                                {
                                    message = "Failed to write to file. Please try again";
                                    //return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
                                }
                            }
                            else
                            {
                                message = "This student already has this book on loan.";
                            }
                        }
                        else
                            message = "You have already processed this loan";
                    }
                    else
                    {
                        if (IsbnInDatabase(isbn) || GoogleBooksSearch(isbn, libraryId))
                        {
                            loanId = CreateLoan(studentId, isbn, ref message);
                            if (loanId > 0)
                                return Json(new { Success = true, Result = 0 }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //System.Media.SystemSounds.Hand.Play(); 
                            message = "The title is not in the database and could not be added using Google.";
                        }
                    }
                }
                else if (db.Products.Any(x => x.Title.Contains(barcode))) //its a title search
                {
                    return Json(new { Success = true, Result = -1 }, JsonRequestBehavior.AllowGet);
                }
                if (!String.IsNullOrEmpty(message))
                {
                    return Json(new { Success = true, Result = message }, JsonRequestBehavior.AllowGet);
                }
            }
            System.Media.SystemSounds.Hand.Play();
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        private IQueryable<ProductSearch> GetBookSearch(string search)
        {
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

            return productList;
        }

        public ActionResult BookSearch(int studentId, string search, int? page)
        {
            if (search == null || studentId == 0)
            {
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public JsonResult ReturnLoan(int id, bool isCancel)
        {
            var query = (from l in db.Loans
                         where l.Loans_Id == id
                         select l).FirstOrDefault();
            if (query != null)
            {
                if (isCancel)
                    db.Loans.Remove(query);
                else
                    query.ReturnDate = DateTime.Now;
                try
                {
                    db.SaveChanges();
                    // IQueryable<OnLoan> model = GetCurrentLoans(query.Student_Id);
                    // return PartialView("_StudentLoans", model);
                    System.Media.SystemSounds.Asterisk.Play();
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }

                catch (Exception)
                {
                    //CusValRight.IsValid = false;
                    //CusValRight.ErrorMessage = "Failed to write to file.";
                }
            }
            System.Media.SystemSounds.Hand.Play();
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Summary(int classId, string sortOrder, string newOrder, string className, bool asc)
        {
            DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            DateTime overdueDate = DateTime.Now.AddDays(-7);
            SummaryList model = new SummaryList();
            IQueryable<StudentSummary> students = from s in db.Students.Where(s => s.Classes_Id == classId && s.Inactive == false)

                                                  join rs in db.ReportStudents.Where(r => r.ReportSchool.Status == 1) on s.Student_Id equals rs.StudentId into rsc
                                                  from y in rsc.DefaultIfEmpty()

                                                  join l2 in
                                                      (from l in db.Loans.Where(d => d.BorrowDate >= startDate && d.Student.Classes_Id == classId)
                                                       group l by l.Student_Id into grp
                                                       let count = grp.Count()
                                                       select new { Student_Id = grp.Key, count }) on s.Student_Id equals l2.Student_Id into l3
                                                  from z in l3.DefaultIfEmpty()

                                                  join l4 in
                                                      (from l in db.Loans.Where(d => d.ReturnDate == null && d.Student.Classes_Id == classId && d.BorrowDate < overdueDate)
                                                       group l by l.Student_Id into grp
                                                       let maxDate = grp.OrderByDescending(g => g.BorrowDate).FirstOrDefault().BorrowDate
                                                       let count = grp.Count()
                                                       select new { Student_Id = grp.Key, maxDate, count }) on s.Student_Id equals l4.Student_Id into l5
                                                  from z2 in l5.DefaultIfEmpty()

                                                  join l6 in
                                                      (from l in db.Results.Where(r => (r.Subject.IsTopic && r.MarksId == null && r.Subject.Subject1.ReportType != 4)
                                                                        || (r.Subject.ParentId == null && r.Subject.ReportType == 1 && (r.MarksId == null || r.EffortId == null))
                                                                        || (!r.Subject.IsTopic && r.Subject.ParentId != null && r.Subject.Subject1.ReportType == 2 && r.EffortId == null))
                                                       group l by l.ReportStudent.StudentId into grp
                                                       let count = grp.Count()
                                                       select new { Student_Id = grp.Key, count }) on s.Student_Id equals l6.Student_Id into l7
                                                  from z3 in l7.DefaultIfEmpty()

                                                  select new StudentSummary()
                                                  {
                                                      Id = s.Student_Id,
                                                      FirstName = s.FirstName,
                                                      LastName = s.LastName,
                                                      ReadLevel = (s.Levels_Id == null) ? String.Empty : s.Level.ReadLevel,
                                                      GradeId = ((int?)s.GradeId ?? 0),
                                                      Gender = s.Gender,
                                                      Grade = (s.GradeId == null) ? String.Empty : s.Grade.Name,
                                                      BooksRead = ((int?)z.count ?? 0),
                                                      OnLoan = ((int?)z2.count ?? 0),
                                                      
                                                      //Overdue =  (z2.maxDate == null) ? "" : ((DateTime)z2.maxDate).ToString("dd/MM/yyyy"),
                                                      Overdue = (z2.maxDate == null) ? null : (DateTime?)z2.maxDate,//(z2.maxDate == null) ? null : (DateTime)z2.maxDate,// z2.maxDate ,////== null) ? "" : z2.maxDate).ToShortDateString(),
                                                      ReportStatus = (y.Status == null) ? 0 : (int)y.Status,
                                                      AllTicked = (z3.count == null || ((int)z3.count == 0)) ? (s.ReportStudents.Any() ? 3 : 1) : 2
                                                  };


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
                    asc = true;
                    sortOrder = newOrder;
                }
            }

            switch (sortOrder)
            {
                case "Gender":
                    if (asc)
                        students = students.OrderBy(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Gender).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Grade":
                    if (asc)
                        students = students.OrderBy(s => s.GradeId).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.GradeId).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Level":
                    if (asc)
                        students = students.OrderBy(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Read":
                    if (asc)
                        students = students.OrderBy(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                default:
                    if (asc)
                        students = students.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }
            model.Summaries = students;
            model.Ascending = asc;
            model.ClassId = classId;
            model.ClassName = className;
            model.SortOrder = sortOrder;
          
            return PartialView("_Summary", model);
        }

        public ActionResult HomeReading(int studentId)//, bool? showCurrent
        {
            if (studentId == 0)
            {
                return Content(""); //baf temp
            }
            string student = "";
            var temp = db.Students.Where(m => m.Student_Id == studentId).FirstOrDefault();
            if (temp != null)
                student = temp.FirstName.Trim() + " " + temp.LastName.Trim();

            StudentLoans model = new StudentLoans();
            model.StudentId = studentId;
            model.Student = student;
            //model.ShowCurrent = showCurrent ?? true;
            model.Loans = GetCurrentLoans(studentId);//GetLoansHistory(studentId);

            return PartialView("_HomeReading", model);
        }

        public ActionResult ShowCurrentLoansView(int studentId)
        {
            IQueryable<OnLoan> model = GetCurrentLoans(studentId);
            return PartialView("_StudentLoans", model);
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

        private IQueryable<OnLoan> GetLoansHistory(int studentId)
        {
            DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            IQueryable<OnLoan> loans = from l in db.Loans
                                       where l.Student_Id == studentId && l.BorrowDate >= startDate
                                       orderby l.ReturnDate ascending, l.BorrowDate ascending
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

        public ActionResult SchoolReports(int gradeId, int studentId, int libraryId, int? schoolReportId)//, string className, int studentId, string student)
        {
            if (studentId == 0 || gradeId == 0)
            {
                return Content(""); //baf temp
            }
            string student = "";
            var temp = db.Students.Where(m => m.Student_Id == studentId).FirstOrDefault();
            if (temp != null)
                student = temp.FirstName.Trim() + " " + temp.LastName.Trim();

            int schRepId = 0;
            if (schoolReportId == null || schoolReportId == 0)
            {
                var schoolReport = (from g in db.GradeReports
                              where g.ReportSchool.Library_Id == libraryId && g.GradeId == gradeId && g.Ready
                              orderby g.ReportSchool.Status ascending, g.ReportSchool.SemesterId descending
                              select g).FirstOrDefault();

                if (schoolReport == null)
                {
                    NoData noData = new NoData();
                    noData.Student = student;
                    noData.Heading = "School Reports";
                    noData.Message = "No reports available for this student";
                    return PartialView("_NoData", noData);
                }
                else
                    schRepId = (int)schoolReport.SchoolReportId;
            }
            else
                schRepId = (int)schoolReportId;

            StudentReport model = new StudentReport();

            var report = (from g in db.GradeReports
                          where g.SchoolReportId == schRepId && g.GradeId == gradeId && g.Ready
                         // where g.ReportSchool.Library_Id == libraryId && g.GradeId == gradeId && g.Ready
                          //orderby g.ReportSchool.Status ascending, g.ReportSchool.SemesterId descending
                          select g).FirstOrDefault();

            if (report == null)
            {
                NoData noData = new NoData();
                noData.Student = student;
                noData.Heading = "School Reports";
                noData.Message = "No reports available for this student";
                return PartialView("_NoData", noData);
            }
            model.LibraryId = libraryId;
            model.SchoolReportId = report.SchoolReportId;
            model.GradeReportId = report.Id;
            model.StudentId = studentId;
            model.Student = student;
            model.ReportName = "School report:  " + report.ReportSchool.Semester.Year + " Semester " + report.ReportSchool.Semester.Number.ToString() + " " + report.Grade.FullName;
            //IQueryable<ReadyReport> reportList = from g in db.GradeReports
            //                                     where g.ReportSchool.Library_Id == libraryId && g.GradeId == gradeId && g.Ready
            //                                     orderby g.ReportSchool.Status ascending, g.ReportSchool.SemesterId descending
            //                                     select new ReadyReport
            //                                     {
            //                                         Id = g.SchoolReportId,
            //                                         ReportName = g.ReportSchool.Semester.Year + " Semester " + g.ReportSchool.Semester.Number.ToString() + " " + g.Grade.FullName,
            //                                     };

            //model.ReadyReports = reportList;

            int studentReportId = 0;

            if (!db.ReportStudents.Any(x => x.StudentId.Equals(studentId) && x.SchoolReportId.Equals(model.SchoolReportId)))
                studentReportId = CreateStudentReport(model.GradeReportId, model.SchoolReportId, studentId);
            else
                studentReportId = db.ReportStudents.Where(x => x.StudentId.Equals(studentId) && x.SchoolReportId.Equals(model.SchoolReportId)).FirstOrDefault().Id;

            if (studentReportId == 0)
                return Content(""); //baf temp

            model.StudentReportId = studentReportId;
            IQueryable<ResultVM> results = from s in db.Subjects.Where(x => x.GradeReportId == model.GradeReportId)
                                           join r in db.Results.Where(x => x.StudentReportId == studentReportId) on s.Id equals r.SubjectId into rl
                                           from _r in rl.DefaultIfEmpty()
                                           // where _r == null ? true : _r.StudentReportId == studentReportId && s.GradeReportId == model.GradeReportId
                                           select new ResultVM()
                                           {
                                               SubjectId = s.Id,
                                               Subject = s.Name,
                                               ColOrder = s.ColOrder,
                                               ParentId = (s.ParentId == null) ? 0 : (int)s.ParentId,
                                               ReportType = s.ReportType,
                                               IsTopic = s.IsTopic,
                                               ResultsId = (_r.Id == null) ? 0 : _r.Id,
                                               AssessListId = (s.ParentId == null) ? s.AssessmentId : 0,
                                               EffortListId = (s.ParentId == null) ? s.EffortId : 0,
                                               MarksId = (_r.MarksId == null) ? 0 : (int)_r.MarksId,
                                               EffortId = (_r.EffortId == null) ? 0 : (int)_r.EffortId,
                                               ParentType = (s.ParentId == null) ? 0 : (int)s.Subject1.ReportType,
                                               KlaComments = s.KlaComments,
                                               Comments = _r.Comments
                                           };
            if (results != null)
            {
                //model.ResultVMs = results;
                model.Klas = results.Where(m => m.ParentId == 0).OrderBy(m => m.ColOrder);
                model.Substrands = results.Where(m => m.ParentId != 0 && !m.IsTopic && m.ParentType == 1);
                model.Substrands2 = results.Where(m => m.ParentId != 0 && !m.IsTopic && m.ParentType == 2);
                model.Indicators = results.Where(m => m.IsTopic);
                int listId1 = results.Where(m => m.AssessListId > 1 && (m.ReportType == 1 || m.ReportType == 2)).FirstOrDefault().AssessListId;
                int listId2 = results.Where(m => m.EffortListId > 1).FirstOrDefault().EffortListId;
                int listId3 = results.Where(m => m.AssessListId > 1 && m.ReportType == 3).FirstOrDefault().AssessListId;
                int listId4 = results.Where(m => m.AssessListId > 1 && m.ReportType == 4).FirstOrDefault().AssessListId;
                IQueryable<RepMark> assess = from m in db.Marks
                                             where m.AssessmentId == listId1 || m.AssessmentId == listId2 || m.AssessmentId == listId3 || m.AssessmentId == listId4
                                             orderby m.ColOrder
                                             select new RepMark()
                                             {
                                                 Id = m.Id,
                                                 Name = m.Name,
                                                 AssessmentId = m.AssessmentId
                                             };
                model.MarksList = assess;
            }

            var query = (from s in db.ReportStudents
                         where s.StudentId == studentId && s.SchoolReportId == model.SchoolReportId
                         select s).FirstOrDefault();
            if (query != null && query.Id > 0)
            {
                model.CommentHeader = query.ReportSchool.CommentHeader;
                model.Comments = query.Comments;
                model.AbsentDate = (DateTime)query.AbsentDate;
                model.AbsentFull = query.AbsentFull;
                model.AbsentPart = query.AbsentPart;
                model.Teacher = query.Teacher;
                model.Teacher2 = query.Teacher2;
                if (query.Status == 3)
                    model.ApprovedBy = "Approved by: " + query.Teacher1.FirstName.Trim() + " " + query.Teacher1.LastName.Trim();
                else
                    model.ApprovedBy = "Not approved";

                model.Status = query.Status;
            }

            return PartialView("_SchoolReports", model);
        }

        private int CreateStudentReport(int GradeReportId, int schoolReportId, int studentId)
        {
            int studentReportId = 0;

            var query = (from s in db.Students
                         where s.Student_Id == studentId
                         select s).FirstOrDefault();

            ReportStudent rs = new ReportStudent();
            rs.SchoolReportId = schoolReportId;
            rs.StudentId = studentId;
            rs.AbsentPart = 0;
            rs.AbsentFull = 0;
            rs.AbsentDate = DateTime.Now;
            rs.Teacher = (query.Class.Teacher_Id == null) ? "" : query.Class.Teacher.FirstName.Trim() + " " + query.Class.Teacher.LastName.Trim();
            rs.Teacher2 = (query.Class.Teacher2Id == null) ? "" : query.Class.Teacher1.FirstName.Trim() + " " + query.Class.Teacher1.LastName.Trim();
            rs.Status = 1;
            db.ReportStudents.Add(rs);
            db.SaveChanges();
            studentReportId = rs.Id;

            var subjects = from s in db.Subjects
                           where s.GradeReportId == GradeReportId
                           select s;

            foreach (var sub in subjects)
            {
                if (sub.ParentId == null && sub.ReportType > 2 || sub.ParentId != null && !sub.IsTopic && sub.Subject1.ReportType != 2)
                {
                    //skip it
                }
                else
                {
                    Result r = new Result();
                    r.StudentReportId = studentReportId;
                    r.SubjectId = sub.Id;
                    db.Results.Add(r);
                }
            }
            db.SaveChanges();

            return studentReportId;
        }

        public ActionResult OrganiseClass(int classId, string sortOrder, string newOrder, string className, bool asc, int libraryId, bool editLevels = false, bool editClasses = false)
        {
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
                    asc = true;
                    sortOrder = newOrder;
                }
            }
            MyClass model = new MyClass();
            model.StudentVMs = GetStudentList(classId, sortOrder, asc);

            model.ClassId = classId;
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.ClassName = className;
            model.LibraryId = libraryId;
            if (editLevels)
                model.EditHeader = "Set level";
            else if (editClasses)
                model.EditHeader = "Set class";
            IQueryable<ClassItem> classes = from c in db.Classes
                                            where c.Library_Id == libraryId && !c.Obsolete
                                            orderby c.ClassDesc
                                            select new ClassItem()
                                            {
                                                Id = c.Classes_Id,
                                                ClassName = c.ClassDesc
                                            };
            model.Classes = classes;

            IQueryable<ReadLevel> levels = from l in db.Levels
                                           where l.Obsolete != true
                                           orderby l.ReadLevel
                                           select new ReadLevel
                                           {
                                               Id = l.Levels_Id,
                                               Level = l.ReadLevel
                                           };
            model.Levels = levels;
            model.EditLevels = editLevels;
            model.EditClasses = editClasses;
            
            return PartialView("_Organise", model);
        }

        public JsonResult SetReadingLevel(int id, int levelId)
        {

            if (levelId > 0 && levelId < 32 && id > 0)
            {
                var studentToUpdate = db.Students.Find(id);
                try
                {
                    studentToUpdate.Levels_Id = levelId;
                    db.SaveChanges();
                    return Json(new { Success = true, Result = id }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AssignToClass(int id, int? classId)
        {
            if (id > 0)
            {
                var studentToUpdate = db.Students.Find(id);
                try
                {
                    if (classId == null)
                        studentToUpdate.Classes_Id = null;
                    else
                        studentToUpdate.Classes_Id = (int)classId;
                    db.SaveChanges();
                    return Json(new { Success = true, Result = id }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveKlaComments(int id, string comments)
        {
            if (id > 0)
            {
                var record = db.Results.Find(id);
                if (record != null)
                {
                    try
                    {
                        record.Comments = comments.Trim();
                        db.SaveChanges();
                        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                    }
                    catch (DataException)
                    {
                        //Log the error (uncomment dex variable name and add a line here to write a log.
                        //ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                        return Json(new { Success = false, Result = "Unable to save changes. Try again, and if the problem persists, see your system administrator." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            return Json(new { Success = false, Result = "Invalid id. Try again, and if the problem persists, see your system administrator." }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveComments(int id, string comments, int fullDays, int partDays, string absentDate, string teacher, string teacher2)
        {
            if (id > 0)
            {
                var record = db.ReportStudents.Find(id);
                if (record != null)
                {
                    try
                    {
                        record.Comments = comments.Trim();
                        record.AbsentFull = fullDays;
                        record.AbsentPart = partDays;
                        record.Teacher = teacher.Trim();
                        record.Teacher2 = teacher2.Trim();
                        //absentDate = "31/03/2015";
                        DateTime dateValue;
                        if (DateTime.TryParse(absentDate, out dateValue) == true)
                        {
                            record.AbsentDate = dateValue;
                        }
                        else
                        {
                            record.AbsentDate = DateTime.Now;
                        }
                        db.SaveChanges();
                        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                    }
                    catch (DataException)
                    {
                        //Log the error (uncomment dex variable name and add a line here to write a log.
                        //ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                        return Json(new { Success = false, Result = "Unable to save changes. Try again, and if the problem persists, see your system administrator." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            return Json(new { Success = false, Result = "Invalid id. Try again, and if the problem persists, see your system administrator." }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //[Authorize(Roles = "Supervisor")]
        public JsonResult ApproveReport(int id, bool cancel)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);

            if (!User.IsInRole("Supervisor"))
                return Json(new { Success = false, Result = "You are not authorised to approve reports" }, JsonRequestBehavior.AllowGet);

            int userId = user.TeacherId;
            var query = (from t in db.Teachers
                         where t.Teacher_Id == userId
                         select t).FirstOrDefault();



            if (id > 0 && query != null)
            {
                string userName = query.FirstName.Trim() + " " + query.LastName.Trim();

                var record = db.ReportStudents.Find(id);
                if (record != null)
                {
                    try
                    {
                        if (cancel)
                        {
                            record.ApprovedBy = null;
                            record.Status = 1;
                        }
                        else
                        {
                            record.ApprovedBy = userId;
                            record.Status = 3;
                        }
                        db.SaveChanges();
                        return Json(new { Success = true, Result = userName }, JsonRequestBehavior.AllowGet);
                    }
                    catch (DataException)
                    {
                        //Log the error (uncomment dex variable name and add a line here to write a log.
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    }
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetMark(int resultId, int markId)
        {
            if (resultId > 0)
            {
                try
                {

                    var resultToUpdate = db.Results.Find(resultId);
                    int oldMarkId = resultToUpdate.MarksId == null ? 0 : (int)resultToUpdate.MarksId;
                    if (oldMarkId == markId)
                    {
                        resultToUpdate.MarksId = null;
                        oldMarkId = -1;
                    }
                    else
                        resultToUpdate.MarksId = markId;

                    db.SaveChanges();
                    return Json(new { Success = true, Result = oldMarkId.ToString() }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SetEffort(int resultId, int markId)
        {
            string oldMarkId;
            if (resultId > 0)
            {
                var resultToUpdate = db.Results.Find(resultId);
                try
                {
                    oldMarkId = resultToUpdate.EffortId == null ? "0" : resultToUpdate.EffortId.ToString();
                    resultToUpdate.EffortId = (int)markId;
                    db.SaveChanges();
                    return Json(new { Success = true, Result = oldMarkId }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ToggleMark(int resultId)
        {
            int markId = 0;
            string showTick = "0";
            if (resultId > 0)
            {
                var resultToUpdate = db.Results.Find(resultId);
                try
                {
                    if (resultToUpdate.MarksId == null)
                    {
                        markId = resultToUpdate.Subject.Subject1.Assessment.Marks.FirstOrDefault().Id;
                        resultToUpdate.MarksId = markId;
                        showTick = "1";
                    }
                    else
                    {
                        resultToUpdate.MarksId = null;
                    }
                    db.SaveChanges();
                    return Json(new { Success = true, Result = showTick }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }
        // GET: Students/Details/5

        public static float TotalRowHeights(Document document, PdfContentByte content, PdfPTable table)
        {
            float height = 0f;
            ColumnText ct = new ColumnText(content);
            // respect current Document.PageSize    
            ct.SetSimpleColumn(
              document.Left, document.Bottom,
              document.Right, document.Top
            );
            ct.AddElement(table);
            // **simulate** adding the PdfPTable to calculate total height
            ct.Go(true);
            for (int i = 0; i < table.Rows.Count; i++)
            {
                height += table.GetRowHeight(i);
            }

            return height;
        }

        private void GetNewSpacing(ref float beforePara, ref float spacing, float spare, int tableCount, int klaCount)
        {
            //if (spare >= 9f)
            //    spare -= 9f;
            float total = tableCount * 2 + (klaCount - 1) * 6 + 1;
            if (total != 0)
            {
                beforePara = Math.Min(spare / total * 3, 40);
                spacing = Math.Min(spare / total, 20);
            }
        }

        public ActionResult PrintSchoolReport(int? studId, int? classes_Id, int? schoolRepId, int libraryId)
        {
            // CreatePDF();
            if ((studId == null && classes_Id == null))//|| schoolReportId <= 0)
                return Content(""); //new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var school = (from l in db.Libraries
                          where l.Library_Id == libraryId
                          select l).FirstOrDefault();

            if (school == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            string schoolName = (school.Licensee == null) ? "" : school.Licensee.Trim();
            string address1 = (school.LicAdd1 == null) ? "" : school.LicAdd1.Trim();
            string address2 = (school.LicAdd2 == null) ? "" : school.LicAdd2.Trim();
            string town = (school.LicTown == null ? "" : school.LicTown.Trim()) +
                (school.LicPostCode == null ? "" : (school.LicPostCode.Substring(1, 0) == "2" ? " NSW " : " ") + school.LicPostCode.Trim());
            string phone = (school.LicPhone == null) ? "" : school.LicPhone.Trim();
            string email = (school.LicEmail == null) ? "" : school.LicEmail.Trim();

            int studentId = (studId == null) ? 0 : (int)studId;
            int classId = (classes_Id == null) ? 0 : (int)classes_Id;
            int schoolReportId = (schoolRepId == null) ? 0 : (int)schoolRepId;

            var report = (from s in db.ReportSchools
                          where (schoolReportId > 0) ? (s.Id == schoolReportId) : (s.Library_Id == libraryId && s.Status == 1)
                          select s).FirstOrDefault();

            if (report == null)
                return Content("");
            else
                schoolReportId = report.Id;
            string semester = "Semester " + report.Semester.Number + " " + report.Semester.Year;
            string grade = "";
            int semesterId = report.SemesterId;
            int gradeReportId = 0;
            int studentReportId = 0;
            int gradeId = 0;

            IQueryable<AssessmentKey> key = from aa in db.Marks
                                            where aa.AssessmentId == 2  //baf temp not pretty
                                            orderby aa.ColOrder descending
                                            select new AssessmentKey()
                                            {
                                                Mark = aa.Name,
                                                Description = aa.Description
                                            };


            Document doc = new Document();
            MemoryStream stream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(doc, stream);
            var footer = new Footer();
            footer.school = schoolName;
            footer.semester = semester;
            writer.PageEvent = footer;

            var path = System.Web.Hosting.HostingEnvironment.MapPath("~/Images");
            iTextSharp.text.Image imageLogo = iTextSharp.text.Image.GetInstance(path + "/DeptEd.jpg");
            imageLogo.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_LEFT;
            imageLogo.ScalePercent(20f);
            imageLogo.SetAbsolutePosition(30f, 5f);

            byte[] buffer = null;
            var crestImage = (from j in db.LibImages
                              where j.Library_Id == libraryId
                              select new
                              {
                                  j.Image_Id,
                                  j.Image
                              }).FirstOrDefault();

            iTextSharp.text.Image crest = null;
            iTextSharp.text.Image watermark = null;

            if (crestImage.Image_Id > 0)
            {
                buffer = (byte[])crestImage.Image;
                crest = iTextSharp.text.Image.GetInstance(buffer);
                crest.ScaleToFit(100f, 120f);//83f, 100f);
                crest.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_LEFT;
                crest.SetAbsolutePosition(60f, doc.PageSize.Height - 130f);//60f, doc.PageSize.Height - 100f);
                if (report.Watermark)
                {
                    watermark = iTextSharp.text.Image.GetInstance(buffer);
                    watermark.ScaleToFit(250f, 250f * watermark.Height / watermark.Width);
                    watermark.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_RIGHT;
                }
            }

            try
            {
                doc.Open();
                //doc.SetMargins(doc.LeftMargin, doc.RightMargin, doc.TopMargin - 20f, doc.BottomMargin - 10f);
                Font times10 = FontFactory.GetFont("Times Roman");
                times10.Size = 10;
                times10.SetStyle("Italic");
                Font zapfdingbats = new Font(Font.FontFamily.ZAPFDINGBATS);
                var tick = new Chunk("\u0033", zapfdingbats);
                Font times14 = FontFactory.GetFont("Times Roman", 14, Font.BOLD);
                Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                Font times10B = FontFactory.GetFont("Times Roman", 10, Font.BOLD);
                Font times9I = FontFactory.GetFont("Times Roman", 9, Font.ITALIC);
                Font times9 = FontFactory.GetFont("Times Roman", 9, Font.NORMAL);

                float[] widths1 = new float[] { 1f };
                float[] widths2 = new float[] { 1f, 4f };
                float[] widths3 = new float[] { 1f, 1f, 1f };
                float[] widths6A = new float[] { 2f, 1f, 1f, 1f, 1f, 1f };
                float[] widths6B = new float[] { 20f, 1f, 1f, 1f, 1f, 1f };
                float[] widths5 = new float[] { 20f, 1f, 1f, 1f, 1f };// 16f, 1f, 1f, 1f, 1f //baf 240415
                float[] widths8 = new float[] { 8f, 1f, 8f, 1f, 8f, 1f };
                int count = 1;

                var students = (from s in db.ReportStudents
                                //where ((studentId == 0) ? true : s.StudentId == studentId) && ((classId == 0) ? true : s.Student.Classes_Id == classId )
                                // where ((studentId == 0) ? s.Student.Classes_Id == classId && s.Student.GradeId != null : s.StudentId == studentId)
                                where ((studentId == 0 && s.Student.Classes_Id == classId && s.Student.GradeId != null) || (studentId > 0 && s.StudentId == studentId)) && s.SchoolReportId == schoolReportId //baf xxx resurrect for print class
                                // where s.StudentId == studentId
                                select s).ToList();

                if (students == null)
                    return Content("");

                foreach (var c in students)//baf xxx resurrect for print class
                {

                    studentId = c.StudentId;//baf xxx resurrect for print class

                    // footer.student = c.Student.FirstName.Trim() + " " + c.Student.LastName.Trim() + " " + c.Student.Class.ClassDesc.Trim();
                    gradeId = (int)c.Student.GradeId;
                    gradeReportId = db.GradeReports.Where(m => m.SchoolReportId == schoolReportId && m.GradeId == gradeId).FirstOrDefault().Id; //baf xxx can we improve this - parameter for single student?
                    studentReportId = db.ReportStudents.Where(m => m.StudentId == studentId && m.SchoolReportId == schoolReportId).FirstOrDefault().Id;//baf xxx can we improve this
                    grade = db.GradeReports.Where(m => m.Id == gradeReportId).FirstOrDefault().Grade.Name;
                    if (count > 1)
                    {
                        // if (count == 2) 
                        footer.NewStudentH = true;
                        doc.NewPage();
                        // if (count == 2) 
                        footer.NewStudent = true;
                    }

                    if (crestImage.Image_Id > 0)
                        doc.Add(crest);

                    if (report.DeptLogo)
                    {
                        doc.Add(imageLogo);
                    }

                    if (report.Watermark)
                    {
                        float pageWidth = doc.PageSize.Width;
                        watermark.SetAbsolutePosition(pageWidth / 2 - 125f, 250f);
                        PdfContentByte cb = writer.DirectContentUnder;
                        PdfGState graphicsState = new PdfGState();
                        graphicsState.FillOpacity = 0.15F;
                        cb.SetGState(graphicsState);
                        cb.AddImage(watermark);
                    }


                    Paragraph para = new Paragraph();

                    para.SpacingAfter = 0f;
                    para.Font = times14;
                    para.Alignment = Element.ALIGN_RIGHT;
                    para.Add(schoolName);
                    //para.ExtraParagraphSpace = 6;
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 0f;
                    para.SpacingAfter = 0f;
                    para.Font = times9;
                    para.Add(address1.Trim() + " " + town.Trim());
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 0f;
                    para.SpacingAfter = 0f;
                    para.Add("Phone: " + phone);
                    doc.Add(para);

                    para.Clear();
                    para.Font = times9I;
                    para.Add("Email: " + email + Environment.NewLine);
                    doc.Add(para);

                    //need spaces here
                    para.Clear();
                    para.SpacingBefore = 25f;
                    para.Font = times14;
                    para.Alignment = Element.ALIGN_CENTER;
                    para.Add(c.Student.FirstName.Trim() + " " + c.Student.LastName.Trim());
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 2f;
                    para.Font = times10B;
                    para.Add("Year: " + grade + "   Class: " + c.Student.Class.ClassDesc.Trim());
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 5f;
                    para.Add(semester);
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 5f;
                    para.Font = times10;
                    para.Alignment = Element.ALIGN_JUSTIFIED;//.ALIGN_LEFT; //baf 140715
                    para.Add(report.Introduction);
                    doc.Add(para);

                    para.Clear();
                    para.SpacingBefore = 5f;
                    para.Font = times10B;
                    para.Alignment = Element.ALIGN_CENTER;
                    para.Add("Assessment of Achievement");
                    doc.Add(para);


                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable table1 = new PdfPTable(2);
                    table1.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                    table1.SetWidths(widths2);
                    table1.LockedWidth = true;
                    table1.HorizontalAlignment = 1;
                    table1.SpacingAfter = 5f;
                    table1.SpacingBefore = 3f;


                    PdfPCell cellE = new PdfPCell();
                    cellE.FixedHeight = 41f;
                    cellE.VerticalAlignment = 1;
                    cellE.Border = Rectangle.BOX;
                    cellE.HorizontalAlignment = 0;
                    cellE.Indent = 1;
                    cellE.SetLeading(0f, 1.2f);
                    cellE.UseBorderPadding = true;
                    foreach (var k in key)
                    {
                        cellE.HorizontalAlignment = 0; //baf 140715
                        cellE.Phrase = new Phrase(k.Mark, times10);
                        table1.AddCell(cellE);

                        cellE.HorizontalAlignment = Element.ALIGN_JUSTIFIED;  //baf 140715
                        cellE.Phrase = new Phrase(k.Description, times10);
                        table1.AddCell(cellE);
                    }
                    doc.Add(table1);

                    PdfPTable table2 = new PdfPTable(3);
                    table2.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                    table2.SetWidths(widths3);
                    table2.LockedWidth = true;
                    table2.HorizontalAlignment = 1;
                    table2.SpacingAfter = 1f;
                    table2.SpacingBefore = 5f;

                    PdfPCell cell = new PdfPCell();
                    cell.Border = 0;
                    cell.Indent = 1;
                    cell.Colspan = 3;
                    cell.FixedHeight = 15f;
                    cell.HorizontalAlignment = 1;
                    cell.Phrase = new Phrase("Absences", times10B);
                    table2.AddCell(cell);

                    cell.Colspan = 1;
                    cell.FixedHeight = 30f;
                    cell.Phrase = new Phrase("Days absent as at " + ((DateTime)c.AbsentDate).ToString("dd/MM/yyyy"), times10);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase("Full days: " + ((c.AbsentFull == 0) ? "0" : c.AbsentFull.ToString()), times10);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase("Partial days: " + ((c.AbsentPart == 0) ? "0" : c.AbsentPart.ToString()), times10);
                    table2.AddCell(cell);

                    cell.Colspan = 3;
                    cell.HorizontalAlignment = 1;
                    cell.FixedHeight = 18f;
                    cell.Phrase = new Phrase(report.CommentHeader, times10B);
                    table2.AddCell(cell);

                    PdfPCell cellC = new PdfPCell();
                    cellC.Border = 0;
                    cellC.Indent = 1;
                    cellC.Colspan = 3;
                    cellC.HorizontalAlignment = Element.ALIGN_JUSTIFIED; //0; //baf 140715
                    cellC.FixedHeight = 235f;//260f
                    cellC.Phrase = new Phrase(c.Comments, times10);
                    cellC.SetLeading(1f, 1.4f);
                    table2.AddCell(cellC);


                    cell.Colspan = 1;
                    cell.FixedHeight = 0f;
                    cell.HorizontalAlignment = 1;
                    cell.Phrase = new Phrase(c.Teacher.Trim(), times10);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase(c.Teacher2.Trim(), times10);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase(report.Principal.Trim(), times10);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase("Teacher", times9I);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase(String.IsNullOrEmpty(c.Teacher2.Trim()) ? "" : "Teacher", times9I);
                    table2.AddCell(cell);

                    cell.Phrase = new Phrase(report.Position.Trim(), times9I);
                    table2.AddCell(cell);

                    doc.Add(table2);

                    // doc.NewPage();

                    IQueryable<ResultsPrint> results = from s in db.Subjects.Where(x => x.GradeReportId == gradeReportId)
                                                       join r in db.Results.Where(x => x.StudentReportId == studentReportId) on s.Id equals r.SubjectId into rl
                                                       from _r in rl.DefaultIfEmpty()
                                                       // where _r == null ? true : _r.StudentReportId == studentReportId && s.GradeReportId == model.GradeReportId
                                                       select new ResultsPrint()
                                                       {
                                                           SubjectId = s.Id,
                                                           Subject = s.Name,
                                                           ColOrder = s.ColOrder,
                                                           ParentId = (s.ParentId == null) ? 0 : (int)s.ParentId,
                                                           ReportType = s.ReportType,
                                                           IsTopic = s.IsTopic,
                                                           ResultsId = (_r.Id == null) ? 0 : _r.Id,
                                                           AssessListId = (s.ParentId == null) ? s.AssessmentId : 0,
                                                           EffortListId = (s.ParentId == null) ? s.EffortId : 0,
                                                           MarksId = (_r.MarksId == null) ? 0 : (int)_r.MarksId,
                                                           EffortId = (_r.EffortId == null) ? 0 : (int)_r.EffortId,
                                                           ParentType = (s.ParentId == null) ? 0 : (int)s.Subject1.ReportType,
                                                           PageBreak = s.PageBreak,
                                                           Comments = (s.KlaComments) ? _r.Comments : string.Empty
                                                       };


                    IQueryable<RepMark> assess = from m in db.Marks
                                                 orderby m.ColOrder
                                                 select new RepMark()
                                                 {
                                                     Id = m.Id,
                                                     Name = m.Name,
                                                     AssessmentId = m.AssessmentId
                                                 };

                    Paragraph klaPara = new Paragraph();

                    klaPara.SpacingBefore = 9f;
                    klaPara.SpacingAfter = 9f;
                    klaPara.Font = times14;
                    klaPara.Alignment = Element.ALIGN_LEFT;

                    List<string> klaName = new List<string>();
                    List<PdfPTable> klaList = new List<PdfPTable>();
                    //List<PdfPTable> commentList = new List<PdfPTable>();
                    List<List<PdfPTable>> tableList = new List<List<PdfPTable>>();

                    PdfPTable table6 = new PdfPTable(6);
                    table6.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                    table6.SetWidths(widths6A);
                    table6.LockedWidth = true;
                    table6.HorizontalAlignment = 1;

                    PdfPTable table6i = new PdfPTable(6);
                    table6i.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                    table6i.SetWidths(widths6B);
                    table6i.LockedWidth = true;
                    table6i.HorizontalAlignment = 1;

                    PdfPTable table5 = new PdfPTable(5);
                    table5.SetWidths(widths5);
                    table5.TotalWidth = 180f * postScriptPointsPerMilimeter; //baf 240415
                    table5.LockedWidth = true;

                    PdfPTable table8 = new PdfPTable(6);
                    table8.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                    table8.SetWidths(widths8);
                    table8.LockedWidth = true;
                    table8.HorizontalAlignment = 1;

                    float pageHeight = doc.PageSize.Height - doc.TopMargin - doc.BottomMargin - 9f;
                    float height = 0f, beforePara = 0f, spacing = 0f, klaHeight = 0f;
                    bool forceBreak = false;
                    int tableCount = 0;
                    string lastKla = "";



                    var klas = results.Where(m => m.ParentId == 0).OrderBy(m => m.ColOrder).ToList();
                    for (int i = 0; i <= klas.Count() + 1; i++)
                    {
                        if (klaName.Count() > 0)
                            lastKla = klaName.Last();

                        if (i > 0)
                        {
                            if (klaHeight + height > pageHeight)
                            {
                                forceBreak = true;
                            }
                            else
                            {
                                height += klaHeight;
                                klaHeight = 0f;
                                forceBreak = false;
                            }
                        }
                        if (i > 0)
                        {
                            bool doBreak = false;
                            if (i < klas.Count())
                                doBreak = klas.ElementAt(i).PageBreak;
                            else if (i == klas.Count() + 1)
                                doBreak = true;

                            if (doBreak || forceBreak)
                            {
                                doc.NewPage();

                                footer.student = c.Student.FirstName.Trim() + " " + c.Student.LastName.Trim() + " " + c.Student.Class.ClassDesc.Trim();

                                bool addedKlaList = false;
                                if ((!forceBreak || klaHeight > pageHeight && height == 0) && klaList.Count() > 0 || i == klas.Count() + 1)
                                {
                                    if (klaList.Count() > 0)
                                        tableList.Add(new List<PdfPTable>(klaList));
                                    tableCount += klaList.Count();
                                    addedKlaList = true;
                                }

                                GetNewSpacing(ref beforePara, ref spacing, pageHeight - height, tableCount, klaName.Count());
                                int k = 0;
                                foreach (var thisKla in tableList)
                                {
                                    klaPara.Clear();
                                    klaPara.Add(klaName[k]);
                                    if (k == 0)
                                        klaPara.SpacingBefore = 9f;
                                    else
                                        klaPara.SpacingBefore = beforePara;
                                    klaPara.SpacingAfter = spacing;
                                    doc.Add(klaPara);
                                    k++;
                                    foreach (var item in thisKla)
                                    {
                                        item.SpacingBefore = spacing;
                                        item.SpacingAfter = spacing;
                                        doc.Add(item);
                                    }
                                }

                                if (!addedKlaList)
                                {
                                    height = klaHeight;
                                    klaHeight = 0f;
                                    tableList.Clear();
                                    tableList.Add(new List<PdfPTable>(klaList));
                                    tableCount = klaList.Count();
                                    klaList.Clear();
                                    klaName.Clear();
                                    klaName.Add(lastKla);
                                }
                                else
                                {
                                    height = 0f;
                                    klaHeight = 0f;
                                    klaList.Clear();
                                    tableList.Clear();
                                    tableCount = 0;
                                    klaName.Clear();
                                }


                            }
                            else
                            {
                                tableList.Add(new List<PdfPTable>(klaList));
                                tableCount += klaList.Count();
                                klaList.Clear();
                                klaHeight = 0f;
                            }
                        }
                        if (i < klas.Count())
                        {
                            var kla = klas.ElementAt(i);

                            klaName.Add(kla.Subject.Trim().ToUpper());// doc.Add(para);
                            klaHeight += 14f;

                            if (kla.ReportType == 1)
                            {
                                cell.HorizontalAlignment = 0;
                                cell.Indent = 1;
                                cell.Border = 0;
                                cell.BackgroundColor = BaseColor.WHITE;
                                cell.Phrase = new Phrase("Overall Achievement", times10);
                                table6.DeleteBodyRows();
                                table6.AddCell(cell);

                                var marks = assess.Where(m => m.AssessmentId == kla.AssessListId).ToList();
                                for (int j = 0; j < marks.Count(); j++)
                                {
                                    var m = marks.ElementAt(j);
                                    cell.HorizontalAlignment = 1;
                                    cell.Border = Rectangle.BOX;
                                    cell.Phrase = new Phrase(m.Name, times10);
                                    if (kla.MarksId == m.Id)
                                        cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                    else
                                        cell.BackgroundColor = BaseColor.WHITE;
                                    table6.AddCell(cell);
                                }


                                cell.HorizontalAlignment = 0;
                                cell.Indent = 1;
                                cell.Border = 0;
                                cell.BackgroundColor = BaseColor.WHITE;
                                cell.Phrase = new Phrase("Effort", times10);
                                table6.AddCell(cell);

                                //foreach (var m in kla.Assessment1.Marks.OrderBy(x => x.ColOrder))
                                var efforts = assess.Where(m => m.AssessmentId == kla.EffortListId).ToList();
                                for (int j = 0; j < efforts.Count(); j++)
                                {
                                    var m = efforts.ElementAt(j);
                                    cell.HorizontalAlignment = 1;
                                    cell.Border = Rectangle.BOX;
                                    cell.Phrase = new Phrase(m.Name.Trim(), times10);
                                    if (kla.EffortId == m.Id)
                                        cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                    else
                                        cell.BackgroundColor = BaseColor.WHITE;
                                    table6.AddCell(cell);
                                }

                                klaList.Add(new PdfPTable(table6));//doc.Add(table6);
                                klaHeight += TotalRowHeights(doc, writer.DirectContent, table6);
                                int b = 1;

                                var areas1 = results.Where(m => m.ParentId == kla.SubjectId && m.IsTopic).OrderBy(m => m.ColOrder).ToList();
                                if (areas1 != null && areas1.Count() > 0)
                                {
                                    var marks1 = assess.Where(m => m.AssessmentId == kla.AssessListId).ToList();

                                    table6i.DeleteBodyRows();

                                    cell.BackgroundColor = BaseColor.WHITE;
                                    cell.Border = 0;
                                    cell.Phrase = new Phrase("", times12);
                                    cell.HorizontalAlignment = 0;
                                    table6i.AddCell(cell);

                                    cell.HorizontalAlignment = 1;
                                    cell.Border = Rectangle.BOX;
                                    b = 1;

                                    //foreach (var m in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                    for (int j = 0; j < marks1.Count(); j++)
                                    {
                                        var m = marks1.ElementAt(j);
                                        cell.Phrase = new Phrase(m.Name.Substring(0, 1), times10);
                                        if (b == 3)
                                            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                        else
                                            cell.BackgroundColor = BaseColor.WHITE;
                                        table6i.AddCell(cell);
                                        b++;
                                    }
                                    //foreach (var a in areas1)
                                    for (int j = 0; j < areas1.Count(); j++)
                                    {
                                        var a = areas1.ElementAt(j);
                                        cell.HorizontalAlignment = 0;
                                        cell.BackgroundColor = BaseColor.WHITE;
                                        cell.Phrase = new Phrase(a.Subject.Trim(), times10);
                                        table6i.AddCell(cell);
                                        b = 1;
                                        //foreach (var m1 in marks)
                                        //foreach (var m1 in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                        for (int k = 0; k < marks1.Count(); k++)
                                        {
                                            var m1 = marks1.ElementAt(k);
                                            cell.HorizontalAlignment = 1;
                                            if (b == 3)
                                                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                            else
                                                cell.BackgroundColor = BaseColor.WHITE;
                                            if (a.MarksId == m1.Id)
                                            {
                                                cell.Phrase = new Phrase(tick);
                                                //cell.Phrase.Add(new Chunk("\u0033", zapfdingbats));
                                            }
                                            else
                                                cell.Phrase = new Phrase("", times10);
                                            table6i.AddCell(cell);
                                            b++;
                                        }

                                    }
                                    klaList.Add(new PdfPTable(table6i));//doc.Add(table6i);
                                    klaHeight += TotalRowHeights(doc, writer.DirectContent, table6i);
                                }


                                var substrands = results.Where(m => m.ParentId == kla.SubjectId && !m.IsTopic).OrderBy(m => m.ColOrder).ToList();
                                if (substrands != null)
                                {
                                    //foreach (var subItem in substrands)
                                    for (int j = 0; j < substrands.Count(); j++)
                                    {
                                        var subItem = substrands.ElementAt(j);
                                        table6i.DeleteBodyRows();

                                        cell.BackgroundColor = BaseColor.WHITE;
                                        cell.Border = Rectangle.BOX;
                                        cell.Phrase = new Phrase(subItem.Subject.Trim().ToUpper(), times12);
                                        cell.HorizontalAlignment = 0;
                                        table6i.AddCell(cell);

                                        cell.HorizontalAlignment = 1;
                                        b = 1;

                                        //foreach (var m in kla.Assessment.Marks)
                                        for (int k = 0; k < marks.Count(); k++)
                                        {
                                            var m = marks.ElementAt(k);
                                            cell.Phrase = new Phrase(m.Name.Substring(0, 1), times10);
                                            if (b == 3)
                                                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                            else
                                                cell.BackgroundColor = BaseColor.WHITE;
                                            table6i.AddCell(cell);
                                            b++;
                                        }

                                        var areas2 = results.Where(m => m.ParentId == subItem.SubjectId).OrderBy(m => m.ColOrder).ToList();
                                        if (areas2 != null && areas2.Count() > 0)
                                        {
                                            //foreach (var a in areas2)
                                            for (int l = 0; l < areas2.Count(); l++)
                                            {
                                                var a = areas2.ElementAt(l);
                                                cell.HorizontalAlignment = 0;
                                                cell.BackgroundColor = BaseColor.WHITE;
                                                cell.Phrase = new Phrase(a.Subject.Trim(), times10);
                                                table6i.AddCell(cell);
                                                b = 1;

                                                //foreach (var m1 in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                                for (int k = 0; k < marks.Count(); k++)
                                                {
                                                    var m1 = marks.ElementAt(k);
                                                    cell.HorizontalAlignment = 1;
                                                    if (b == 3)
                                                        cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                                    else
                                                        cell.BackgroundColor = BaseColor.WHITE;
                                                    if (a.MarksId == m1.Id)
                                                        cell.Phrase = new Phrase(tick);
                                                    else
                                                        cell.Phrase = new Phrase("", times10);
                                                    table6i.AddCell(cell);
                                                    b++;
                                                }

                                            }
                                            klaList.Add(new PdfPTable(table6i));//doc.Add(table6i);
                                            klaHeight += TotalRowHeights(doc, writer.DirectContent, table6i);
                                        }
                                    }
                                }
                            }
                            else if (kla.ReportType == 2)
                            {
                                var substrands = results.Where(m => m.ParentId == kla.SubjectId && !m.IsTopic).OrderBy(m => m.ColOrder).ToList();
                                if (substrands.Count > 0)// != null)
                                {
                                    var efforts = assess.Where(m => m.AssessmentId == kla.EffortListId).ToList();

                                    table6.DeleteBodyRows();

                                    cell.HorizontalAlignment = 0;
                                    cell.Indent = 1;
                                    cell.Border = 0;
                                    cell.BackgroundColor = BaseColor.WHITE;
                                    cell.Phrase = new Phrase("Effort in", times9I);
                                    table6.AddCell(cell);

                                    for (int j = 0; j < efforts.Count(); j++)
                                    {
                                        cell.Phrase = new Phrase("", times10);
                                        table6.AddCell(cell);
                                    }

                                    //foreach (var subItem in substrands)
                                    for (int j = 0; j < substrands.Count(); j++)
                                    {
                                        var subItem = substrands.ElementAt(j);
                                        cell.HorizontalAlignment = 0;
                                        cell.Indent = 1;
                                        cell.Border = 0;
                                        cell.BackgroundColor = BaseColor.WHITE;
                                        cell.Phrase = new Phrase(subItem.Subject.Trim(), times10);
                                        table6.AddCell(cell);

                                        //foreach (var m in kla.Assessment1.Marks.OrderBy(x => x.ColOrder))
                                        for (int k = 0; k < efforts.Count(); k++)
                                        {
                                            var m = efforts.ElementAt(k);
                                            cell.HorizontalAlignment = 1;
                                            cell.Border = Rectangle.BOX;
                                            cell.Phrase = new Phrase(m.Name.Trim(), times10);
                                            if (subItem.EffortId == m.Id)
                                                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                            else
                                                cell.BackgroundColor = BaseColor.WHITE;
                                            table6.AddCell(cell);
                                        }
                                    }
                                    klaList.Add(new PdfPTable(table6));// doc.Add(table6);
                                    klaHeight += TotalRowHeights(doc, writer.DirectContent, table6);
                                }

                                var areas1 = results.Where(m => m.ParentId == kla.SubjectId && m.IsTopic).OrderBy(m => m.ColOrder).ToList();
                                if (areas1 != null && areas1.Count() > 0)
                                {
                                    var marks = assess.Where(m => m.AssessmentId == kla.AssessListId).ToList();
                                    table6i.DeleteBodyRows();

                                    cell.BackgroundColor = BaseColor.WHITE;
                                    cell.Border = 0;
                                    cell.Phrase = new Phrase("", times12);
                                    cell.HorizontalAlignment = 0;
                                    table6i.AddCell(cell);

                                    cell.HorizontalAlignment = 1;
                                    cell.Border = Rectangle.BOX;
                                    int b = 1;
                                    //foreach (var m in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                    for (int j = 0; j < marks.Count(); j++)
                                    {
                                        var m = marks.ElementAt(j);
                                        cell.Phrase = new Phrase(m.Name.Substring(0, 1), times10);
                                        if (b == 3)
                                            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                        else
                                            cell.BackgroundColor = BaseColor.WHITE;
                                        table6i.AddCell(cell);
                                        b++;
                                    }
                                    //foreach (var a in areas1)
                                    for (int j = 0; j < areas1.Count(); j++)
                                    {
                                        var a = areas1.ElementAt(j);
                                        cell.HorizontalAlignment = 0;
                                        cell.BackgroundColor = BaseColor.WHITE;
                                        cell.Phrase = new Phrase(a.Subject, times10);
                                        table6i.AddCell(cell);
                                        b = 1;
                                        //foreach (var m1 in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                        for (int k = 0; k < marks.Count(); k++)
                                        {
                                            var m1 = marks.ElementAt(k);
                                            cell.HorizontalAlignment = 1;
                                            if (b == 3)
                                                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                                            else
                                                cell.BackgroundColor = BaseColor.WHITE;
                                            if (a.MarksId == m1.Id)
                                                cell.Phrase = new Phrase(tick);
                                            else
                                                cell.Phrase = new Phrase("", times10);
                                            table6i.AddCell(cell);
                                            b++;
                                        }

                                    }
                                    klaList.Add(new PdfPTable(table6i));// doc.Add(table6);
                                    klaHeight += TotalRowHeights(doc, writer.DirectContent, table6i);
                                }
                            }

                            else if (kla.ReportType == 3)
                            {
                                var areas1 = results.Where(m => m.ParentId == kla.SubjectId && m.IsTopic).OrderBy(m => m.ColOrder).ToList();
                                if (areas1 != null && areas1.Count() > 0)
                                {
                                    var marks = assess.Where(m => m.AssessmentId == kla.AssessListId).ToList();
                                    string key3 = "Key: ";
                                    //foreach (var m in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                    for (int j = 0; j < marks.Count(); j++)
                                    {
                                        var m = marks.ElementAt(j);
                                        key3 += m.Name.Substring(0, 1) + " - " + m.Name.Trim() + "   ";
                                    }

                                    table5.DeleteBodyRows();

                                    cell.BackgroundColor = BaseColor.WHITE;
                                    cell.Border = 0;
                                    cell.Phrase = new Phrase(key3, times9I);
                                    cell.HorizontalAlignment = 0;
                                    cell.BackgroundColor = BaseColor.WHITE;
                                    table5.AddCell(cell);

                                    cell.HorizontalAlignment = 1;
                                    cell.Border = Rectangle.BOX;

                                    // foreach (var m in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                    for (int j = 0; j < marks.Count(); j++)
                                    {
                                        var m = marks.ElementAt(j);
                                        cell.Phrase = new Phrase(m.Name.Substring(0, 1), times10);
                                        table5.AddCell(cell);
                                    }
                                    //foreach (var a in areas1)
                                    for (int j = 0; j < areas1.Count(); j++)
                                    {
                                        var a = areas1.ElementAt(j);
                                        cell.HorizontalAlignment = 0;
                                        cell.BackgroundColor = BaseColor.WHITE;
                                        cell.Phrase = new Phrase(a.Subject.Trim(), times10);
                                        table5.AddCell(cell);

                                        //foreach (var m1 in kla.Assessment.Marks.OrderBy(x => x.ColOrder))
                                        for (int k = 0; k < marks.Count(); k++)
                                        {
                                            var m1 = marks.ElementAt(k);
                                            cell.HorizontalAlignment = 1;
                                            if (a.MarksId == m1.Id)
                                                cell.Phrase = new Phrase(tick);
                                            else
                                                cell.Phrase = new Phrase("", times10);
                                            table5.AddCell(cell);
                                        }

                                    }
                                    klaList.Add(new PdfPTable(table5));//doc.Add(table5);
                                    klaHeight += TotalRowHeights(doc, writer.DirectContent, table5);
                                }
                            }

                            else if (kla.ReportType == 4)
                            {
                                var areas1 = results.Where(m => m.ParentId == kla.SubjectId && m.IsTopic).OrderBy(m => m.ColOrder);
                                if (areas1 != null && areas1.Count() > 0)
                                {
                                    cell.BackgroundColor = BaseColor.WHITE;
                                    int j = 1;
                                    foreach (var a in areas1)
                                    {
                                        cell.HorizontalAlignment = 0;
                                        cell.Border = Rectangle.BOX;
                                        cell.Phrase = new Phrase(a.Subject.Trim(), times10);
                                        table8.AddCell(cell);

                                        cell.HorizontalAlignment = 1;
                                        if (a.MarksId != 0)
                                            cell.Phrase = new Phrase(tick);
                                        else
                                            cell.Phrase = new Phrase("", times10);
                                        table8.AddCell(cell);
                                        j++;
                                    }
                                    int m = areas1.Count() % 3;
                                    if (m > 0)
                                    {
                                        for (int k = 0; k < 3 - m; k++)
                                        {
                                            cell.HorizontalAlignment = 0;
                                            cell.Border = Rectangle.BOX;
                                            cell.Phrase = new Phrase("", times10);
                                            table8.AddCell(cell);

                                            cell.HorizontalAlignment = 1;
                                            cell.Phrase = new Phrase("", times10);
                                            table8.AddCell(cell);
                                        }
                                    }
                                    klaList.Add(new PdfPTable(table8));//doc.Add(table8);
                                    klaHeight += TotalRowHeights(doc, writer.DirectContent, table8);
                                }
                            }
                            if (!String.IsNullOrEmpty(kla.Comments))
                            {

                                PdfPTable tableKC = new PdfPTable(3);
                                tableKC.TotalWidth = 180f * postScriptPointsPerMilimeter;//150
                                tableKC.SetWidths(widths3);
                                tableKC.LockedWidth = true;
                                tableKC.HorizontalAlignment = 1;
                                tableKC.SpacingAfter = 1f;
                                tableKC.SpacingBefore = 5f;

                                PdfPCell cellCH = new PdfPCell();
                                cellCH.Border = 0;
                                cellCH.Indent = 1;
                                cellCH.Colspan = 3;
                                cellCH.HorizontalAlignment = 1;
                                cellCH.FixedHeight = 18f;
                                cellCH.Phrase = new Phrase("Comments", times10B);
                                tableKC.AddCell(cellCH);


                                PdfPCell cellKC = new PdfPCell();
                                cellKC.Border = 0;
                                cellKC.Indent = 1;
                                cellKC.Colspan = 3;
                                cellKC.HorizontalAlignment = Element.ALIGN_JUSTIFIED; //0; //baf 140715
                                // cellKC.FixedHeight = 280f;
                                cellKC.Phrase = new Phrase(kla.Comments, times10);
                                cellKC.SetLeading(1f, 1.4f);
                                tableKC.AddCell(cellKC);
                                klaList.Add(new PdfPTable(tableKC));
                                klaHeight += TotalRowHeights(doc, writer.DirectContent, tableKC);
                                //doc.Add(tableKC);
                            }
                        }

                    }
                    count++;
                }//baf xxx resurrect for print class

                doc.Close();
                byte[] file = stream.ToArray();
                MemoryStream output = new MemoryStream();
                output.Write(file, 0, file.Length);
                output.Position = 0;

                HttpContext.Response.AddHeader("content-disposition", "attachment; filename=SchoolReports.pdf");

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
                if (doc.IsOpen())
                    doc.Close();
            }

            return Content("");
        }

        [HttpPost]
        public ActionResult Details(int? id, string sortOrder, bool asc)
        {

            if (id == null)
            {
                return Content("");
            }
            Student student = db.Students.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            StudentDet model = new StudentDet();
            model.Id = student.Student_Id;
            model.SRN = student.SRN;
            model.Student = student.FirstName.Trim() + " " + student.LastName.Trim();
            model.Gender = String.IsNullOrEmpty(student.Gender.Trim()) ? "Not set" : (student.Gender == "M" ? "Boy" : "Girl");
            model.ClassName = student.Classes_Id == null ? "Not set" : student.Class.ClassDesc;
            model.Grade = student.GradeId == null ? "Not set" : student.Grade.Name;
            model.ReadLevel = student.Levels_Id == null ? "Not set" : student.Level.ReadLevel;
            model.ClassId = student.Classes_Id == null ? 0 : (int)student.Classes_Id;
            model.LibraryId = student.Library_id;
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            IQueryable<Contact> contacts = from sc in db.StudentContacts
                                           where sc.Student_Id == id

                                           select new Contact()
                                           {
                                               FullName = ((sc.Parent.Title_Id > 1) ? sc.Parent.ContactTitle.Salutation.Trim() + " " : "") + sc.Parent.FirstName.Trim() + " " + sc.Parent.LastName.Trim(),
                                               Relationship = sc.Relationship.Relationship1,
                                               RelationId = sc.Relation_Id == null ? 0 : (int)sc.Relation_Id,
                                               TitleId = sc.Parent.Title_Id == null ? 0 : (int)sc.Parent.Title_Id
                                           };
            model.Contacts = contacts;
            //string year = DateTime.Now.Year.ToString();
            //DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            //IQueryable<StudentLoanGroup> loansSummary = from l in db.Loans
            //                                            where l.Student_Id == id
            //                                            group l by l.BorrowDate.Year into yearGroup
            //                                            orderby yearGroup.Key descending
            //                                            select new StudentLoanGroup()
            //                                            {
            //                                                Year = yearGroup.Key,
            //                                                BookCount = yearGroup.Count()
            //                                            };
            //ViewBag.LoansSummary = loansSummary;
            //int pageSize = 10;
            //int pageNumber = loansPage ?? 1;
            //ViewBag.LoansPage = loansPage;
            //var loansByPage = student.Loans.OrderByDescending(x => x.BorrowDate).ToPagedList(pageNumber, pageSize);
            //ViewBag.LoansByPage = loansByPage;

            return View("_Details", model);

            //return View(student.Loans.ToPagedList(pageNumber, pageSize));

        }

        [HttpPost]
        public JsonResult IsSrnUnique(string SRN)
        {
            if (String.IsNullOrEmpty(SRN))
                return Json(true, JsonRequestBehavior.AllowGet);

            try
            {
                if (String.IsNullOrEmpty(SRN))
                    return Json(true, JsonRequestBehavior.AllowGet);

                else
                {
                    var student = db.Students.Single(m => m.SRN.ToLower() == SRN.ToLower());
                    return Json(student == null, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Students/Create
        [OutputCache(Duration = 0)]
        public ActionResult Create(int libraryId, int classId, string sortOrder, bool asc, string className)
        {
            StudentCreate model = new StudentCreate();
            model.Inactive = false;
            model.LibraryId = libraryId;
            model.ClassId = classId;
            model.ClassName = className;
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.GradeId = 0;
            model.LevelId = null;
            model.Gender = "";
            IQueryable<ReadLevel> levels = from l in db.Levels
                                           where l.Obsolete != true
                                           orderby l.ReadLevel
                                           select new ReadLevel
                                           {
                                               Id = l.Levels_Id,
                                               Level = l.ReadLevel
                                           };
            model.LevelsList = levels;

            IQueryable<GradeItem> grades = from g in db.Grades
                                           select new GradeItem
                                           {
                                               Id = g.Id,
                                               Grade = g.FullName
                                           };
            model.GradesList = grades;
            return View("_Create", model);

        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "Student_Id,FirstName,LastName,KnownAs,Dob,Classes_Id,Inactive,Phone,Levels_Id,Address_Id,Library_id,Gender,Old_Id")] Student student)
        //public ActionResult Create([Bind(Include = "FirstName,LastName,Classes_Id,Levels_Id")] Student student)
        public ActionResult Create(StudentCreate model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Student s = new Student();
                    s.Library_id = model.LibraryId;
                    s.FirstName = model.FirstName;
                    s.LastName = model.LastName;
                    s.Classes_Id = model.ClassId;
                    s.Gender = model.Gender;
                    s.Inactive = false;
                    s.SRN = model.SRN;
                    if (model.LevelId > 0)
                        s.Levels_Id = model.LevelId;
                    s.GradeId = model.GradeId;
                    db.Students.Add(s);
                    try
                    {
                        db.SaveChanges();
                        int newId = s.Student_Id;
                        return RedirectToAction("MyClass", new { classId = model.ClassId, libraryId = model.LibraryId, sortOrder = model.SortOrder, asc = model.Ascending, Activity = "Organise", className = model.ClassName, selectedId = newId });
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    }

                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return View("_Create", model);
        }

        // GET: Students/Edit/5
        [OutputCache(Duration = 0)]
        public ActionResult Edit(int? id, int classId, int libraryId, string sortOrder, bool asc)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = db.Students.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            StudentEdit model = new StudentEdit();
            model.Id = student.Student_Id;
            //model.Name = student.FirstName.Trim() + " " + student.LastName.Trim();
            model.FirstName = student.FirstName.Trim();
            model.LastName = student.LastName.Trim();
            model.SRN = student.SRN.Trim();
            model.Inactive = student.Inactive == null ? false : (bool)student.Inactive;
            model.Gender = student.Gender;
            model.Classes_Id = student.Classes_Id;
            model.GradeId = student.GradeId ?? 0;
            model.Levels_Id = student.Levels_Id ?? 0;
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            IQueryable<ClassItem> classes = from c in db.Classes
                                            where c.Library_Id == libraryId && !c.Obsolete
                                            orderby c.ClassDesc
                                            select new ClassItem()
                                            {
                                                Id = c.Classes_Id,
                                                ClassName = c.ClassDesc
                                            };
            model.ClassList = classes;

            IQueryable<GradeItem> grades = from g in db.Grades
                                           select new GradeItem
                                           {
                                               Id = g.Id,
                                               Grade = g.FullName
                                           };
            model.GradesList = grades;

            IQueryable<ReadLevel> levels = from l in db.Levels
                                           where l.Obsolete != true
                                           orderby l.ReadLevel
                                           select new ReadLevel
                                           {
                                               Id = l.Levels_Id,
                                               Level = l.ReadLevel
                                           };
            model.LevelsList = levels;

            return PartialView("_Edit", model);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        [HttpPost, ActionName("Edit")]//
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id, int classes_Id, string sortOrder, bool ascending)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var student = db.Students.Find(id);
            if (TryUpdateModel(student, "",
               new string[] { "FirstName", "LastName", "Gender", "Inactive", "SRN", "Classes_Id", "GradeId", "Levels_Id" }))
            {
                try
                {
                    db.Entry(student).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("MyClass", new { selectedId = id, classId = classes_Id, lastOrder = sortOrder, asc = ascending, activity = "Organise" });

                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");

                }
            }
            StudentVM model = new StudentVM();
            model.Id = student.Student_Id;
            model.Name = student.FirstName.Trim() + " " + student.LastName.Trim();
            model.FirstName = student.FirstName;
            model.LastName = student.LastName;
            model.Inactive = student.Inactive == null ? false : (bool)student.Inactive;
            model.Gender = student.Gender;
            model.ClassId = student.Classes_Id;
            return PartialView("_Edit", model);
        }

        //public ActionResult Edit([Bind(Include = "Student_Id,FirstName,LastName,KnownAs,Dob,Classes_Id,Inactive,Phone,Levels_Id,Address_Id,Library_id,Gender,Old_Id")] Student student)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(student).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //   // ViewBag.Address_Id = new SelectList(db.CardAddresses, "Address_Id", "Add1", student.Address_Id);
        //    ViewBag.Classes_Id = new SelectList(db.Classes, "Classes_Id", "ClassDesc", student.Classes_Id);
        //    ViewBag.Levels_Id = new SelectList(db.Levels, "Levels_Id", "ReadLevel", student.Levels_Id);
        //   // ViewBag.Library_id = new SelectList(db.Libraries, "Library_Id", "Licensee", student.Library_id);
        //    return View(student);
        //}

        // GET: Students/Delete/5
        public ActionResult Delete(int? id, string classFilter, string searchString, string lastOrder, int? page, bool? saveChangesError = false, bool? hasLoans = false, bool ascending = true)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (saveChangesError.GetValueOrDefault())
            {
                if (hasLoans == true)
                    ViewBag.ErrorMessage = "Delete failed. The student has outstanding loans.";
                else
                    ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
            }
            Student student = db.Students.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClassFilter = classFilter;
            ViewBag.SearchString = searchString;
            ViewBag.SortColumn = lastOrder;
            ViewBag.Page = page;
            return View(student);
        }

        // POST: Students/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Student student = db.Students.Find(id);
        //    db.Students.Remove(student);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string classFilter, string searchString, string lastOrder, int? page)
        {
            try
            {
                var oustandingLoans = (from l in db.Loans
                                       where l.Student_Id == id && l.ReturnDate == null
                                       select l).Count();
                if ((int)oustandingLoans > 0)
                {

                    return RedirectToAction("Delete", new { id = id, saveChangesError = true, hasLoans = true });
                }
                else
                {
                    //Student student = db.Students.Find(id);
                    //db.Students.Remove(student);
                    Student studentToDelete = new Student() { Student_Id = id };
                    db.Entry(studentToDelete).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            catch (DataException/* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
            TempData["ClassFilter"] = classFilter;
            TempData["UseTempData"] = "Y";
            TempData["SearchString"] = searchString;
            TempData["SortColumn"] = lastOrder;
            TempData["Page"] = page;
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Teacher, Supervisor")]
        public ActionResult Index(string classList, string searchString, string sortOrder, string lastOrder, int? page)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());//baf xxx need to set up a viewmodel here if I'm going to use it
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Students/SelectClass" });
            int libraryId = user.LibraryId;


            if (Session["classId"] == null)
                return RedirectToAction("SelectClass");


            bool ascending = true;
            if (TempData["UseTempData"] != null)
            {
                searchString = TempData["SearchString"] == null ? null : TempData["SearchString"].ToString();
                classList = TempData["ClassFilter"] == null ? null : TempData["ClassFilter"].ToString();
                lastOrder = TempData["SortColumn"] == null ? "FirstName" : TempData["SortColumn"].ToString();
                page = TempData["Page"] == null ? 1 : (int)TempData["Page"];
            }

            if (String.IsNullOrEmpty(sortOrder))
            {
                if (String.IsNullOrEmpty(lastOrder))
                    ViewBag.SortColumn = sortOrder = "FirstName";

                else if (lastOrder.Substring(0, 1) == "D")
                {
                    ascending = false;
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
                    ascending = false;
                    ViewBag.SortColumn = "D" + sortOrder;
                }
                //else if (sortOrder == "D" + lastOrder)
                //    ViewBag.SortColumn = sortOrder;
                else
                    ViewBag.SortColumn = sortOrder;
            }

            var ClassLst = new List<string>();
            ClassLst.Add("All");
            ClassLst.Add("Not set");
            bool showInactive = false;
            var ClassQry = from c in db.Classes
                           where c.Library_Id == libraryId && !c.Obsolete
                           orderby c.ClassDesc
                           select c.ClassDesc.TrimEnd();

            ClassLst.AddRange(ClassQry.Distinct());
            ViewBag.classList = new SelectList(ClassLst);
            if (string.IsNullOrEmpty(classList))
            {
                var selectList = new SelectList(ClassLst, "ClassDesc", "ClassDesc", new { id = "All" });
                ViewBag.SelectedClass = selectList;
            }
            else if (classList == "Not set")
            {
                var selectList = new SelectList(ClassLst, "ClassDesc", "ClassDesc", new { id = "Not set" });
                ViewBag.SelectedLevel = selectList;
            }
            else
            {
                var selectList = new SelectList(ClassLst, "ClassDesc", "ClassDesc", new { id = classList });
                ViewBag.SelectedClass = selectList;
            }
            var students = from s in db.Students
                           where s.Library_id == libraryId && s.Inactive == showInactive
                           select s;

            if (classList == "Not set")
            {
                students = students.Where(s => s.Classes_Id == null);
                ViewBag.LevelFilter = classList;
            }
            else if (!String.IsNullOrEmpty(classList) && classList != "All")
            {
                students = students.Where(s => s.Class.ClassDesc == classList);
                ViewBag.ClassFilter = classList;
            }

            else if (!String.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => (s.FirstName.StartsWith(searchString) || s.LastName.StartsWith(searchString)));
                ViewBag.SearchString = searchString;
            }

            switch (sortOrder)
            {
                case "LastName":
                    if (ascending)
                        students = students.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                    else
                        students = students.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                    break;

                case "Class":
                    if (ascending)
                        students = students.OrderBy(s => s.Class.ClassDesc).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Class.ClassDesc).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Level":
                    if (ascending)
                        students = students.OrderBy(s => s.Level.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.Level.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                default:
                    if (ascending)
                        students = students.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        students = students.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }
            int pageSize = 15;
            int pageNumber = (page ?? 1);
            ViewBag.Page = page;

            var LevelQry = from l in db.Levels
                           where l.Obsolete != true
                           orderby l.ReadLevel
                           select new
                           {
                               LevelId = l.Levels_Id,
                               l.ReadLevel
                           };
            //ViewBag.LevelId = new SelectList(LevelQry, "LevelId", "ReadLevel");
            ViewBag.LevelId = AddDefaultOption(new SelectList(LevelQry, "LevelId", "ReadLevel"), "", "", false);
            var ClassQry2 = from c in db.Classes
                            where c.Library_Id == libraryId && c.Obsolete != true
                            orderby c.ClassDesc
                            select new
                            {
                                ClassId = c.Classes_Id,
                                c.ClassDesc
                            };
            ViewBag.ClassId = AddDefaultOption(new SelectList(ClassQry2, "ClassId", "ClassDesc"), "Not set", String.Empty, true);
            return View(students.ToPagedList(pageNumber, pageSize));
            // return View(students);
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

