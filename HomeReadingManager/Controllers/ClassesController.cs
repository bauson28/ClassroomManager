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
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;



namespace HomeReadingManager.Controllers
{
    public class ClassesController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
      
        private ApplicationUserManager _userManager;

        public ClassesController()
        {
        }

        public ClassesController(ApplicationUserManager userManager)
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
        // GET: Classes
        [Authorize(Roles = "Teacher, Supervisor, Administrator")]
        public ActionResult Index(string message, bool showInactive = false, bool editMode = false)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Classes/Index" });
            int libraryId = user.LibraryId;
            
            ClassesVM model = new ClassesVM();
            model.Message = message;
            model.ShowInactive = showInactive;
            model.IsAdministrator = User.IsInRole("Administrator");
            IQueryable<ClassDetail> classDetail = from c in db.Classes
                                                  where c.Library_Id == libraryId && c.Obsolete == showInactive
                                                  select new ClassDetail()
                                                  {
                                                      ClassId = c.Classes_Id,
                                                      ClassName = c.ClassDesc,
                                                      TeacherId = c.Teacher_Id == null ? 0 : (int)c.Teacher_Id,
                                                      Teacher = c.Teacher_Id == null ? "" : c.Teacher.FirstName.Trim() + " " + c.Teacher.LastName.Trim(),
                                                      TeacherId2 = c.Teacher2Id == null ? 0 : (int)c.Teacher2Id,
                                                      Teacher2 = c.Teacher2Id == null ? "" : c.Teacher1.FirstName.Trim() + " " + c.Teacher1.LastName.Trim(),
                                                      Inactive = c.Obsolete,
                                                      Stage = c.Stage
                                                  };
            classDetail = classDetail.OrderBy(c => c.ClassName);
            model.ClassDetails = classDetail;
           
            if (editMode)
            {
                IQueryable<TeacherRec> teachers = from t in db.Teachers
                                                  where t.Library_Id == libraryId && !t.Inactive
                                                  orderby t.FirstName, t.LastName
                                                  select new TeacherRec()
                                                  {
                                                      Id = t.Teacher_Id,
                                                      Teacher = t.FirstName.Trim() + " " + t.LastName.Trim()
                                                  };
                //model.Teachers = teachers;
                //model.TeacherId = user.TeacherId;
                ViewBag.TeachersList = teachers;
                return View("EditClasses", model);
            }
            return View(model);
        }

        public JsonResult AssignToClass(int id, int? teacherId, bool second)
        {
            if (id > 0)
            {
                var classToUpdate = db.Classes.Find(id);
                try
                {
                    if (teacherId == null || teacherId == 0)
                        if (second)
                            classToUpdate.Teacher2Id = null;
                        else 
                            classToUpdate.Teacher_Id = null;
                    else
                        if (second)
                            classToUpdate.Teacher2Id = (int)teacherId;
                        else
                            classToUpdate.Teacher_Id = (int)teacherId;
                    db.SaveChanges();
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult MakeInactive(int id)
        {
            if (id > 0)
            {
                var classToUpdate = db.Classes.Find(id);
                try
                {

                    classToUpdate.Obsolete = ! classToUpdate.Obsolete;
                   
                    db.SaveChanges();
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }
        // GET: Classes/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Class @class = db.Classes.Find(id);
            if (@class == null)
            {
                return HttpNotFound();
            }
            return View(@class);
        }

        // GET: Classes/Create
        public ActionResult Create()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Classes/Index" });

            int libraryId = user.LibraryId;
            if (!User.IsInRole("Administrator"))
                return RedirectToAction("Index", "Classes", new { libraryId = libraryId, message = "You are not authorised to create classes." });
          
            ClassEdit model = new ClassEdit();
            //model.Teacher_Id = 0;
            //model.Teacher2Id = 0;
            model.Obsolete = false;
            model.Stage = "1";
            IQueryable<TeacherRec> teachers = from t in db.Teachers
                                              where t.Library_Id == libraryId && !t.Inactive
                                              orderby t.FirstName, t.LastName
                                              select new TeacherRec()
                                              {
                                                  Id = t.Teacher_Id,
                                                  Teacher = t.FirstName.Trim() + " " + t.LastName.Trim()
                                              };
            model.Teachers = teachers;
            model.Stages = GetStages();
            return View(model);
        }

        // POST: Classes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ClassDesc,Stage,Teacher_Id,Teacher2Id")] ClassEdit model)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Classes/Index" });
                int libraryId = user.LibraryId;
                Class c = new Class();
                c.Library_Id = libraryId;
                c.ClassDesc = model.ClassDesc;
                c.Stage = model.Stage;
                if (model.Teacher_Id != null && model.Teacher_Id > 0)
                    c.Teacher_Id = model.Teacher_Id;
                if (model.Teacher2Id != null && model.Teacher2Id > 0)
                    c.Teacher2Id = model.Teacher2Id;
                c.Obsolete = false;
                db.Classes.Add(c);
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(model);
        }

        // GET: Classes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Class c = db.Classes.Find(id);
            if (c == null)
            {
                return HttpNotFound();
            }
            ClassEdit model = GetClassEditModel(c);
                        
            return View(model);
        }

        private ClassEdit GetClassEditModel(Class c)
        {
            var user = UserManager.FindById(User.Identity.GetUserId()); //baf xxx
            int libraryId = user.LibraryId;
            ClassEdit model = new ClassEdit();
            model.Classes_Id =c.Classes_Id;
            model.ClassDesc = c.ClassDesc;
            model.Teacher_Id = c.Teacher_Id == null ? 0 : (int)c.Teacher_Id;
            model.Teacher2Id = c.Teacher2Id == null ? 0 : (int)c.Teacher2Id;
            model.Obsolete = c.Obsolete;
            model.Stage = c.Stage;

            IQueryable<TeacherRec> teachers = from t in db.Teachers
                                              where t.Library_Id == libraryId && !t.Inactive
                                              orderby t.FirstName, t.LastName
                                              select new TeacherRec()
                                              {
                                                  Id = t.Teacher_Id,
                                                  Teacher = t.FirstName.Trim() + " " + t.LastName.Trim()
                                              };
            model.Teachers = teachers;
            model.Stages = GetStages();

            return model;
        }

        private Dictionary<string, string> GetStages()
        {
            return new Dictionary<string, string>
            {
                {"0", "Kindergarten & pre-school"},
                {"1", "Stage 1"},
                {"2", "Stage 2"},
                {"3", "Stage 3"}
             };
        }

        // POST: Classes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Class classToUpdate = db.Classes.Find(id);
            if (ModelState.IsValid)
            {
                
                if (TryUpdateModel(classToUpdate, "", new string[] { "ClassDesc", "Teacher_Id", "Teacher2Id", "Obsolete", "Stage" }))
          
                try
                {
                    db.Entry(classToUpdate).State = EntityState.Modified;

                    if (classToUpdate.Teacher_Id == 0)
                        classToUpdate.Teacher_Id = null;
                    if (classToUpdate.Teacher2Id == 0)
                        classToUpdate.Teacher2Id = null;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                 }
            }
            ClassEdit model = GetClassEditModel(classToUpdate);

            return View(model);
        }

        // GET: Classes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Class model = db.Classes.Find(id);
            ClassDetail thisClass = new ClassDetail();
            thisClass.ClassId = (int)id;
            thisClass.ClassName = model.ClassDesc;
            if (model.Teacher != null)
                thisClass.Teacher = model.Teacher.FirstName.Trim() + " " + model.Teacher.LastName.Trim();
            if (model.Teacher1 != null)
            thisClass.Teacher2 = model.Teacher1.FirstName.Trim() + " " + model.Teacher1.LastName.Trim();
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(thisClass);
        }

        // POST: Classes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Class @class = db.Classes.Find(id);
            db.Classes.Remove(@class);
            db.SaveChanges();
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
    }
}
