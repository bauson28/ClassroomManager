using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HomeReadingManager.Models;
using HomeReadingManager.ViewModels;
using System.Web.UI.HtmlControls;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.IO;


namespace HomeReadingManager.Controllers
{
    public class SubjectsController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();

        private ApplicationUserManager _userManager;

        public SubjectsController()
        {

        }

        public SubjectsController(ApplicationUserManager userManager)
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

        private IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue, bool addLine)
        {
            var items = new List<SelectListItem>();
            if (addLine)
                items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
            items.AddRange(list);
            return items;
        }

        private void LoadKlas(int newId, int oldId)
        {
            int newGrade = 0;
            if (oldId > 0)
            {
                int current = 0;
                var query = (from s in db.Subjects
                             where s.GradeReport.SchoolReportId == oldId && !s.Inactive && s.ParentId == null// s.GradeReportId == g.Id && ! s.Inactive
                             orderby s.GradeReportId, s.ParentId, s.ColOrder
                             select s).ToList();
                if (query != null)
                {
                    foreach (var item in query)
                    {
                        if (current != item.GradeReportId)
                        {
                            current = item.GradeReportId;
                            newGrade = db.GradeReports.Where(m => m.SchoolReportId == newId && m.GradeId == item.GradeReport.GradeId).FirstOrDefault().Id;
                        }
                        Subject subject = new Subject();
                        subject.Name = item.Name;
                        subject.GradeReportId = newGrade;
                        subject.OldId = item.Id;
                        subject.AssessmentId = item.AssessmentId;
                        subject.EffortId = item.EffortId;
                        subject.Inactive = false;
                        subject.ReportType = item.ReportType;
                        subject.ColOrder = item.ColOrder;
                        subject.IsTopic = item.IsTopic;
                        db.Subjects.Add(subject);
                    }
                    try
                    {
                        db.SaveChanges();
                    }

                    catch (Exception ex)
                    {
                        var bf = ex;
                        //var dels = from s in db.Subjects
                        //           where s.GradeReportId == newGrade
                        //           select s;

                        //foreach (var del in dels)
                        //{
                        //    db.Subjects.Remove(del);
                        //}
                    }
                }
            }
        }

        private void LoadSubKlas(int newId, int oldId, bool indicator)
        {
            int newGrade = 0;
            if (oldId > 0)
            {
                int current = 0;
                var query = (from s in db.Subjects
                             where s.GradeReport.SchoolReportId == oldId && !s.Inactive && s.ParentId != null && s.IsTopic == indicator// s.GradeReportId == g.Id && ! s.Inactive
                             orderby s.GradeReportId, s.ParentId, s.ColOrder
                             select s).ToList();
                if (query != null)
                {
                    foreach (var item in query)
                    {
                        if (current != item.GradeReportId)
                        {
                            current = item.GradeReportId;
                            newGrade = db.GradeReports.Where(m => m.SchoolReportId == newId && m.GradeId == item.GradeReport.GradeId).FirstOrDefault().Id;
                        }
                        Subject subject = new Subject();
                        subject.Name = item.Name;
                        subject.GradeReportId = newGrade;
                        subject.OldId = item.Id;
                        subject.AssessmentId = item.AssessmentId;
                        subject.EffortId = item.EffortId;
                        subject.Inactive = false;
                        subject.ReportType = item.ReportType;
                        subject.ColOrder = item.ColOrder;
                        subject.IsTopic = item.IsTopic;
                        if (item.ParentId != null)
                        {
                            int tempId = db.Subjects.Where(m => m.OldId == item.ParentId).FirstOrDefault().Id;
                            if (tempId > 0)
                                subject.ParentId = tempId;
                        }
                        db.Subjects.Add(subject);
                    }
                    try
                    {
                        db.SaveChanges();
                    }

                    catch (Exception ex)
                    {
                        var bf = ex;
                    }
                }
            }
        }

        private int GetLatestSchoolReportId(int libraryId)
        {
            var query = (from s in db.ReportSchools
                         where s.Library_Id == libraryId
                         orderby s.Id descending
                         select s).FirstOrDefault();
            if (query != null)
                return query.Id;

            return 0;
        }

        private int GetNextSemesterId(int id)
        {
            if (id > 0)
            {
                var query = (from s in db.Semesters
                             where s.Id > id
                             orderby s.Id
                             select s).FirstOrDefault();
                if (query != null)
                {
                    return query.Id;
                }
            }
            var query2 = (from s in db.Semesters
                          where s.Year == DateTime.Now.Year.ToString() && s.Number == (DateTime.Now.Month < 7 ? 1 : 2)
                          select s).FirstOrDefault();
            if (query2 != null)
            {
                return query2.Id;
            }
            return 0;
        }

        private int GetNextSemester(int id, int libraryId)
        {
            int newId = 0;
            int oldSemesterId = 0;
            int newSemesterId = 0;
            int reportId = 0;
            int currentId = 0;
            bool loadKlas = false;
            try
            {
                currentId = GetLatestSchoolReportId(libraryId);
                ReportSchool frontPage = new ReportSchool();
                if (id > 0 && currentId > 0)
                {
                    var report = (from s in db.ReportSchools
                                  where s.Id == id
                                  select s).FirstOrDefault();
                    if (report != null && report.Id > 0)
                    {
                        reportId = report.Id;
                        oldSemesterId = report.SemesterId;
                        frontPage.Library_Id = libraryId;
                        var reportSchoolToUpdate = db.ReportSchools.Find(currentId);
                        frontPage.SemesterId = GetNextSemesterId(reportSchoolToUpdate.SemesterId);
                        frontPage.Status = 1;
                        frontPage.UseSuperCom = false; // report.UseSuperCom;
                        frontPage.Watermark = report.Watermark;
                        frontPage.DeptLogo = report.DeptLogo;
                        frontPage.Principal = report.Principal;
                        frontPage.Position = report.Position;
                        frontPage.Introduction = report.Introduction;
                        frontPage.CommentHeader = report.CommentHeader;
                        frontPage.KlaComments = report.KlaComments;
                        db.ReportSchools.Add(frontPage);
                        //frontPage.SuperHeader = report.SuperHeader;
                        oldSemesterId = report.SemesterId;

                        db.Entry(reportSchoolToUpdate).State = EntityState.Modified;
                        reportSchoolToUpdate.Status = 2;
                        db.SaveChanges();
                        loadKlas = true;
                    }
                }
                if (reportId == 0)
                {
                    frontPage.Library_Id = libraryId;
                    frontPage.SemesterId = GetNextSemesterId(0);
                    frontPage.Status = 1;
                    frontPage.UseSuperCom = false;//true;
                    frontPage.Principal = "Insert principal's name";
                    frontPage.Position = "Principal";
                    frontPage.Introduction = "Our school reports your child's progress with written reports twice a year and through interviews or meetings. Please contact the school, if required, to discuss this report and your child's progress with the teacher. This report is for work covered in the first half of the year.";
                    frontPage.CommentHeader = "General Comment";
                    frontPage.Watermark = false;
                    frontPage.DeptLogo = true;
                    // frontPage.SuperHeader = "Principal's Comment";
                    frontPage.KlaComments = false;
                    db.ReportSchools.Add(frontPage);
                    db.SaveChanges();
                    //newId = frontPage.Id;

                    //baf xxx load default klas
                }

                newId = frontPage.Id;
                var grades = (from g in db.Grades
                              select g).ToList();
                foreach (var item in grades)
                {
                    GradeReport gradeReport = new GradeReport();
                    gradeReport.GradeId = item.Id;
                    gradeReport.SchoolReportId = newId;
                    gradeReport.Ready = false;
                    db.GradeReports.Add(gradeReport);
                }
                db.SaveChanges();


                newSemesterId = frontPage.SemesterId;

            }
            catch (DataException  /* dex  */)
            {
                //var bf = dex;
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            if (newSemesterId > 0 && loadKlas)
            {
                LoadKlas(newId, id);
                LoadSubKlas(newId, id, false);
                LoadSubKlas(newId, id, true);
            }

            return newSemesterId;
        }

        private int GetNextColOrder(int parentId, bool isTopic, int gradeReportId)
        {
            if (parentId == 0)
            {
                var query = (from s in db.Subjects
                             where s.ParentId == null && s.IsTopic == isTopic && s.GradeReportId == gradeReportId
                             orderby s.ColOrder descending
                             select s).FirstOrDefault();
                if (query == null)
                    return 1;
                else
                    return query.ColOrder + 1;
            }
            else
            {
                var query = (from s in db.Subjects
                             where s.ParentId == parentId && s.IsTopic == isTopic
                             orderby s.ColOrder descending
                             select s).FirstOrDefault();
                if (query == null)
                    return 1;
                else
                    return query.ColOrder + 1;
            }
        }
        // GET: Subjects

        private IEnumerable<Kla> GetDefaultKlas(int gradeReportId)
        {
            var query = from k in db.DefaultKlas
                        where !k.Inactive
                        orderby k.ColOrder
                        select k;
            foreach (var item in query)
            {
                Subject sub = new Subject
                {
                    Name = item.Name,
                    GradeReportId = gradeReportId,
                    Inactive = false,
                    ColOrder = item.ColOrder,
                    AssessmentId = item.AssessmentId,
                    EffortId = item.EffortId,
                    ReportType = item.ReportType,
                    IsTopic = false
                };
                db.Subjects.Add(sub);
            }

            try
            {
                db.SaveChanges();
            }
            catch (DataException  /* dex */ )
            {
            }
            IEnumerable<Kla> klas = from s in db.Subjects
                                    where s.ParentId == null && s.GradeReportId == gradeReportId
                                    orderby s.ColOrder
                                    select new Kla()
                                    {
                                        Id = s.Id,
                                        Name = s.Name,
                                        ColOrder = s.ColOrder,
                                        ReportType = s.ReportType,
                                        AnchorId = "Return" + s.Id
                                    };
            return klas;
        }

        //[Authorize(Roles = "Supervisor")]
        public ActionResult Index(string AnchorId, int? gradeReportId, int? libraryId)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Subjects/Index" });
                libraryId = user.LibraryId;
            }
            if (gradeReportId == null)
                return RedirectToAction("ReportForm");

            IEnumerable<Kla> klas = from s in db.Subjects
                                    where s.ParentId == null && s.GradeReportId == gradeReportId
                                    orderby s.ColOrder
                                    select new Kla()
                                    {
                                        Id = s.Id,
                                        Name = s.Name,
                                        ColOrder = s.ColOrder,
                                        ReportType = s.ReportType,
                                        AnchorId = "Return" + s.Id,
                                        PageBreak = s.PageBreak,
                                        KlaComments = s.KlaComments
                                        // Marks = s.Assessment.Marks.Where(m => m.AssessmentId == s.AssessmentId).ToList() as (IEnumerable<Mark>),
                                        //Efforts = (IEnumerable<Mark>)s.Assessment.Marks.Where(m=>m.AssessmentId == s.EffortId)
                                    };

            IEnumerable<SubKla> substrands = from s in db.Subjects
                                             where s.GradeReportId == gradeReportId && s.ParentId != null && !s.IsTopic //&& s.Subject1.ParentId == null 
                                             orderby s.Subject1.ColOrder, s.ColOrder
                                             select new SubKla()
                                             {
                                                 Id = s.Id,
                                                 Name = s.Name,
                                                 ParentId = (int)s.ParentId,
                                                 Parent = s.Subject1.Name,
                                                 ColOrder = s.ColOrder,
                                                 AnchorId = "Return" + s.Id,
                                                 ReportType = s.Subject1.ReportType
                                             };


            IEnumerable<AssessmentArea> areas = from s in db.Subjects
                                                where s.GradeReportId == gradeReportId && s.ParentId != null && s.IsTopic //&& s.Subject1.ParentId != null
                                                orderby s.Subject1.ColOrder, s.ColOrder
                                                select new AssessmentArea()
                                                {
                                                    Id = s.Id,
                                                    Name = s.Name,
                                                    ParentId = (int)s.ParentId,
                                                    ColOrder = s.ColOrder,
                                                    AnchorId = "Return" + s.Id
                                                };

            SchoolReports model = new SchoolReports();
            if (klas.Count() == 0 && gradeReportId != null)
                klas = GetDefaultKlas((int)gradeReportId);
            //model.Grade = (gradeId == 0) ? "" : "Grade " + grade;
            model.Klas = klas;
            model.Substrands = substrands;
            model.AssessmentAreas = areas;
            model.LibraryId = (int)libraryId;

            var query = (from g in db.GradeReports
                         where g.Id == gradeReportId
                         select g).FirstOrDefault();
            if (query != null)
            {
                model.Ready = query.Ready;
                model.GradeId = query.Grade.Id;
                model.Grade = query.Grade.Name;
                model.GradeName = query.Grade.FullName;
                model.SchoolReportId = query.SchoolReportId;
            }
            model.GradeReportId = (int)gradeReportId;

            // AnchorId = "Result52";
            if (!String.IsNullOrEmpty(AnchorId))
                model.AnchorId = AnchorId;

            return View(model);
        }

        public ActionResult ReportForm(string message, int? SemesterId, int? gradeReportId, int? libraryId)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Subjects/Index" });
                libraryId = user.LibraryId;
            }

            if (gradeReportId != null && gradeReportId > 0)
            {
                return RedirectToAction("Index", new { gradeReportId = gradeReportId, libraryId = libraryId });
            }
            int semesterId = 0;
            if (SemesterId != null)
            {
                semesterId = (int)SemesterId;
            }


            var query = (from r in db.ReportSchools
                         where r.Library_Id == libraryId && ((semesterId > 0 && r.SemesterId == semesterId) || (semesterId == 0 && r.Status > 0))
                         orderby r.Status
                         select r).FirstOrDefault();
            FrontPage model = new FrontPage();
            model.LibraryId = (int)libraryId;
            model.Message = message;
            if (query == null || query.Id == 0)
            {
                model.Id = 0;
                model.ReportName = "No existing reports";
                model.Subtitle = "Click the plus button below to start preparing your school reports.";
            }
            else
            {
                model.Subtitle = "";
                model.Id = query.Id;
                if (query.Status == 1)
                    model.ReportName = "Preparing reports for: Semester " + query.Semester.Number + " " + query.Semester.Year;

                else
                    model.ReportName = "Completed reports for Semester " + query.Semester.Number + " " + query.Semester.Year;


                IEnumerable<RepGrade> repGrades = from gr in db.GradeReports
                                                  where gr.SchoolReportId == query.Id
                                                  select new RepGrade()
                                                  {
                                                      Id = gr.Id,
                                                      GradeId = gr.GradeId,
                                                      Grade = gr.Grade.FullName,
                                                      Ready = gr.Ready
                                                  };
                model.RepGrades = repGrades;
                model.SemesterId = query.SemesterId;
                model.SchoolReportId = query.Id;
                model.UseSuperCom = false;// query.UseSuperCom;
                model.Status = query.Status;
                model.Principal = query.Principal;
                model.Position = query.Position;
                model.Introduction = query.Introduction;
                model.CommentHeader = query.CommentHeader;
                model.KlaComments = query.KlaComments;
                //model.SuperHeader = query.SuperHeader;
                IEnumerable<RepSemester> repSemesters = from s in db.ReportSchools
                                                        where s.Library_Id == libraryId
                                                        select new RepSemester()
                                                        {
                                                            Id = s.Semester.Id,
                                                            Semester = s.Semester.Year + " Semester " + s.Semester.Number.ToString(),
                                                        };
                if (repSemesters != null && repSemesters.Count() > 0)
                {
                    model.RepSemesters = repSemesters;
                }
            }
            return View(model);
        }

        [HttpGet]
        public JsonResult SetGrade(string grade, int id)
        {
            int gradeId = 0;
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null || !(User.IsInRole("Supervisor") || User.IsInRole("Administrator")))
                return Json(new { Success = false, Result = "You are not authorised to design reports." }, JsonRequestBehavior.AllowGet);

            grade = grade.Trim();
            switch (grade)
            {
                case "Pre-school":
                    gradeId = 1;
                    break;
                case "Kindergarten":
                    gradeId = 2;
                    break;
                case "Grade 1":
                    gradeId = 3;
                    break;
                case "Grade 2":
                    gradeId = 4;
                    break;
                case "Grade 3":
                    gradeId = 5;
                    break;
                case "Grade 4":
                    gradeId = 6;
                    break;
                case "Grade 5":
                    gradeId = 7;
                    break;
                case "Grade 6":
                    gradeId = 8;
                    break;
                default:
                    return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
            var query = (from r in db.GradeReports
                         where r.SchoolReportId == id && r.GradeId == gradeId
                         select r).FirstOrDefault();

            if (query != null)
                return Json(new { Success = true, Result = query.Id }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetAsReady(int id, bool isReady)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var query = (from gr in db.GradeReports
                                 where gr.Id == id
                                 select gr).FirstOrDefault();
                    if (query != null)
                    {
                        query.Ready = isReady;
                        db.SaveChanges();
                        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }

            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetPageBreak(int id, bool hasPageBreak)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var query = (from s in db.Subjects
                                 where s.Id == id
                                 select s).FirstOrDefault();
                    if (query != null)
                    {
                        query.PageBreak = hasPageBreak;
                        db.SaveChanges();
                        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }

            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        //// GET: Subjects/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Subject subject = db.Subjects.Find(id);
        //    if (subject == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(subject);
        //}

        // GET: Subjects/Create
        [OutputCache(Duration = 0)]
        public ActionResult CreateArea(int? parentId, int? gradeId, string grade, string parent, string grandParent, int gradeReportId)
        {
            AssessmentArea model = new AssessmentArea();
            model.ParentId = (int)parentId;
            model.GradeId = (int)gradeId;
            model.Grade = grade;
            model.Parent = "KLA: " + (String.IsNullOrEmpty(grandParent) ? parent.Trim() : grandParent.Trim() + " - " + parent.Trim());
            model.GradeReportId = gradeReportId;
            return PartialView("_CreateArea", model);
        }

        // POST: Subjects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateArea(Subject subject, int? gradeId, int parentId, int gradeReportId, int libraryId)//[Bind(Include = "Name,ParentId")]
        {
            if (ModelState.IsValid)
            {
                int id = 0;
                string buffer = subject.Name;
                if (!String.IsNullOrEmpty(buffer))
                {
                    foreach (var name in buffer.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Subject sub = new Subject
                        {
                            Name = name.Substring(0, Math.Min(name.Length, 240)),
                            GradeReportId = gradeReportId,
                            ParentId = parentId,
                            Inactive = false,
                            ColOrder = GetNextColOrder(parentId, true, 0),
                            AssessmentId = 1,
                            EffortId = 1,
                            ReportType = 1,
                            IsTopic = true,
                            PageBreak = false
                        };

                        db.Subjects.Add(sub);

                        try
                        {
                            db.SaveChanges();
                            id = sub.Id;
                            var students = (from r in db.Results
                                            where r.Subject.GradeReportId == gradeReportId
                                            select new
                                            {
                                                r.StudentReportId
                                            }).Distinct();
                            foreach (var item in students)
                            {
                                Result r = new Result();
                                r.StudentReportId = item.StudentReportId;
                                r.SubjectId = sub.Id;
                                db.Results.Add(r);
                            }
                            db.SaveChanges();

                        }
                        catch (DataException dex)
                        {
                            var bf = dex;
                            ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");

                        }

                    }
                    if (id > 0)
                    {
                        return RedirectToAction("Index", new { AnchorId = "Return" + id, gradeReportId = gradeReportId, libraryId = libraryId });
                    }
                }
            }
            return RedirectToAction("Index", new { gradeReportId = gradeReportId, libraryId = libraryId });
        }

        // GET: Subjects/Create
        [OutputCache(Duration = 0)]
        public ActionResult CreateSubstrand(int? parentId, int? gradeId, string grade, string parent, int gradeReportId)
        {
            SubKlaCreate model = new SubKlaCreate();
            model.ParentId = (int)parentId;
            model.GradeId = (int)gradeId;
            model.Grade = grade;
            model.Parent = "KLA: " + parent;
            model.GradeReportId = gradeReportId;

            return PartialView("_CreateSubstrand", model);
        }

        // POST: Subjects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSubstrand(Subject subject, int? gradeId, int gradeReportId, int libraryId) //[Bind(Include = "Name,ParentId")] 
        {
            if (ModelState.IsValid)
            {
                int id = 0;
                string buffer = subject.Name;
                if (!String.IsNullOrEmpty(buffer))
                {
                    foreach (var name in buffer.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Subject sub = new Subject
                        {
                            Name = name.Substring(0, Math.Min(name.Length, 60)),
                            GradeReportId = gradeReportId,
                            ParentId = subject.ParentId,
                            Inactive = false,
                            ColOrder = GetNextColOrder((int)subject.ParentId, false, 0),
                            AssessmentId = 1,
                            EffortId = 1,
                            ReportType = 1,
                            IsTopic = false,
                            PageBreak = false
                        };
                        db.Subjects.Add(sub);

                        try
                        {
                            db.SaveChanges();
                            id = sub.Id;
                        }
                        catch (DataException  /* dex */ )
                        {
                            ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                        }
                    }
                    if (id > 0)
                        return RedirectToAction("Index", new { AnchorId = "Return" + id, gradeReportId = gradeReportId, libraryId = libraryId });
                }
            }
            return RedirectToAction("Index", new { gradeReportId = gradeReportId, libraryId = libraryId });
        }

        // GET: Subjects/Create
        [OutputCache(Duration = 0)]
        public ActionResult CreateKla(int? gradeId, string grade, int gradeReportId)
        {
            Kla model = new Kla();
            model.GradeId = (int)gradeId;
            model.Grade = grade;
            model.ReportType = 1;
            model.GradeReportId = gradeReportId;
            model.PageBreak = false;
            model.KlaComments = false;
            GradeReport gradeReport = db.GradeReports.Find(gradeReportId);
            model.ShowComments = gradeReport.ReportSchool.KlaComments;

            return PartialView("_CreateKla", model);
        }

        // POST: Subjects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateKla([Bind(Include = "Name, ReportType, KlaComments")] Subject subject, int? gradeId, int gradeReportId, int libraryId)
        {
            if (ModelState.IsValid)
            {
                db.Subjects.Add(subject);
                subject.GradeReportId = gradeReportId;
                subject.IsTopic = false;
                subject.Inactive = false;
                subject.ColOrder = GetNextColOrder(0, false, gradeReportId);
                subject.PageBreak = false;
                switch (subject.ReportType)
                {
                    case 1:
                        subject.AssessmentId = 2;
                        subject.EffortId = 3;
                        break;

                    case 2:
                        subject.AssessmentId = 2;
                        subject.EffortId = 3;
                        break;

                    case 3:
                        subject.AssessmentId = 4;
                        subject.EffortId = 1;
                        break;

                    case 4:
                        subject.AssessmentId = 5;
                        subject.EffortId = 1;
                        break;
                }

                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index", new { AnchorId = "Return" + subject.Id, gradeReportId = gradeReportId, libraryId = libraryId });
                }
                catch (DataException  /* dex */ )
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            return RedirectToAction("Index", new { gradeReportId = gradeReportId, libraryId = libraryId });

        }

        public ActionResult OpenNewSemester(int libraryId, int? id, bool nextSemester = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return Content("");

            if (!User.IsInRole("Administrator"))
                return Content("");
            //return RedirectToAction("ReportForm", "Subjects", new { libraryId = libraryId, message = "You are not authorised to start a new semester's reports." });

            NextSemester model = new NextSemester();
            model.LibraryId = libraryId;

            var report = (from s in db.ReportSchools
                          where s.Library_Id == libraryId
                          orderby s.SemesterId
                          select s).Take(2);
            if (report.Count() == 0)
            {
                model.Options = 1;
                model.Selection = 3;
                model.Semester1 = "Semester1";
                model.Semester2 = "Semester2";
                model.Id1 = 0;
                model.Id2 = 0;
            }
            else if (report.Count() == 1)
            {
                model.Options = 2;
                model.Selection = 1;
                model.Semester1 = "Semester " + report.FirstOrDefault().Semester.Number + " " + report.FirstOrDefault().Semester.Year;
                model.Semester2 = "";
                model.Id1 = report.FirstOrDefault().Id;
                model.Id2 = 0;
            }
            else
            {
                model.Options = 3;
                model.Selection = 1;
                model.Semester1 = "Semester " + report.FirstOrDefault().Semester.Number + " " + report.FirstOrDefault().Semester.Year;
                model.Semester2 = "Semester " + report.Skip(1).FirstOrDefault().Semester.Number + " " + report.Skip(1).FirstOrDefault().Semester.Year; ;
                model.Id1 = report.FirstOrDefault().Id;
                model.Id2 = report.Skip(1).FirstOrDefault().Id;
            }

            return View("_NextSemester", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenNewSemester(NextSemester model, int selection)
        {
            if (model == null)
                return Content("");

            int id = -1;
            switch (selection)
            {
                case 1:
                    id = model.Id1;
                    break;
                case 2:
                    id = model.Id2;
                    break;
                case 3:
                    id = 0;
                    break;
            }
            int semesterId = 0;
            if (id >= 0)
                semesterId = GetNextSemester(id, model.LibraryId);
            if (semesterId == 0)
                return Content("");

            return RedirectToAction("ReportForm", new { SemesterId = semesterId });
        }

        // GET: Subjects/Edit/5
        public ActionResult EditFrontPage(int? id, int semesterId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Subjects/ReportForm" });

            if (!(User.IsInRole("Administrator") || User.IsInRole("Supervisor")))
                return RedirectToAction("ReportForm", "Subjects", new { libraryId = user.LibraryId, message = "You are not authorised to amend the default report settings." });

            ReportSchool report = db.ReportSchools.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            FrontPage model = new FrontPage();

            model.Id = (int)id;
            model.ReportName = " Semester " + report.Semester.Number + " " + report.Semester.Year;
            model.UseSuperCom = false;// report.UseSuperCom;
            model.Principal = report.Principal.Trim();
            model.Position = report.Position.Trim();
            model.Introduction = report.Introduction.Trim();
            model.CommentHeader = report.CommentHeader.Trim();
            model.SemesterId = semesterId;
            model.Watermark = report.Watermark;
            model.DeptLogo = report.DeptLogo;
            model.KlaComments = report.KlaComments;
            //model.SuperHeader = report.SuperHeader.Trim();
            var image = (from li in db.LibImages
                         where li.Library_Id == report.Library_Id
                         select li).FirstOrDefault();
            if (image != null && image.Image_Id > 0)
            {
                model.ImageId = image.Image_Id;
                model.Crest = image.Image;
            }
            else
                model.ImageId = 0;

            return View("EditFrontPage", model);
        }

        // POST: Subjects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult EditFrontPage([Bind(Include = "Principal,Position,Introduction")] FrontPage report)
        public ActionResult EditFrontPage(FrontPage report, int semesterId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var frontPage = (from s in db.ReportSchools
                                     where s.Id == report.Id
                                     select s).FirstOrDefault();
                    if (frontPage != null)
                    {
                        int libraryId = frontPage.Library_Id;
                        frontPage.UseSuperCom = false;
                        frontPage.Principal = report.Principal;
                        frontPage.Position = report.Position;
                        frontPage.Introduction = report.Introduction;
                        frontPage.CommentHeader = report.CommentHeader;
                        frontPage.Watermark = report.Watermark;
                        frontPage.DeptLogo = report.DeptLogo;
                        frontPage.KlaComments = report.KlaComments;
                        // frontPage.SuperHeader = report.SuperHeader;
                        db.SaveChanges();
                        if (Request.Files["crest"] != null && Request.Files["crest"].ContentLength > 0)
                        {
                            if (Request.Files["crest"].ContentLength < 1024 * 1024 && Request.Files["crest"].ContentType == "image/jpeg")
                            {
                                byte[] Image;
                                using (var binaryReader = new BinaryReader(Request.Files["crest"].InputStream))
                                {
                                    Image = binaryReader.ReadBytes(Request.Files["crest"].ContentLength);
                                }
                                if (Image != null)
                                {
                                    int libImageId = db.LibImages.Where(l => l.Library_Id == libraryId).FirstOrDefault().Image_Id;
                                    if (libImageId == 0)
                                    {
                                        LibImage li = new LibImage();
                                        li.Library_Id = libraryId;
                                        li.Image = Image;
                                        db.LibImages.Add(li);
                                        db.SaveChanges();
                                    }
                                    else
                                    {
                                        var libImageToUpdate = db.LibImages.Find(libImageId);
                                        if (libImageToUpdate != null)
                                        {
                                            libImageToUpdate.Image = Image;
                                            db.SaveChanges();
                                        }
                                    }
                                }
                            }
                            db.SaveChanges();
                        }
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
                return RedirectToAction("ReportForm", new { semesterId = semesterId });
            }
            return View(report);
        }

        // GET: Subjects/Edit/5
        [OutputCache(Duration = 0)]
        public ActionResult EditKla(int? id, int gradeReportId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subject subject = db.Subjects.Find(id);
            if (subject == null)
            {
                return HttpNotFound();
            }

            Kla model = new Kla();
            model.Id = subject.Id;
            model.Name = subject.Name.Trim();
            model.ReportType = subject.ReportType;
            model.GradeReportId = gradeReportId;
            model.PageBreak = subject.PageBreak;
            model.KlaComments = subject.KlaComments;
            model.ShowComments = subject.GradeReport.ReportSchool.KlaComments;

            return PartialView("_EditKla", model);
        }

        // POST: Subjects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditKla(Kla subject, int gradeReportId, int libraryId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var query = (from s in db.Subjects
                                 where s.Id == subject.Id
                                 select s).FirstOrDefault();
                    if (query != null)
                    {
                        query.Name = subject.Name;
                        query.KlaComments = subject.KlaComments;
                        query.PageBreak = subject.PageBreak;
                        //query.ReportType = subject.ReportType;
                        //switch (subject.ReportType)
                        //{
                        //    case 1:
                        //        subject.AssessmentId = 2;
                        //        subject.EffortId = 3;
                        //        break;

                        //    case 2:
                        //        subject.AssessmentId = 2;
                        //        subject.EffortId = 3;
                        //        break;

                        //    case 3:
                        //        subject.AssessmentId = 4;
                        //        subject.EffortId = 1;
                        //        var query2 = from s in db.Subjects
                        //                     where s.ParentId == subject.Id && !s.IsTopic
                        //                     select s;

                        //        foreach (var Subject in query2)
                        //        {
                        //            db.Subjects.Remove(Subject);
                        //        }
                        //        break;

                        //    case 4:
                        //        subject.AssessmentId = 5;
                        //        subject.EffortId = 1;
                        //        var query3 = from s in db.Subjects
                        //                     where s.ParentId == subject.Id && !s.IsTopic
                        //                     select s;

                        //        foreach (var Subject in query3)
                        //        {
                        //            db.Subjects.Remove(Subject);
                        //        }
                        //        break;
                        //}
                        db.SaveChanges();
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
                return RedirectToAction("Index", new { AnchorId = "Return" + subject.Id, gradeReportId = gradeReportId, libraryId = libraryId });
            }

            return PartialView("_Edit", subject);
        }

        // GET: Subjects/Edit/5
        [OutputCache(Duration = 0)]
        public ActionResult EditSubstrand(int? id, int gradeReportId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subject subject = db.Subjects.Find(id);
            if (subject == null)
            {
                return HttpNotFound();
            }
            SubKla model = new SubKla();
            model.Id = subject.Id;
            model.Name = subject.Name.Trim();
            model.GradeReportId = gradeReportId;

            return PartialView("_EditSubstrand", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSubstrand(Subject subject, int gradeReportId, int libraryId) //[Bind(Include = "Id, Name")] , Library_Id, ParentId, GradeId, AssessmentId, EffortId, Inactive, ReportType, ColOrder, IsTopic
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var query = (from s in db.Subjects
                                 where s.Id == subject.Id
                                 select s).FirstOrDefault();
                    if (query != null)
                    {
                        query.Name = subject.Name;
                        db.SaveChanges();
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
                return RedirectToAction("Index", new { AnchorId = "Return" + subject.Id, gradeReportId = gradeReportId, libraryId = libraryId });
            }

            return PartialView("_EditSubstrand", subject);
        }

        // GET: Subjects/Edit/5
        [OutputCache(Duration = 0)]
        public ActionResult EditArea(int? id, int gradeReportId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subject subject = db.Subjects.Find(id);
            if (subject == null)
            {
                return HttpNotFound();
            }
            AssessmentArea model = new AssessmentArea();
            model.Id = subject.Id;
            model.Name = subject.Name.Trim();
            model.GradeReportId = gradeReportId;

            return PartialView("_EditArea", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditArea(Subject subject, int gradeReportId, int libraryId) //[Bind(Include = "Id, Name")] , Library_Id, ParentId, GradeId, AssessmentId, EffortId, Inactive, ReportType, ColOrder, IsTopic
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var query = (from s in db.Subjects
                                 where s.Id == subject.Id
                                 select s).FirstOrDefault();
                    if (query != null)
                    {
                        query.Name = subject.Name;
                        db.SaveChanges();
                    }
                }
                catch (DataException  /* dex  */)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
                return RedirectToAction("Index", new { AnchorId = "Return" + subject.Id, gradeReportId = gradeReportId, libraryId = libraryId });
            }

            return PartialView("_EditArea", subject);
        }

        // GET: Subjects/Delete/5
        public ActionResult Delete(int? id, int returnId, int gradeReportId, int libraryId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subject subject = db.Subjects.Find(id);
            if (subject == null)
            {
                return HttpNotFound();
            }
            SubjectDelete subjectDelete = new SubjectDelete();
            subjectDelete.Id = subject.Id;
            subjectDelete.Name = subject.Name;
            subjectDelete.ReturnId = returnId;
            subjectDelete.GradeReportId = gradeReportId;
            if (subject.ParentId == null)
            {
                subjectDelete.Message = "Are you sure you want to delete this KLA?";
                if (subject.Subjects1.Count() > 0)
                    subjectDelete.Message2 = "(All its child substrands and assessments areas will also be deleted.)";
                subjectDelete.Type = "Key Learning Area";
            }
            else if (subject.IsTopic)
            {
                subjectDelete.Message = "Are you sure you want to delete this assessment area?";
                subjectDelete.Message2 = "";
                subjectDelete.Type = "Assessment Area";
            }
            else //substrand
            {
                subjectDelete.Message = "Are you sure you want to delete this substrand?";
                if (subject.Subjects1.Count() > 0)
                    subjectDelete.Message2 = "(All its child assessments areas will also be deleted.)";
                subjectDelete.Type = "Substrand";
            }
            //return View(subject);
            return PartialView("_Delete", subjectDelete);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, int ReturnId, int gradeReportId, int libraryId)
        {
            string returnString = "";

            if (ReturnId > 0)
                returnString = "Return" + ReturnId;
            var query = (from s in db.Subjects
                         where s.ParentId == id || s.Subject1.ParentId == id || s.Id == id
                         orderby s.ParentId descending, s.Subject1.ParentId descending, s.Id descending
                         select s).ToList();

            foreach (var subject in query)
            //for (int i = 0; i < query.Count(); i++)
            {
                var results = from r in db.Results
                              where r.Subject.GradeReportId == gradeReportId && r.SubjectId == subject.Id
                              select r;
                foreach (var result in results)
                {
                    db.Results.Remove(result);
                }
                db.Subjects.Remove(subject);
            }
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                var bf = ex;
            }
            return RedirectToAction("Index", new { AnchorId = returnString, gradeReportId = gradeReportId, libraryId = libraryId });
        }

        public ActionResult MoveKla(int id, int gradeReportId, int colOrder, int libraryId, bool down = false)
        {
            bool doSave = false;
            int anchorId = 0;

            if (gradeReportId == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (down)
            {
                var klas = (from s in db.Subjects
                            where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder >= colOrder
                            orderby s.ColOrder ascending
                            select s).Take(2);
                if (klas != null && klas.Count() > 1)
                {
                    //int thisColOrder = list.First().ColOrder;
                    anchorId = klas.First().Id;
                    int nextOrder = klas.Skip(1).First().ColOrder;
                    klas.First().ColOrder = nextOrder;
                    klas.Skip(1).First().ColOrder = colOrder;
                    doSave = true;
                }
            }
            else
            {
                var klas = (from s in db.Subjects
                            where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder <= colOrder
                            orderby s.ColOrder descending
                            select s).Take(2);
                if (klas != null && klas.Count() > 1)
                {
                    //int colOrder = list.First().ColOrder;
                    anchorId = klas.First().Id;
                    int nextOrder = klas.Skip(1).First().ColOrder;
                    klas.First().ColOrder = nextOrder;
                    klas.Skip(1).First().ColOrder = colOrder;
                    doSave = true;
                }
            }
            if (doSave)
            {
                try
                {
                    db.SaveChanges();
                    System.Media.SystemSounds.Exclamation.Play();
                }
                catch (Exception)
                {
                    //var bf = dex;
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            else
                System.Media.SystemSounds.Hand.Play();

            return RedirectToAction("Index", new { AnchorId = "Return" + id, gradeReportId = gradeReportId, libraryId = libraryId });
        }

        public ActionResult MovePosition(int id, int gradeReportId, int type, int colOrder, int libraryId, int? parentId, bool down = false)
        {
            bool doSave = false;

            int i = 0;
            int thisOrder = 0, klaOrder = 0, grandParentId = 0;
            if (gradeReportId == 0)  //id == null || 
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            switch (type)
            {
                case 1: //kla
                    {
                        if (down)
                        {
                            var klas = (from s in db.Subjects
                                        where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder >= colOrder
                                        orderby s.ColOrder ascending
                                        select s).Take(2);
                            if (klas != null && klas.Count() > 1)
                            {
                                //int thisColOrder = list.First().ColOrder;
                                int nextOrder = klas.Skip(1).First().ColOrder;
                                klas.First().ColOrder = nextOrder;
                                klas.Skip(1).First().ColOrder = colOrder;
                                doSave = true;
                            }
                        }
                        else
                        {
                            var klas = (from s in db.Subjects
                                        where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder <= colOrder
                                        orderby s.ColOrder descending
                                        select s).Take(2);
                            if (klas != null && klas.Count() > 1)
                            {
                                //int colOrder = list.First().ColOrder;
                                int nextOrder = klas.Skip(1).First().ColOrder;
                                klas.First().ColOrder = nextOrder;
                                klas.Skip(1).First().ColOrder = colOrder;
                                doSave = true;
                            }
                        }
                    }
                    break;

                case 2: //substrand
                    {
                        if (down)
                        {
                            var substrands1 = (from s in db.Subjects
                                               where s.ParentId == parentId && s.ColOrder >= colOrder && !s.IsTopic && s.Subject1.ReportType < 3
                                               orderby s.ColOrder ascending
                                               select s).Take(2);
                            if (substrands1 != null)
                            {
                                if (substrands1.Count() > 1)
                                {
                                    //int thisColOrder = list.First().ColOrder;
                                    int nextOrder = substrands1.Skip(1).First().ColOrder;
                                    substrands1.First().ColOrder = nextOrder;
                                    substrands1.Skip(1).First().ColOrder = colOrder;
                                    doSave = true;
                                }
                                else
                                {
                                    thisOrder = substrands1.FirstOrDefault().Subject1.ColOrder;
                                    if (substrands1.Count() == 1 && thisOrder > 0)
                                    {
                                        var klas = (from s in db.Subjects
                                                    where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder > thisOrder && s.ReportType < 3
                                                    orderby s.ColOrder ascending
                                                    select s).FirstOrDefault();
                                        if (klas != null)
                                        {
                                            var substrands2 = (from s in db.Subjects
                                                               where s.ParentId == klas.Id && !s.IsTopic
                                                               orderby s.ColOrder ascending
                                                               select s);
                                            i = 2;
                                            foreach (var item in substrands2)
                                            {
                                                item.ColOrder = i;
                                                i++;
                                            }
                                            substrands1.First().ColOrder = 1;
                                            substrands1.First().ParentId = klas.Id;
                                            doSave = true;
                                        }
                                    }
                                }
                            }
                        }
                        else //up
                        {
                            var substrands1 = (from s in db.Subjects
                                               where s.ParentId == parentId && s.ColOrder <= colOrder && !s.IsTopic && s.Subject1.ReportType < 3
                                               orderby s.ColOrder descending
                                               select s).Take(2);
                            if (substrands1 != null)
                            {
                                if (substrands1.Count() > 1)
                                {
                                    //int colOrder = substrands1.First().ColOrder;
                                    int nextOrder = substrands1.Skip(1).First().ColOrder;
                                    substrands1.First().ColOrder = nextOrder;
                                    substrands1.Skip(1).First().ColOrder = colOrder;
                                    doSave = true;
                                }
                                else
                                {
                                    if (substrands1.Count() == 1)
                                    {
                                        thisOrder = substrands1.FirstOrDefault().Subject1.ColOrder;
                                        var klas = (from s in db.Subjects
                                                    where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder < thisOrder && s.ReportType < 3
                                                    orderby s.ColOrder descending
                                                    select s).FirstOrDefault();
                                        if (klas != null)
                                        {
                                            var substrands2 = (from s in db.Subjects
                                                               where s.ParentId == klas.Id && !s.IsTopic
                                                               orderby s.ColOrder descending
                                                               select s).FirstOrDefault();
                                            if (substrands2 == null)
                                                substrands1.First().ColOrder = 1;
                                            else
                                                substrands1.First().ColOrder = substrands2.ColOrder + 1;
                                            substrands1.First().ParentId = klas.Id;
                                            doSave = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;

                case 3: //area
                    {
                        if (down)
                        {
                            var area1 = (from s in db.Subjects
                                         where s.ParentId == parentId && s.ColOrder >= colOrder && s.IsTopic
                                         orderby s.ColOrder ascending
                                         select s).Take(2);
                            if (area1 != null)
                            {
                                if (area1.Count() > 1)
                                {
                                    //int thisColOrder = list.First().ColOrder;
                                    int nextOrder = area1.Skip(1).First().ColOrder;
                                    area1.First().ColOrder = nextOrder;
                                    area1.Skip(1).First().ColOrder = colOrder;
                                    doSave = true;
                                }
                                else
                                {
                                    thisOrder = area1.FirstOrDefault().Subject1.ColOrder;
                                    if (area1.Count() == 1 && thisOrder > 0)
                                    {
                                        if (area1.FirstOrDefault().Subject1.ParentId == null)
                                        {
                                            grandParentId = (int)area1.First().ParentId;
                                            klaOrder = area1.FirstOrDefault().Subject1.ColOrder;
                                            thisOrder = -1;
                                        }
                                        else
                                        {
                                            grandParentId = (int)area1.FirstOrDefault().Subject1.ParentId;
                                            klaOrder = area1.FirstOrDefault().Subject1.Subject1.ColOrder;
                                        }
                                        bool done = false;
                                        if (area1.FirstOrDefault().Subject1.ReportType == 1)
                                        {
                                            var substrands = (from s in db.Subjects
                                                              where s.ParentId == grandParentId && !s.IsTopic && s.ColOrder > thisOrder
                                                              orderby s.ColOrder ascending
                                                              select s).FirstOrDefault();
                                            if (substrands != null)
                                            {
                                                var area2 = (from s in db.Subjects
                                                             where s.ParentId == substrands.Id
                                                             orderby s.ColOrder ascending
                                                             select s);
                                                i = 2;
                                                foreach (var item in area2)
                                                {
                                                    item.ColOrder = i;
                                                    i++;
                                                }
                                                area1.First().ColOrder = 1;
                                                area1.First().ParentId = substrands.Id;
                                                doSave = true;
                                                done = true;
                                            }
                                        }
                                        if (!done)
                                        {
                                            var klas = (from s in db.Subjects
                                                        where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder > klaOrder
                                                        orderby s.ColOrder ascending
                                                        select s).FirstOrDefault();
                                            if (klas != null)
                                            {
                                                var area2 = (from s in db.Subjects
                                                             where s.ParentId == klas.Id && s.IsTopic
                                                             orderby s.ColOrder ascending
                                                             select s);
                                                i = 2;
                                                foreach (var item in area2)
                                                {
                                                    item.ColOrder = i;
                                                    i++;
                                                }
                                                area1.First().ColOrder = 1;
                                                area1.First().ParentId = klas.Id;
                                                doSave = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else //up
                        {
                            var area1 = (from s in db.Subjects
                                         where s.ParentId == parentId && s.ColOrder <= colOrder && s.IsTopic
                                         orderby s.ColOrder descending
                                         select s).Take(2);
                            if (area1 != null)
                            {
                                if (area1.Count() > 1) // in same substrand
                                {
                                    //int colOrder = area1.First().ColOrder;
                                    int nextOrder = area1.Skip(1).First().ColOrder;
                                    area1.First().ColOrder = nextOrder;
                                    area1.Skip(1).First().ColOrder = colOrder;
                                    doSave = true;
                                }
                                else
                                {
                                    if (area1.Count() == 1)
                                    {
                                        thisOrder = area1.FirstOrDefault().Subject1.ColOrder;


                                        grandParentId = (area1.FirstOrDefault().Subject1.ParentId == null) ? 0 : (int)area1.FirstOrDefault().Subject1.ParentId;
                                        if (grandParentId > 0)
                                        {
                                            //check if more substrands
                                            int thisId = 0;
                                            var substrands = (from s in db.Subjects
                                                              where s.ParentId == grandParentId && !s.IsTopic && s.ColOrder < thisOrder
                                                              orderby s.ColOrder descending
                                                              select s).FirstOrDefault();
                                            if (substrands != null) //move to next substrand
                                                thisId = substrands.Id;
                                            else
                                                thisId = grandParentId;

                                            var area2 = (from s in db.Subjects
                                                         where s.ParentId == thisId && s.IsTopic
                                                         orderby s.ColOrder descending
                                                         select s).FirstOrDefault();
                                            if (area2 == null)
                                                area1.First().ColOrder = 1;
                                            else
                                                area1.First().ColOrder = area2.ColOrder + 1;
                                            area1.First().ParentId = thisId;
                                            doSave = true;

                                            if (!doSave)
                                            {
                                                grandParentId = (int)area1.First().ParentId;
                                                klaOrder = area1.FirstOrDefault().Subject1.ColOrder;
                                                var klas = (from s in db.Subjects
                                                            where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder < klaOrder
                                                            orderby s.ColOrder descending
                                                            select s).FirstOrDefault();
                                                if (klas != null)
                                                {
                                                    thisId = klas.Id;
                                                    var substrands2 = (from s in db.Subjects
                                                                       where s.ParentId == klas.Id && !s.IsTopic
                                                                       orderby s.ColOrder descending
                                                                       select s).FirstOrDefault();
                                                    if (substrands2 != null) //at bottom in previous substrand
                                                    {
                                                        thisId = substrands2.Id;
                                                    }
                                                    var area3 = (from s in db.Subjects
                                                                 where s.ParentId == thisId && s.IsTopic
                                                                 orderby s.ColOrder descending
                                                                 select s).FirstOrDefault();
                                                    if (area3 == null)
                                                        area1.First().ColOrder = 1;
                                                    else
                                                        area1.First().ColOrder = area3.ColOrder + 1;
                                                    area1.First().ParentId = thisId;
                                                    doSave = true;
                                                }
                                            }
                                        }
                                        else //next kla
                                        {
                                            klaOrder = area1.FirstOrDefault().Subject1.ColOrder;
                                            var klas = (from s in db.Subjects
                                                        where s.ParentId == null && s.GradeReportId == gradeReportId && s.ColOrder < klaOrder
                                                        orderby s.ColOrder descending
                                                        select s).FirstOrDefault();
                                            if (klas != null) //kla found
                                            {
                                                int thisId = klas.Id;
                                                if (klas.ReportType == 1)
                                                {
                                                    var substrands = (from s in db.Subjects
                                                                      where s.ParentId == klas.Id && !s.IsTopic
                                                                      orderby s.ColOrder descending
                                                                      select s).FirstOrDefault();

                                                    if (substrands != null) //at bottom in previous substrand
                                                    {
                                                        thisId = substrands.Id;
                                                    }
                                                }
                                                var area2 = (from s in db.Subjects
                                                             where s.ParentId == thisId && s.IsTopic
                                                             orderby s.ColOrder descending
                                                             select s).FirstOrDefault();
                                                if (area2 == null)
                                                    area1.First().ColOrder = 1;
                                                else
                                                    area1.First().ColOrder = area2.ColOrder + 1;
                                                area1.First().ParentId = thisId;
                                                doSave = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
            if (doSave)
            {
                try
                {
                    db.SaveChanges();
                    System.Media.SystemSounds.Exclamation.Play();
                }
                catch (DataException  /* dex */ )
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            else
                System.Media.SystemSounds.Hand.Play();

            return RedirectToAction("Index", new { AnchorId = "Return" + id, gradeReportId = gradeReportId, libraryId = libraryId });
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