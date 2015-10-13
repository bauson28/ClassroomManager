using HomeReadingManager.Models;
using HomeReadingManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using HomeReadingManager.MyClasses;
using System.Threading.Tasks;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System.Web.Profile;
using System.Net;
using System.Data.Entity;
using System.Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;



namespace HomeReadingManager.Controllers
{
    public class ReportsController : Controller
    {
        public string TellMeDate()
        {
            return DateTime.Today.ToString();
        }

        private HomeReadingEntities db = new HomeReadingEntities();
        
        private ApplicationUserManager _userManager;

        public ReportsController()
        {

        }

        public ReportsController(ApplicationUserManager userManager)
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

        private Dictionary<string, string> GetDaysOutList()
        {
            return new Dictionary<string, string>
            {
                { "0", "Select days" },  { "2", "2 days" }, {  "3", "3 days" }, {"4", "4 days" }, {"5", "5 days" }, { "7", "7days" },
                                { "10", "10 days" }, { "14" , "14 days"}, { "30", "30 days" }, { "60", "60 days" }
             };
        }

        private Dictionary<string, string> GetYearList()
        {
            Dictionary<string, string> years = new Dictionary<string, string>();
            for (int i = 0; i < 5; i++)
            {
                years.Add((DateTime.Now.Year - i).ToString(),(DateTime.Now.Year - i).ToString());
            }
            return years;
        }
       
        // GET: Reports
        //[Authorize(Roles = "Parent helper, Teacher, Supervisor")]
        public ActionResult Index(int? reportType, int? studentId, int? year, int? days, int? classId, int? page, DateTime? activityDate,
             string student, string className, string searchString, int? searchPage, string sortOrder, string newOrder, string school, int? libraryId, bool asc = true)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Reports/Index" });
                libraryId = user.LibraryId;
                var query = (from l in db.Libraries
                             where l.Library_Id == libraryId
                             select l).FirstOrDefault();
                school = query.Licensee;
            }
           
            ReportData model = new ReportData();
            
            model.ReportType = reportType == null ? 1 : (int)reportType;
            model.Student_Id = studentId == null ? 0 : (int)studentId;
            model.Student = student;
            model.ClassId = classId == null ? 0 : (int)classId;
            model.ClassName = className;
            model.Days = days == null ? 0 : (int)days;
            model.Year = year == null ? DateTime.Now.Year : (int)year;
            model.ActivityDate = activityDate == null ? DateTime.Now.AddDays(-1) : Convert.ToDateTime(activityDate);

            switch (model.ReportType)
            {
                default:
                    model.ReportName = model.Days == 0 ? "Select the number of days out" : "Loans Overdue by " + model.Days + " Days";
                    break;
                case 2:
                    model.ReportName = "Borrowings on " + model.ActivityDate.DayOfWeek + " " + model.ActivityDate.ToString("dd MMM yyyy");
                    break;
                case 3:
                    model.ReportName = "Loans returned on " + model.ActivityDate.DayOfWeek + " " + model.ActivityDate.ToString("dd MMM yyyy");
                    break;
                case 4:
                    model.ReportName = "School Summary by Class";
                    break;
                case 5:
                    model.ReportName = model.ClassId == 0 ? "Select the class" : "Summary for Class " + model.ClassName;
                    break;
                case 6:
                    model.ReportName = model.Student_Id == 0 ? "Student search: select student" : "Loans for " + model.Student;
                    model.Years = GetYearList();
                    break;
            }

            IQueryable<ClassItem> classes = from c in db.Classes
                                            where c.Library_Id == (int)libraryId && !c.Obsolete
                                            orderby c.ClassDesc
                                            select new ClassItem()
                                            {
                                                Id = c.Classes_Id,
                                                ClassName = c.ClassDesc
                                            };
            model.Classes = classes;
            model.DaysList = GetDaysOutList();
            model.SearchPage = (searchPage ?? 0);
            model.SearchString = searchString;
            model.SelectedYear = (year ?? DateTime.Now.Year);
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;
            model.School = school;
            model.LibraryId = (int)libraryId;
          
            return View(model);
        }

        public ActionResult Overdue(int? days, string newOrder, string sortOrder, int? page, int libraryId, string school, bool asc = true)
        {
            if (days == null || (int)days == 0)
                return PartialView("_SelectReport");

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
           
            
            
            IQueryable<OverdueItem> reportList = from l in db.Loans.Where(d => d.ReturnDate == null && System.Data.Entity.DbFunctions.DiffDays(d.BorrowDate, DateTime.Now) >= days
                                                        && d.Student.Library_id == libraryId)
                                                 select new OverdueItem()
                                                 {
                                                     Title = l.Product.Title,
                                                     Student_Id = l.Student_Id,
                                                     FirstName = l.Student.FirstName,
                                                     LastName = l.Student.LastName,
                                                     BorrowDate = l.BorrowDate,
                                                     Days = (int)System.Data.Entity.DbFunctions.DiffDays(l.BorrowDate, DateTime.Now),
                                                     ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                                     ClassName = l.Student.Class.ClassDesc,
                                                 };

            //reportList = reportList.OrderByDescending(s => s.Days);
            switch (sortOrder)
            {
                case "FirstName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;

                case "LastName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                    else
                        reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                    break;

                case "ClassName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                default:
                    if (asc)
                        reportList = reportList.OrderBy(s => s.Days).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.Days).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;
            }
            
            if (reportList.Count() > 0)
            {
                int pageSize = 17;
                int pageNumber = (page ?? 1);
                OverdueModel model = new OverdueModel();
                model.LibraryId = libraryId;
                model.School = school;
                model.SortOrder = sortOrder;
                model.Ascending = asc;
                model.Page = pageNumber;
                model.Days = (int)days;
                model.OverdueList = reportList.ToPagedList(pageNumber, pageSize);
                
                return PartialView("_Overdue", model);

            }
            else
            {
                // CusValRight.IsValid = false;
                //CusValRight.ErrorMessage = barcode + " is not a valid ISBN.";
            }
            return PartialView("_SelectReport");
        }

        public ActionResult ActivityByDate(DateTime? activityDate, string activityDateString , string newOrder, string sortOrder, int? page, bool borrowing, int libraryId, string school, bool asc = true)
        {
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "FirstName";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }

            activityDate = (activityDate == null) ? DateTime.Now.AddDays(-1) : Convert.ToDateTime(activityDate);

            IQueryable<BorrowItem> reportList;
            if (borrowing)
            {
                reportList = from l in db.Loans.Where(d => System.Data.Entity.DbFunctions.TruncateTime(d.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(activityDate)
                                                                                                                 && d.Student.Library_id == libraryId)
                             select new BorrowItem()
                             {
                                 Title = l.Product.Title,
                                 Student_Id = l.Student_Id,
                                 FirstName = l.Student.FirstName,
                                 LastName = l.Student.LastName,
                                 ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                 ClassName = l.Student.Class.ClassDesc,
                             };
            }
            else
            {
                reportList = from l in db.Loans.Where(d => System.Data.Entity.DbFunctions.TruncateTime(d.ReturnDate) == System.Data.Entity.DbFunctions.TruncateTime(activityDate)
                                                                                                                 && d.Student.Library_id == libraryId)
                             select new BorrowItem()
                             {
                                 Title = l.Product.Title,
                                 Student_Id = l.Student_Id,
                                 FirstName = l.Student.FirstName,
                                 LastName = l.Student.LastName,
                                 ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                 ClassName = l.Student.Class.ClassDesc,
                             };
            }
            switch (sortOrder)
            {
                case "LastName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                    else
                        reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                    break;

                case "ClassName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Title":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.Title);
                    else
                        reportList = reportList.OrderByDescending(s => s.Title);
                    break;

                default:
                    if (asc)
                        reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }

            if (reportList != null)//.Count() > 0)
            {
                BorrowModel model = new BorrowModel();

                int pageSize = 17;
                int pageNumber = (page ?? 1);
                model.LibraryId = libraryId;
                model.School = school;
                model.SortOrder = sortOrder;
                model.Ascending = asc;
                model.Page = pageNumber;
                model.ActivityDate = (DateTime)activityDate;
                model.ReportType = (borrowing) ? 2 : 3;
                model.BorrowList = reportList.ToPagedList(pageNumber, pageSize);
                
                return PartialView("_Borrowed", model);
            }
            return PartialView("_SelectReport");
        }

        public ActionResult SchoolLoans(string newOrder, string sortOrder, int? page, int libraryId, string school, bool asc = true)
        {
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "ClassName";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }

            DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            string year = DateTime.Now.Year.ToString();

            IQueryable<SchoolLoansItem> reportList = from c in db.Classes.Where(s => !s.Obsolete && s.Library_Id == libraryId && s.Students.Count > 0)

                                                     join t in db.Teachers on c.Teacher_Id equals t.Teacher_Id into t2
                                                     from t3 in t2.DefaultIfEmpty()

                                                     join b2 in
                                                         (from b in db.BooksReads.Where(d => d.ForYear == year && d.Student.Inactive == false)
                                                          group b by b.Student.Classes_Id into grp
                                                          let total = grp.Sum(b => b.BooksRead1)
                                                          select new { Class_Id = grp.Key, BooksRead = total }) on c.Classes_Id equals b2.Class_Id into b3
                                                     from x in b3.DefaultIfEmpty()

                                                     join l2 in
                                                         (from l in db.Loans.Where(d => d.BorrowDate >= startDate && d.Student.Inactive == false)

                                                          group l by l.Student.Classes_Id into grp
                                                          let count = grp.Count()
                                                          select new { Class_Id = grp.Key, BooksRead = count }) on c.Classes_Id equals l2.Class_Id into l3
                                                     from y in l3.DefaultIfEmpty()

                                                     join s2 in
                                                         (from s in db.Students.Where(d => d.Inactive == false)
                                                          group s by s.Classes_Id into grp
                                                          let maxLevel = grp.Max(l => l.Levels_Id)//Level.ReadLevel //baf important need to work this out when I add different reading level types
                                                          let minLevel = grp.Min(l => l.Levels_Id)//Level.ReadLevel
                                                          let avgLevel = Math.Round(grp.Average(l => l.Level.Levels_Id), 2)
                                                          select new { Class_Id = grp.Key, MaxLevel = maxLevel, MinLevel = minLevel, AvgLevel = avgLevel }) on c.Classes_Id equals s2.Class_Id into s3

                                                     from z in s3.DefaultIfEmpty()

                                                     select new SchoolLoansItem()
                                                     {
                                                         ClassId = c.Classes_Id,
                                                         ClassName = c.ClassDesc,
                                                         //Teacher = (t3.FirstName == null) ? string.Empty : t3.FirstName + " " + t3.LastName,
                                                         Teacher = t3.FirstName + " " + t3.LastName,
                                                         BooksRead = ((y.BooksRead == null) ? 0 : y.BooksRead) + ((x.BooksRead == null) ? 0 : x.BooksRead),//+ y.BooksRead + x.BooksRead,
                                                         MaxLevel = ((z.MaxLevel == null) ? 0 : (int)z.MaxLevel),
                                                         MinLevel = ((z.MinLevel == null) ? 0 : (int)z.MinLevel),
                                                         AvgLevel = ((z.AvgLevel == null) ? 0 : (float)z.AvgLevel)
                                                     };

            switch (sortOrder)
            {
                case "Teacher":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.Teacher);
                    else
                        reportList = reportList.OrderByDescending(s => s.Teacher);
                    break;

                case "BooksRead":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.BooksRead).ThenBy(n => n.ClassName);
                    else
                        reportList = reportList.OrderByDescending(s => s.BooksRead).ThenBy(n => n.ClassName);
                    break;

                case "AvgLevel":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.AvgLevel).ThenBy(n => n.ClassName);
                    else
                        reportList = reportList.OrderByDescending(s => s.AvgLevel).ThenBy(n => n.ClassName);
                    break;

                default:
                    if (asc)
                        reportList = reportList.OrderBy(s => s.ClassName);
                    else
                        reportList = reportList.OrderByDescending(s => s.ClassName);
                    break;
            }
            if (reportList != null)
            {
                int pageSize = 17;
                int pageNumber = (page ?? 1);
                SchoolLoansModel model = new SchoolLoansModel();
                model.LibraryId = libraryId;
                model.School = school;
                model.SortOrder = sortOrder;
                model.Ascending = asc;
                model.Page = pageNumber;
                model.SchoolLoansList = reportList.ToPagedList(pageNumber, pageSize);

                return PartialView("_School", model);

            }
            return PartialView("_SelectReport");
        }

        public ActionResult ClassLoans(int? classId, string newOrder, string sortOrder, int? page, string className, int libraryId, string school, bool asc = true)
        {
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "FirstName";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }

            classId = classId == null ? 0 : (int)classId;
            DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
            string year = DateTime.Now.Year.ToString();


            IQueryable<ClassLoansItem> reportList = from s in db.Students.Where(s => s.Classes_Id == classId && s.Inactive == false)
                                                    join b in db.BooksReads.Where(b => b.ForYear == year) on s.Student_Id equals b.Student_Id into br
                                                    from x in br.DefaultIfEmpty()
                                                    join rl in db.Levels on s.Levels_Id equals rl.Levels_Id into rlc
                                                    from y in rlc.DefaultIfEmpty()

                                                    join l2 in
                                                        (from l in db.Loans.Where(d => d.BorrowDate >= startDate && d.Student.Classes_Id == classId && d.Student.Inactive == false)
                                                         group l by l.Student_Id into grp
                                                         let count = grp.Count()
                                                         select new { Student_Id = grp.Key, count }) on s.Student_Id equals l2.Student_Id into l3
                                                    from z in l3.DefaultIfEmpty()

                                                    select new ClassLoansItem()
                                                    {
                                                        Student_Id = s.Student_Id,
                                                        FullName = s.FirstName.Trim() + " " + s.LastName,
                                                        FirstName = s.FirstName,
                                                        LastName = s.LastName,
                                                        ReadLevel = y.ReadLevel,
                                                        BooksRead = ((int?)z.count ?? 0) + ((int?)x.BooksRead1 ?? 0)
                                                    };

            switch (sortOrder)
            {
                case "LastName":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                    else
                        reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                    break;

                case "Books":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                case "Level":
                    if (asc)
                        reportList = reportList.OrderBy(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                    break;

                default:
                    if (asc)
                        reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }
            if (reportList != null)
            {
                int pageSize = 17;
                int pageNumber = (page ?? 1);
                ClassLoansModel model = new ClassLoansModel();
                model.LibraryId = libraryId;
                model.School = school;
                model.SortOrder = sortOrder;
                model.Ascending = asc;
                model.Page = pageNumber;
                model.ClassId = (int)classId;
                model.ClassName = className;
                model.ClassLoansList = reportList.ToPagedList(pageNumber, pageSize);

                return PartialView("_Class", model);

            }
            return PartialView("_SelectReport");
        }

        public ActionResult StudentLoans(int? studentId, int? page, string student, int? year, int libraryId, string school)
        {
            if (studentId == null || (int)studentId == 0)
               PartialView("_SelectReport");
            studentId = (int)studentId;
            int thisYear = year == null ? DateTime.Now.Year : (int)year;
          
            DateTime startDate = DateTime.ParseExact("01/01/" + thisYear, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
            DateTime endDate = DateTime.ParseExact("31/12/" + thisYear, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

            IQueryable<StudentLoansItem> reportList = from loan in db.Loans
                                                      where loan.Student_Id == studentId
                                                        && System.Data.Entity.DbFunctions.TruncateTime(loan.BorrowDate) >= System.Data.Entity.DbFunctions.TruncateTime(startDate)
                                                        && System.Data.Entity.DbFunctions.TruncateTime(loan.BorrowDate) <= System.Data.Entity.DbFunctions.TruncateTime(endDate)
                                                        orderby loan.BorrowDate descending
                                                      select new StudentLoansItem()
                                                      {
                                                          Title = loan.Product.Title,
                                                          ReadLevel = loan.Product.Levels_Id == null ? "Not set" : loan.Product.Level.ReadLevel,
                                                          BorrowDate = loan.BorrowDate,
                                                          DaysOut = (loan.ReturnDate == null) ?
                                                              ((int)System.Data.Entity.DbFunctions.DiffDays(loan.BorrowDate, DateTime.Now)).ToString() + "  ...out" :
                                                              ((int)System.Data.Entity.DbFunctions.DiffDays(loan.BorrowDate, loan.ReturnDate)).ToString()
                                                      };

           if (reportList != null)
            {
                int pageSize = 17;
                int pageNumber = (page ?? 1);

                StudentLoansModel model = new StudentLoansModel();
                model.LibraryId = libraryId;
                model.School = school;
                model.Page = pageNumber;
                model.StudentId = (int)studentId;
                model.Student = student;
                model.NoLoansMessage = "No loans for this student in " + thisYear.ToString();
                model.StudentLoansList = reportList.ToPagedList(pageNumber, pageSize);
                return PartialView("_Student", model);

            }
            return PartialView("_SelectReport");
        }

        public ActionResult StudentSearch(string searchString, string sortOrder, string newOrder, int? page, int? libraryId, string school, bool asc = true)
        {
            if (String.IsNullOrEmpty(searchString))
            {
                //return PartialView("_SelectReport"); //baf fix this
                return Content("");
            }
            if (String.IsNullOrEmpty(newOrder))
            {
                if (String.IsNullOrEmpty(sortOrder))
                    sortOrder = "FirstName";
            }
            else
            {
                page = 1;
                if (sortOrder == newOrder)
                    asc = !asc;
                else
                    sortOrder = newOrder;
            }

            IQueryable<StudentSearchItem> reportList = from s in db.Students
                                                       where s.Library_id == (int)libraryId && s.Inactive == false && (s.FirstName.StartsWith(searchString) || s.LastName.StartsWith(searchString))
                                                       select new StudentSearchItem()
                                                       {
                                                           StudentId = s.Student_Id,
                                                           Student = s.FirstName.Trim() + " " + s.LastName.Trim(),
                                                           FirstName = s.FirstName,
                                                           LastName = s.LastName,
                                                           ClassId = s.Classes_Id,
                                                           ClassName = s.Class.ClassDesc
                                                       };
            if (reportList != null && reportList.Count() > 0)
            {
                switch (sortOrder)
                {
                    case "LastName":
                        if (asc)
                            reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                        else
                            reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                        break;

                    case "Class":
                        if (asc)
                            reportList = reportList.OrderBy(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                        else
                            reportList = reportList.OrderByDescending(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                        break;

                    default:
                        if (asc)
                            reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                        else
                            reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                        break;
                }
                int pageSize = 12;
                int pageNumber = (page ?? 1);
                StudentSearchModel model = new StudentSearchModel();
                model.School = school;
                model.LibraryId = (int)libraryId;
                model.Page = pageNumber;
                model.StudentSearchList = reportList.ToPagedList(pageNumber, pageSize);
                model.SearchString = searchString;

                return PartialView("_StudentSearch", model);
           }
           return PartialView("_SelectReport"); //baf fix this
        }

        public ActionResult PrintOverdueLetters(int days, string school, int libraryId)
        {
            string letterUpper = String.Empty, letterLower = String.Empty, signedBy = String.Empty, position = String.Empty, pageSize = String.Empty, schoolName = String.Empty;
            bool sizeA4 = false, docClose = false;

            if (days > 0)
            {
               var query1 = (from l in db.Libraries
                              where l.Library_Id == libraryId
                              select l).FirstOrDefault();

                if (query1 != null)
                {
                    letterUpper = query1.StLetter1;
                    letterLower = query1.StLetter2;
                    signedBy = query1.StLetterName;
                    position = query1.StLetterPosition;
                    sizeA4 = query1.StLetterSize == "A4";
                }
                float postScriptPointsPerMilimeter = 2.834645669f;
                Document doc = new Document();
                MemoryStream stream = new MemoryStream();
                PdfWriter writer = PdfWriter.GetInstance(doc, stream);
                try
                {
                    doc.Open();
                    docClose = true;

                    var query = from loan in db.Loans.Where(d => d.ReturnDate == null && (int)System.Data.Entity.DbFunctions.DiffDays(d.BorrowDate, DateTime.Now) >= days && d.Student.Library_id == libraryId)
                                join sc in db.StudentContacts on loan.Student_Id equals sc.Student_Id into br
                                from x in br.DefaultIfEmpty().Take(1)
                                orderby loan.Student.Class.ClassDesc, loan.Student_Id

                                select new
                                {
                                    loan.Student_Id,
                                    Title = loan.Product.Title,
                                    Isbn = loan.Product.Isbn,
                                    StudentFirstName = loan.Student.FirstName,
                                    StudentLastName = loan.Student.LastName,
                                    Salutation = x.Parent.ContactTitle.Salutation,
                                    TitleId = x.Parent.Title_Id,
                                    ParentFirstName = x.Parent.FirstName,
                                    ParentLastName = x.Parent.LastName,
                                    ClassDesc = loan.Student.Class.ClassDesc,
                                    Borrowed = loan.BorrowDate
                                };

                    if (query != null)
                    {
                        Font times10 = FontFactory.GetFont("Times Roman");
                        times10.Size = 10;
                        times10.SetStyle("Italic");

                        List<Overdues> overdues = new List<Overdues>();
                        // ProfileBase userProfile = ProfileBase.Create(User.Identity.Name, true);  ///baf XXXX
                        // string schoolName = "Mt Druitt Public School";//userProfile.GetPropertyValue("LibraryName").ToString(); //baf XXXX
                        string text1 = letterUpper;//tbLetterText1.Text;
                        //string userName = tbSignedBy.Text;
                        //string position = tbPosition.Text;
                        string line1 = "", line2 = "", line3 = "", salutation = "";
                        string line4 = letterLower;//tbLetterText2.Text;
                        float[] widths = new float[] { 1f, 2f, 1f };
                        int studentId = 0;
                        //bool sizeA4 = (rblLetterSize.SelectedIndex == 1);
                        int count = 1;

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
                            crest.ScaleToFit(100f, 120f);
                            crest.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_RIGHT;
                        }

                        foreach (var c in query)
                        {
                            if (studentId == c.Student_Id)
                            {
                                overdues.Add(new Overdues(c.Title, c.Isbn, String.Format("{0:dd/MM/yyyy}", c.Borrowed), 0)); //c.Borrowed.ToShortDateString()
                            }
                            else
                            {
                                if (studentId > 0)
                                {
                                    if (query2.Image_Id > 0)
                                        doc.Add(crest);
                                    Paragraph para = new Paragraph();

                                    // para.IndentationLeft = 150f * postScriptPointsPerMilimeter;
                                    para.Add(line1 + Environment.NewLine + line2 + Environment.NewLine + DateTime.Now.ToShortDateString());
                                    doc.Add(para);

                                    //para.Clear();
                                    //para.SpacingBefore = 0f;
                                    //para.Add(line2);
                                    //doc.Add(para);

                                    //para.Clear();
                                    //para.Add(DateTime.Now.ToShortDateString() + Environment.NewLine + Environment.NewLine);
                                    //doc.Add(para);

                                    //need spaces here
                                    para.Clear();
                                    para.IndentationLeft = 0f;
                                    para.SpacingBefore = 15f;
                                    para.Add(salutation + Environment.NewLine);
                                    doc.Add(para);

                                    para.Clear();
                                    para.SpacingBefore = 10f;
                                    para.Add(line3);
                                    doc.Add(para);

                                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                                    PdfPTable books = new PdfPTable(3);
                                    books.TotalWidth = 150f * postScriptPointsPerMilimeter;
                                    books.SetWidths(widths);
                                    books.LockedWidth = true;
                                    books.HorizontalAlignment = 1;
                                    books.SpacingAfter = 15f;
                                    books.SpacingBefore = 15f;


                                    foreach (var o in overdues)
                                    {
                                        PdfPCell cell1 = new PdfPCell();
                                        // cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                        cell1.Border = 0;
                                        cell1.HorizontalAlignment = 0;
                                        cell1.Indent = 1;
                                        cell1.Phrase = new Phrase(o.Isbn, times10);
                                        books.AddCell(cell1);

                                        PdfPCell cell2 = new PdfPCell();
                                        //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                        cell2.Border = 0;
                                        cell2.HorizontalAlignment = 0;
                                        cell2.Indent = 1;
                                        cell2.Phrase = new Phrase(o.Title, times10);
                                        books.AddCell(cell2);

                                        PdfPCell cell3 = new PdfPCell();
                                        //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                        cell3.Border = 0;
                                        cell3.HorizontalAlignment = 2;
                                        cell3.Indent = 1;
                                        cell3.Phrase = new Phrase(o.BorrowDate, times10);
                                        books.AddCell(cell3);
                                    }
                                    doc.Add(books);

                                    para.Clear();
                                    para.SpacingBefore = 0f;
                                    para.Add(line4 + Environment.NewLine + Environment.NewLine);
                                    doc.Add(para);

                                    para.Clear();
                                    para.SpacingBefore = 15f;
                                    para.Add(signedBy);
                                    doc.Add(para);

                                    para.Clear();
                                    para.SpacingBefore = 0f;
                                    para.Add(position);
                                    doc.Add(para);

                                    para.Clear();
                                    para.Add(school);

                                    if (sizeA4 || count % 2 == 0)
                                    {
                                        doc.Add(para);
                                        doc.NewPage();
                                    }
                                    else
                                    {
                                        para.SpacingBefore = 20f;
                                        doc.Add(para);
                                    }
                                    count++;
                                }

                                //start next student
                                overdues.Clear();
                                line1 = c.StudentFirstName.TrimEnd() + " " + c.StudentLastName.TrimEnd();
                                line2 = "Class: " + c.ClassDesc;
                                if (c.TitleId > 1 && !string.IsNullOrEmpty(c.ParentLastName))
                                    salutation = "Dear " + c.Salutation.TrimEnd() + " " + c.ParentLastName.TrimEnd() + ",";
                                else if (!string.IsNullOrEmpty(c.ParentFirstName) && !string.IsNullOrEmpty(c.ParentLastName))
                                    salutation = "Dear " + c.ParentFirstName.TrimEnd() + " " + c.ParentLastName.TrimEnd() + ",";
                                else
                                    salutation = "Dear Mr/Mrs " + c.StudentLastName.TrimEnd() + ",";

                                // line3 = "Our records show that your child " + c.StudentFirstName.TrimEnd() + " has not returned the following books borrowed from the Home Reading program:";
                                if (!string.IsNullOrEmpty(c.StudentFirstName))
                                    line3 = text1.Replace("FIRSTNAME", c.StudentFirstName.TrimEnd());
                                if (!string.IsNullOrEmpty(c.StudentLastName))
                                    line3 = line3.Replace("LASTNAME", c.StudentLastName.TrimEnd());
                                if (!string.IsNullOrEmpty(c.ClassDesc))
                                    line3 = line3.Replace("CLASSNAME", c.ClassDesc.TrimEnd());
                                overdues.Add(new Overdues("Isbn", "Title", "Borrowed on", 0));
                                //overdues.Add(new Overdue(c.Isbn.TrimEnd(), c.Title.TrimEnd(), c.Borrowed.ToShortDateString()));
                                overdues.Add(new Overdues(c.Isbn.TrimEnd(), c.Title.TrimEnd(), String.Format("{0:dd/MM/yyyy}", c.Borrowed), 0));

                                //line4 = "Please return them immediately. Unreturned books will incur a charge of $7.00 each, payable at the office.";

                                studentId = c.Student_Id;
                            }
                        }
                    }

                    doc.Close();
                    docClose = false;
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=OverdueLoansLetters.pdf");

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
                    if (docClose)
                        doc.Close();
                }
            }
            return Content("");
        }

        public ActionResult EditLettersHome(int days, int libraryId, string school, string sortOrder, int? page = 1, bool asc = true)
        {
            var query = (from l in db.Libraries
                         where l.Library_Id == libraryId
                         select l).FirstOrDefault();

            LettersHome report = new LettersHome();
            if (query != null)
            {
                report.StLetter1 = query.StLetter1;
                report.StLetter2 = query.StLetter2;
                report.StLetterName = query.StLetterName;
                report.StLetterPosition = query.StLetterPosition;
                report.StLetterSize = query.StLetterSize;
                report.A4Page = query.StLetterSize == "A4";
                var query2 = (from r in db.LibImages
                              where r.Library_Id == libraryId
                              select r).FirstOrDefault();

                if (query2 != null)
                {
                    report.Crest_Id = query2.Image_Id;
                    report.Crest = query2.Image;
                }
                else
                {
                    report.Crest_Id = 0;
                }
                report.School = school;
                report.LibraryId = libraryId;
                report.SortOrder = sortOrder;
                report.Page = page ?? 1;
                report.Ascending = asc;
                report.Days = days;
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
           
            return View(report);

        }

        [HttpPost, ActionName("EditLettersHome")]
        [ValidateAntiForgeryToken]
        public ActionResult EditLetterHomePost(LettersHome model)
        {
            
             var libraryToUpdate = db.Libraries.Find(model.LibraryId);

            if (TryUpdateModel(libraryToUpdate, "",
               new string[] { "StLetter1", "StLetter2", "StLetterName", "StLetterPosition" })) //, "StPageSize"
            {
                try
                {

                    db.Entry(libraryToUpdate).State = EntityState.Modified;
                    if (model.A4Page)
                        libraryToUpdate.StLetterSize = "A4";
                    else
                        libraryToUpdate.StLetterSize = "A5";
                    if (Request.Files["file"] != null && Request.Files["file"].ContentLength > 0)
                    {
                        if (Request.Files["file"].ContentLength < 1024 * 1024 && Request.Files["file"].ContentType == "image/jpeg")
                        {
                            byte[] Image;
                            using (var binaryReader = new BinaryReader(Request.Files["file"].InputStream))
                            {
                                Image = binaryReader.ReadBytes(Request.Files["file"].ContentLength);
                                if (Image != null)
                                {
                                    var imageQuery = (from j in db.LibImages
                                                      where j.Library_Id == model.LibraryId
                                                      select j).FirstOrDefault();

                                    if (imageQuery != null && imageQuery.Image_Id > 0)
                                    {
                                        imageQuery.Image = Image;
                                    }
                                    else
                                    {
                                        LibImage pi = new LibImage();
                                        pi.Library_Id = model.LibraryId;
                                        pi.Image = Image;
                                        db.LibImages.Add(pi);
                                    }
                                }
                            }

                        }
                    }
                    db.SaveChanges();
                    //return View(student);
                    return RedirectToAction("Index", new {school = model.School, libraryId = model.LibraryId, sortOrder = model.SortOrder, asc = model.Ascending, page = model.Page, days = model.Days, reportType = 1 });

                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            return View(libraryToUpdate);
        }

        public ActionResult OverdueLoansReport(int days, string sortOrder, int libraryId, string school, bool asc = true)
        {
            bool docClose = false;
            string pdfFileName = string.Empty;

            if (days > 0)
            {
                string path = Server.MapPath("");
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
                    docClose = true;

                    IQueryable<OverdueItem> reportList = from l in db.Loans.Where(d => d.ReturnDate == null
                                                                    && System.Data.Entity.DbFunctions.DiffDays(d.BorrowDate, DateTime.Now) >= days
                                                                    && d.Student.Library_id == libraryId)
                                                         select new OverdueItem()
                                                         {
                                                             Title = l.Product.Title,
                                                             Student_Id = l.Student_Id,
                                                             FirstName = l.Student.FirstName,
                                                             LastName = l.Student.LastName,
                                                             FullName = l.Student.FirstName.Trim() + " " + l.Student.LastName.Trim(),
                                                             BorrowDate = l.BorrowDate,
                                                             Days = (int)System.Data.Entity.DbFunctions.DiffDays(l.BorrowDate, DateTime.Now),
                                                             ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                                             ClassName = l.Student.Class.ClassDesc,
                                                         };
                    if (reportList != null)
                    {
                        switch (sortOrder)
                        {
                            case "FirstName":
                                if (asc)
                                    reportList = reportList.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
                                else
                                    reportList = reportList.OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName);
                                break;
                            case "LastName":
                                if (asc)
                                    reportList = reportList.OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
                                else
                                    reportList = reportList.OrderByDescending(x => x.LastName).ThenBy(x => x.FirstName);
                                break;
                            case "ClassName":
                                if (asc)
                                    reportList = reportList.OrderBy(x => x.ClassName).OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
                                else
                                    reportList = reportList.OrderByDescending(x => x.ClassName).OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
                                break;
                            default:
                                if (asc)
                                    reportList = reportList.OrderBy(x => x.Days).OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
                                else
                                    reportList = reportList.OrderByDescending(x => x.Days).OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
                                break;
                        }
                        Font times10 = FontFactory.GetFont("Times Roman");
                        times10.Size = 10;
                        //times10.SetStyle("Italic");
                        Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                        Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                        float[] widths = new float[] { 3f, 2f, 4f, 2f, 1f };
                        int count = 0, studentId = 0;
                        int pageSize = 30;
                        int pageNo = 0;

                        float height1 = 7f;
                        iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                        PdfPTable books = new PdfPTable(5);
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
                                para.Add(school + " Home Reading");
                                para.Font = times12;
                                para.Alignment = Element.ALIGN_CENTER;
                                doc.Add(para);

                                para.Clear();
                                para.Alignment = Element.ALIGN_CENTER;
                                para.Add("Loans Overdue by " + days + " days");
                                para.Font = times12;
                                doc.Add(para);

                                para.Clear();
                                para.SpacingAfter = 10f;
                                para.Alignment = Element.ALIGN_CENTER;
                                para.Font = times10;
                                para.Add(DateTime.Now.ToShortDateString());
                                doc.Add(para);

                                PdfPCell cellH1 = new PdfPCell();
                                cellH1.Border = 0;
                                cellH1.HorizontalAlignment = Element.ALIGN_LEFT;
                                cellH1.Indent = 1;
                                cellH1.Phrase = new Phrase("Student", times11);
                                books.AddCell(cellH1);

                                PdfPCell cellH2 = new PdfPCell();
                                //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH2.Border = 0;
                                cellH2.HorizontalAlignment = Element.ALIGN_LEFT;
                                cellH2.Indent = 1;
                                cellH2.Phrase = new Phrase("Class", times11);
                                books.AddCell(cellH2);

                                PdfPCell cellH3 = new PdfPCell();
                                //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH3.Border = 0;
                                cellH3.HorizontalAlignment = Element.ALIGN_LEFT;
                                cellH3.Indent = 1;
                                cellH3.Phrase = new Phrase("Book", times11);
                                books.AddCell(cellH3);

                                PdfPCell cellH4 = new PdfPCell();
                                //cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH4.Border = 0;
                                cellH4.HorizontalAlignment = Element.ALIGN_LEFT;
                                cellH4.Indent = 1;
                                cellH4.Phrase = new Phrase("Date", times11);
                                books.AddCell(cellH4);

                                PdfPCell cellH5 = new PdfPCell();
                                //cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                                cellH5.Border = 0;
                                cellH5.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellH5.Indent = 1;
                                cellH5.Phrase = new Phrase("Days", times11);
                                books.AddCell(cellH5);
                            }

                            PdfPCell cell1 = new PdfPCell();
                            cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell1.Border = 0;
                            cell1.HorizontalAlignment = Element.ALIGN_LEFT;
                            cell1.Indent = 1;
                            cell1.Phrase = new Phrase(c.FullName, times10);
                            books.AddCell(cell1);

                            PdfPCell cell2 = new PdfPCell();
                            cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell2.Border = 0;
                            cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                            cell2.Indent = 1;
                            //cell2.NoWrap = true;
                            cell2.Phrase = new Phrase((studentId == c.Student_Id) ? "" : c.ClassName, times10);
                            books.AddCell(cell2);

                            PdfPCell cell3 = new PdfPCell();
                            cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell3.Border = 0;
                            cell3.HorizontalAlignment = Element.ALIGN_LEFT;
                            cell3.Indent = 1;
                            //cell3.NoWrap = true;
                            cell3.Phrase = new Phrase(c.Title, times10);
                            books.AddCell(cell3);

                            PdfPCell cell4 = new PdfPCell();
                            cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell4.Border = 0;
                            cell4.HorizontalAlignment = Element.ALIGN_LEFT;
                            cell4.Indent = 1;
                            cell4.Phrase = new Phrase(c.BorrowDate.ToShortDateString(), times10);
                            books.AddCell(cell4);

                            PdfPCell cell5 = new PdfPCell();
                            cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cell5.Border = 0;
                            cell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cell5.Indent = 1;
                            cell5.Phrase = new Phrase(c.Days.ToString(), times10);
                            books.AddCell(cell5);

                            count++;
                            studentId = c.Student_Id;
                        }
                        if (count > 0)
                        {
                            Paragraph para = new Paragraph();
                            doc.Add(books);
                            para.Add("Page " + pageNo.ToString());
                            doc.Add(para);
                        }
                        doc.Close();
                        docClose = false;
                        byte[] file = stream.ToArray();
                        MemoryStream output = new MemoryStream();
                        output.Write(file, 0, file.Length);
                        output.Position = 0;

                        HttpContext.Response.AddHeader("content-disposition", "attachment; filename=OverdueLoansReport.pdf");

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
                    if (docClose)
                        doc.Close();
                }
            }
            return Content("");
        }

        public ActionResult ActivityReport(bool borrowing, DateTime activityDate, string sortOrder, int libraryId, string school, bool asc = true)
        {
            bool docClose = false;
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
                docClose = true;
                IQueryable<BorrowItem> reportList;
                if (borrowing)
                    reportList = from l in db.Loans.Where(d => System.Data.Entity.DbFunctions.TruncateTime(d.BorrowDate) == System.Data.Entity.DbFunctions.TruncateTime(activityDate)
                                                                       && d.Student.Library_id == libraryId)
                                 select new BorrowItem()
                                 {
                                     Title = l.Product.Title,
                                     Student_Id = l.Student_Id,
                                     FirstName = l.Student.FirstName,
                                     LastName = l.Student.LastName,
                                     FullName = l.Student.FirstName.Trim() + " " + l.Student.LastName.Trim(),
                                     ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                     ClassName = l.Student.Class.ClassDesc,
                                 };
                else
                    reportList = from l in db.Loans.Where(d => System.Data.Entity.DbFunctions.TruncateTime(d.ReturnDate) == System.Data.Entity.DbFunctions.TruncateTime(activityDate)
                                                                          && d.Student.Library_id == libraryId)
                                 select new BorrowItem()
                                 {
                                     Title = l.Product.Title,
                                     Student_Id = l.Student_Id,
                                     FirstName = l.Student.FirstName,
                                     LastName = l.Student.LastName,
                                     FullName = l.Student.FirstName.Trim() + " " + l.Student.LastName.Trim(),
                                     ClassId = l.Student.Classes_Id == null ? 0 : (int)l.Student.Classes_Id,
                                     ClassName = l.Student.Class.ClassDesc,
                                 };

                if (reportList != null)
                {
                    switch (sortOrder)
                    {
                        case "LastName":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                            else
                                reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                            break;

                        case "ClassName":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            else
                                reportList = reportList.OrderByDescending(s => s.ClassName).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            break;

                        case "Title":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.Title);
                            else
                                reportList = reportList.OrderByDescending(s => s.Title);
                            break;

                        default:
                            if (asc)
                                reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                            else
                                reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                            break;
                    }
                    Font times10 = FontFactory.GetFont("Times Roman");
                    times10.Size = 10;
                    //times10.SetStyle("Italic");
                    Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                    Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                    float[] widths = new float[] { 4f, 2f, 6f };

                    int count = 0, studentId = 0;
                    int pageSize = 30;
                    int pageNo = 0;

                    float height1 = 7f;
                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable books = new PdfPTable(3);
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
                            para.Add(school + " Home Reading");
                            para.Font = times12;
                            para.Alignment = Element.ALIGN_CENTER;
                            doc.Add(para);

                            para.Clear();
                            para.Alignment = Element.ALIGN_CENTER;
                            if (borrowing)
                                para.Add("Borrowings on " + activityDate.DayOfWeek + " " + activityDate.ToString("dd MMM yyyy"));
                            else
                                para.Add("Loans Returned on " + activityDate.DayOfWeek + " " + activityDate.ToString("dd MMM yyyy")); ;
                            para.Font = times12;
                            doc.Add(para);

                            para.Clear();
                            para.SpacingAfter = 10f;
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Font = times10;
                            para.Add(DateTime.Now.ToShortDateString());
                            doc.Add(para);

                            PdfPCell cellH1 = new PdfPCell();
                            cellH1.Border = 0;
                            cellH1.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH1.Indent = 1;
                            cellH1.Phrase = new Phrase("Student", times11);
                            books.AddCell(cellH1);

                            PdfPCell cellH2 = new PdfPCell();
                            //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH2.Border = 0;
                            cellH2.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH2.Indent = 1;
                            cellH2.Phrase = new Phrase("Class", times11);
                            books.AddCell(cellH2);

                            PdfPCell cellH3 = new PdfPCell();
                            //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH3.Border = 0;
                            cellH3.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH3.Indent = 1;
                            cellH3.Phrase = new Phrase("Book", times11);
                            books.AddCell(cellH3);
                        }

                        PdfPCell cell1 = new PdfPCell();
                        cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell1.Border = 0;
                        cell1.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell1.Indent = 1;
                        cell1.Phrase = new Phrase((studentId == c.Student_Id) ? "" : c.FullName, times10);
                        books.AddCell(cell1);

                        PdfPCell cell2 = new PdfPCell();
                        cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell2.Border = 0;
                        cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell2.Indent = 1;
                        cell2.Phrase = new Phrase((studentId == c.Student_Id) ? "" : c.ClassName, times10);
                        books.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell3.Indent = 1;
                        cell3.Phrase = new Phrase(c.Title, times10);
                        books.AddCell(cell3);

                        count++;
                        studentId = c.Student_Id;
                    }
                    if (count > 0)
                    {
                        Paragraph para = new Paragraph();
                        doc.Add(books);
                        para.Add("Page " + pageNo.ToString());
                        doc.Add(para);
                    }

                    doc.Close();
                    docClose = false;
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=ActivityReport.pdf");

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
                if (docClose)
                    doc.Close();
            }

            return Content(""); ;
        }

        public ActionResult SchoolSummaryReport(string sortOrder, int libraryId, string school, bool asc = true)
        {
            bool docClose = false;
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
                docClose = true;

                DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
                string year = DateTime.Now.Year.ToString();

                IQueryable<SchoolLoansItem> reportList = from c in db.Classes.Where(s => !s.Obsolete && s.Library_Id == libraryId && s.Students.Count > 0)

                                                         join t in db.Teachers on c.Teacher_Id equals t.Teacher_Id into t2
                                                         from t3 in t2.DefaultIfEmpty()

                                                         join b2 in
                                                             (from b in db.BooksReads
                                                              group b by b.Student.Classes_Id into grp
                                                              let total = grp.Sum(b => b.BooksRead1)
                                                              select new { Class_Id = grp.Key, BooksRead = total }) on c.Classes_Id equals b2.Class_Id into b3
                                                         from x in b3.DefaultIfEmpty()

                                                         join l2 in
                                                             (from l in db.Loans.Where(d => d.BorrowDate >= startDate)

                                                              group l by l.Student.Classes_Id into grp
                                                              let count = grp.Count()
                                                              select new { Class_Id = grp.Key, BooksRead = count }) on c.Classes_Id equals l2.Class_Id into l3
                                                         from y in l3.DefaultIfEmpty()

                                                         join s2 in
                                                             (from s in db.Students
                                                              group s by s.Classes_Id into grp
                                                              let maxLevel = grp.Max(l => l.Levels_Id)//Level.ReadLevel //baf important need to work this out when I add different reading level types
                                                              let minLevel = grp.Min(l => l.Levels_Id)//Level.ReadLevel
                                                              let avgLevel = Math.Round(grp.Average(l => l.Level.Levels_Id), 2)
                                                              select new { Class_Id = grp.Key, MaxLevel = maxLevel, MinLevel = minLevel, AvgLevel = avgLevel }) on c.Classes_Id equals s2.Class_Id into s3

                                                         from z in s3.DefaultIfEmpty()

                                                         select new SchoolLoansItem()
                                                         {
                                                             ClassId = c.Classes_Id,
                                                             ClassName = c.ClassDesc,
                                                             //Teacher = (t3.FirstName == null) ? string.Empty : t3.FirstName + " " + t3.LastName,
                                                             Teacher = t3.FirstName + " " + t3.LastName,
                                                             BooksRead = ((y.BooksRead == null) ? 0 : y.BooksRead) + ((x.BooksRead == null) ? 0 : x.BooksRead),//+ y.BooksRead + x.BooksRead,
                                                             MaxLevel = ((z.MaxLevel == null) ? 0 : (int)z.MaxLevel),
                                                             MinLevel = ((z.MinLevel == null) ? 0 : (int)z.MinLevel),
                                                             AvgLevel = ((z.AvgLevel == null) ? 0 : (float)z.AvgLevel)
                                                         };

                if (reportList != null)
                {
                    switch (sortOrder)
                    {
                        case "Teacher":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.Teacher);
                            else
                                reportList = reportList.OrderByDescending(s => s.Teacher);
                            break;

                        case "BooksRead":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.BooksRead).ThenBy(n => n.ClassName);
                            else
                                reportList = reportList.OrderByDescending(s => s.BooksRead).ThenBy(n => n.ClassName);
                            break;

                        case "AvgLevel":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.AvgLevel).ThenBy(n => n.ClassName);
                            else
                                reportList = reportList.OrderByDescending(s => s.AvgLevel).ThenBy(n => n.ClassName);
                            break;

                        default:
                            if (asc)
                                reportList = reportList.OrderBy(s => s.ClassName);
                            else
                                reportList = reportList.OrderByDescending(s => s.ClassName);
                            break;
                    }
                    Font times10 = FontFactory.GetFont("Times Roman");
                    times10.Size = 10;
                    //times10.SetStyle("Italic");
                    Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                    Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                    float[] widths = new float[] { 2f, 3.5f, 2f, 1.5f, 1.5f, 1.5f };

                    int count = 0;
                    int pageSize = 30;
                    int pageNo = 0;

                    float height1 = 7f;
                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable books = new PdfPTable(6);
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
                            para.Add(school + " Home Reading");
                            para.Font = times12;
                            para.Alignment = Element.ALIGN_CENTER;
                            doc.Add(para);

                            para.Clear();
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Add("Borrowing by Class for " + startDate.Year);
                            para.Font = times12;
                            doc.Add(para);

                            para.Clear();
                            para.SpacingAfter = 10f;
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Font = times10;
                            para.Add(DateTime.Now.ToShortDateString());
                            doc.Add(para);

                            PdfPCell cellH1 = new PdfPCell();
                            cellH1.Border = 0;
                            cellH1.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH1.Indent = 1;
                            cellH1.Phrase = new Phrase("Class", times11);
                            books.AddCell(cellH1);

                            PdfPCell cellH2 = new PdfPCell();
                            //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH2.Border = 0;
                            cellH2.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH2.Indent = 1;
                            cellH2.Phrase = new Phrase("Teacher", times11);
                            books.AddCell(cellH2);

                            PdfPCell cellH3 = new PdfPCell();
                            //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH3.Border = 0;
                            cellH3.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH3.Indent = 1;
                            cellH3.Phrase = new Phrase("Books", times11);
                            books.AddCell(cellH3);

                            PdfPCell cellH4 = new PdfPCell();
                            //cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH4.Border = 0;
                            cellH4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH4.Indent = 1;
                            cellH4.Phrase = new Phrase("Max Level", times11);
                            books.AddCell(cellH4);

                            PdfPCell cellH5 = new PdfPCell();
                            //cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH5.Border = 0;
                            cellH5.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH5.Indent = 1;
                            cellH5.Phrase = new Phrase("Min Level", times11);
                            books.AddCell(cellH5);

                            PdfPCell cellH6 = new PdfPCell();
                            //cell6.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH6.Border = 0;
                            cellH6.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH6.Indent = 1;
                            cellH6.Phrase = new Phrase("Avg Level", times11);
                            books.AddCell(cellH6);
                        }

                        PdfPCell cell1 = new PdfPCell();
                        cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell1.Border = 0;
                        cell1.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell1.Indent = 1;
                        //cell1.NoWrap = true;
                        cell1.Phrase = new Phrase(c.ClassName, times10);
                        books.AddCell(cell1);

                        PdfPCell cell2 = new PdfPCell();
                        cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell2.Border = 0;
                        cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell2.Indent = 1;
                        //cell2.NoWrap = true;
                        cell2.Phrase = new Phrase(c.Teacher, times10);
                        books.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell3.Indent = 1;
                        //cell3.NoWrap = true;
                        cell3.Phrase = new Phrase(c.BooksRead.ToString(), times10);
                        books.AddCell(cell3);

                        PdfPCell cell4 = new PdfPCell();
                        cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell4.Border = 0;
                        cell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell4.Indent = 1;
                        cell4.Phrase = new Phrase(c.MaxLevel.ToString(), times10);
                        books.AddCell(cell4);

                        PdfPCell cell5 = new PdfPCell();
                        cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell5.Border = 0;
                        cell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell5.Indent = 1;
                        cell5.Phrase = new Phrase(c.MinLevel.ToString(), times10);
                        books.AddCell(cell5);

                        PdfPCell cell6 = new PdfPCell();
                        cell6.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell6.Border = 0;
                        cell6.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell6.Indent = 1;
                        cell6.Phrase = new Phrase(c.AvgLevel.ToString(), times10);
                        books.AddCell(cell6);
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
                    docClose = false;
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
                if (docClose)
                    doc.Close();
            }

            return Content("");
        }

        public ActionResult ClassSummaryReport(int classId, string className, int libraryId, string school, string sortOrder, bool asc = true)
        {
            bool closeDoc = false;
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
                closeDoc = true;
                DateTime startDate = Convert.ToDateTime("01/01/" + DateTime.Now.Year.ToString());
                string year = DateTime.Now.Year.ToString();

                IQueryable<ClassLoansItem> reportList = from s in db.Students.Where(s => s.Classes_Id == classId)
                                                        join b in db.BooksReads.Where(b => b.ForYear == year) on s.Student_Id equals b.Student_Id into br
                                                        from x in br.DefaultIfEmpty()
                                                        join rl in db.Levels on s.Levels_Id equals rl.Levels_Id into rlc
                                                        from y in rlc.DefaultIfEmpty()

                                                        join l2 in
                                                            (from l in db.Loans.Where(d => d.BorrowDate >= startDate && d.Student.Classes_Id == classId)
                                                             group l by l.Student_Id into grp
                                                             let count = grp.Count()
                                                             select new { Student_Id = grp.Key, count }) on s.Student_Id equals l2.Student_Id into l3
                                                        from z in l3.DefaultIfEmpty()

                                                        select new ClassLoansItem()
                                                        {
                                                            Student_Id = s.Student_Id,
                                                            FullName = s.FirstName.Trim() + " " + s.LastName,
                                                            FirstName = s.FirstName,
                                                            LastName = s.LastName,
                                                            ReadLevel = y.ReadLevel,
                                                            BooksRead = ((int?)z.count ?? 0) + ((int?)x.BooksRead1 ?? 0)
                                                        };
                if (reportList != null)
                {
                    switch (sortOrder)
                    {
                        case "LastName":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                            else
                                reportList = reportList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                            break;

                        case "Books":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            else
                                reportList = reportList.OrderByDescending(s => s.BooksRead).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            break;

                        case "Level":
                            if (asc)
                                reportList = reportList.OrderBy(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            else
                                reportList = reportList.OrderByDescending(s => s.ReadLevel).ThenBy(n => n.FirstName).ThenBy(n => n.LastName);
                            break;

                        default:
                            if (asc)
                                reportList = reportList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                            else
                                reportList = reportList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                            break;
                    }
                    Font times10 = FontFactory.GetFont("Times Roman");
                    times10.Size = 10;
                    //times10.SetStyle("Italic");
                    Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                    Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                    float[] widths = new float[] { 2f, 1f, 1f };
                    int count = 0, studentId = 0;
                    int pageSize = 30;
                    int pageNo = 0;
                    float height1 = 7f;
                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable books = new PdfPTable(3);
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
                            para.Add(school + " Home Reading");
                            para.Font = times12;
                            para.Alignment = Element.ALIGN_CENTER;
                            doc.Add(para);

                            para.Clear();
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Add("Summary for Class " + className);
                            para.Font = times12;
                            doc.Add(para);

                            para.Clear();
                            para.SpacingAfter = 10f;
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Font = times10;
                            para.Add(DateTime.Now.ToShortDateString());
                            doc.Add(para);

                            PdfPCell cellH1 = new PdfPCell();
                            cellH1.Border = 0;
                            cellH1.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH1.Indent = 1;
                            cellH1.Phrase = new Phrase("Student", times11);
                            books.AddCell(cellH1);

                            PdfPCell cellH2 = new PdfPCell();
                            //cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH2.Border = 0;
                            cellH2.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH2.Indent = 1;
                            cellH2.Phrase = new Phrase("Reading Level", times11);
                            books.AddCell(cellH2);

                            PdfPCell cellH3 = new PdfPCell();
                            //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH3.Border = 0;
                            cellH3.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH3.Indent = 1;
                            cellH3.Phrase = new Phrase("Books Read", times11);
                            books.AddCell(cellH3);
                        }

                        PdfPCell cell1 = new PdfPCell();
                        cell1.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell1.Border = 0;
                        cell1.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell1.Indent = 1;
                        cell1.Phrase = new Phrase(c.FullName, times10);
                        books.AddCell(cell1);

                        PdfPCell cell2 = new PdfPCell();
                        cell2.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell2.Border = 0;
                        cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell2.Indent = 1;
                        cell2.Phrase = new Phrase(c.ReadLevel, times10);
                        books.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell3.Indent = 1;
                        cell3.Phrase = new Phrase(c.BooksRead.ToString(), times10);
                        books.AddCell(cell3);

                        count++;
                        studentId = c.Student_Id;
                    }
                    if (count > 0)
                    {
                        Paragraph para = new Paragraph();
                        doc.Add(books);
                        para.Add("Page " + pageNo.ToString());
                        doc.Add(para);
                    }

                    doc.Close();
                    closeDoc = false;
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=ClassSummaryReport.pdf");

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
                if (closeDoc)
                    doc.Close();
            }
            return Content("");
        }

        public ActionResult StudentBorrowingReport(int studentId, string student, string sortOrder, int year, int libraryId, string school)
        {
            bool docClose = false;
            string pdfFileName = string.Empty;
            string path = Server.MapPath("");
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
                docClose = true;

                DateTime startDate = Convert.ToDateTime("01/01/" + year);
                DateTime endDate = Convert.ToDateTime("31/12/" + year);

                IQueryable<StudentLoansItem> reportList = from loan in db.Loans
                                                          where loan.Student_Id == studentId
                                                            && System.Data.Entity.DbFunctions.TruncateTime(loan.BorrowDate) >= System.Data.Entity.DbFunctions.TruncateTime(startDate)
                                                            && System.Data.Entity.DbFunctions.TruncateTime(loan.BorrowDate) <= System.Data.Entity.DbFunctions.TruncateTime(endDate)
                                                          select new StudentLoansItem()
                                                          {
                                                              Title = loan.Product.Title,
                                                              ReadLevel = loan.Product.Level.ReadLevel,
                                                              BorrowDate = loan.BorrowDate,
                                                              DaysOut = (loan.ReturnDate == null) ?
                                                                  ((int)System.Data.Entity.DbFunctions.DiffDays(loan.BorrowDate, DateTime.Now)).ToString() + "  ...out" :
                                                                  ((int)System.Data.Entity.DbFunctions.DiffDays(loan.BorrowDate, loan.ReturnDate)).ToString()
                                                          };


                if (reportList != null)
                {
                    reportList = reportList.OrderByDescending(s => s.BorrowDate);

                    Font times10 = FontFactory.GetFont("Times Roman");
                    times10.Size = 10;
                    //times10.SetStyle("Italic");
                    Font times11 = FontFactory.GetFont("Times Roman", 11, Font.BOLD);
                    Font times12 = FontFactory.GetFont("Times Roman", 12, Font.BOLD);
                    float[] widths = new float[] { 3f, 1f, 1f, 1f };

                    int count = 0;
                    int pageSize = 30;
                    int pageNo = 0;

                    float height1 = 7f;
                    iTextSharp.text.List list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);
                    PdfPTable books = new PdfPTable(4);
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

                            //if (query2.Image_Id > 0)
                            //    doc.Add(crest);

                            para.Clear();
                            para.SpacingAfter = 5f;
                            para.Add(school + " Home Reading");
                            para.Font = times12;
                            para.Alignment = Element.ALIGN_CENTER;
                            doc.Add(para);

                            para.Clear();
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Add("Loans for " + student.Trim());
                            para.Font = times12;
                            doc.Add(para);

                            para.Clear();
                            para.SpacingAfter = 10f;
                            para.Alignment = Element.ALIGN_CENTER;
                            para.Font = times10;
                            para.Add(DateTime.Now.ToShortDateString());
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
                            cellH2.Phrase = new Phrase("Read Level", times11);
                            books.AddCell(cellH2);

                            PdfPCell cellH3 = new PdfPCell();
                            //cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH3.Border = 0;
                            cellH3.HorizontalAlignment = Element.ALIGN_LEFT;
                            cellH3.Indent = 1;
                            cellH3.Phrase = new Phrase("Borrowed", times11);
                            books.AddCell(cellH3);

                            PdfPCell cellH4 = new PdfPCell();
                            //cell4.FixedHeight = height1 * postScriptPointsPerMilimeter;
                            cellH4.Border = 0;
                            cellH4.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cellH4.Indent = 1;
                            cellH4.Phrase = new Phrase("Days Out", times11);
                            books.AddCell(cellH4);
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
                        cell2.Phrase = new Phrase(c.ReadLevel, times10);
                        books.AddCell(cell2);

                        PdfPCell cell3 = new PdfPCell();
                        cell3.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell3.Border = 0;
                        cell3.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell3.Indent = 1;
                        //cell3.NoWrap = true;
                        cell3.Phrase = new Phrase(c.BorrowDate.ToShortDateString(), times10);
                        books.AddCell(cell3);

                        PdfPCell cell5 = new PdfPCell();
                        cell5.FixedHeight = height1 * postScriptPointsPerMilimeter;
                        cell5.Border = 0;
                        cell5.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell5.Indent = 1;
                        cell5.Phrase = new Phrase(c.DaysOut.ToString(), times10);
                        books.AddCell(cell5);

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
                    docClose = false;
                    byte[] file = stream.ToArray();
                    MemoryStream output = new MemoryStream();
                    output.Write(file, 0, file.Length);
                    output.Position = 0;

                    HttpContext.Response.AddHeader("content-disposition", "attachment; filename=StudentBorrowingReport.pdf");

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
                if (docClose)
                    doc.Close();
            }

            return Content("");
        }
    }
}

