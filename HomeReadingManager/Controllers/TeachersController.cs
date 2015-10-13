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
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;


namespace HomeReadingManager.Controllers
{
    public class TeachersController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
       
        private ApplicationUserManager _userManager;

        public TeachersController()
        {

        }

        public TeachersController(ApplicationUserManager userManager)
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

        private int CheckForDuplicateTeacher(string first, string last, int teacherId, ref string firstName, ref string lastName)
        {
            int libraryId = 6;//Convert.ToInt32(Session["libkey"].ToString());
            int duplicateId = 0;

            var query = (from t in db.Teachers
                         where t.FirstName.Equals(first, StringComparison.OrdinalIgnoreCase) && t.LastName.Equals(last, StringComparison.OrdinalIgnoreCase)
                         && t.Teacher_Id != teacherId && t.Library_Id == libraryId
                         select t).FirstOrDefault();
            if (query != null)
            {
                duplicateId = query.Teacher_Id;
                firstName = query.FirstName.TrimEnd();
                lastName = query.LastName.TrimEnd();
            }

            return duplicateId;
        }

        [Authorize(Roles = "Teacher, Supervisor")]
        public ActionResult Index(string message, string sortOrder, string newOrder, int? libraryId, int? page, bool showInactive = false, bool asc = true)
        {
            if (libraryId == null)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Teachers/Index" });

                libraryId = user.LibraryId;
            }
            bool isAdministrator = User.IsInRole("Administrator");

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

            
            IQueryable<TeacherList> teacherList = from t in db.Teachers
                                                  where t.Library_Id == (int)libraryId && t.Inactive == showInactive
                                                  select new TeacherList()
                                                  {
                                                      Teacher_Id = t.Teacher_Id,
                                                      FirstName = t.FirstName,
                                                      LastName = t.LastName,
                                                      Inactive = (bool)t.Inactive,
                                                      Email = t.Email,
                                                      Salutation = t.ContactTitle.Salutation,
                                                      AllowEdit = (isAdministrator || t.Email == User.Identity.Name)
                                                  };
            //isAdministrator || 
            switch (sortOrder)
            {
                case "LastName":
                    if (asc)
                        teacherList = teacherList.OrderBy(s => s.LastName).ThenBy(n => n.FirstName);
                    else
                        teacherList = teacherList.OrderByDescending(s => s.LastName).ThenBy(n => n.FirstName);
                    break;

                default:
                    if (asc)
                        teacherList = teacherList.OrderBy(s => s.FirstName).ThenBy(n => n.LastName);
                    else
                        teacherList = teacherList.OrderByDescending(s => s.FirstName).ThenBy(n => n.LastName);
                    break;
            }

            int pageSize = 13;
            int pageNumber = (page ?? 1);

            var teachers = from t in db.Teachers
                           where t.Library_Id == (int)libraryId && t.Inactive == showInactive
                           select t;

            TeachersVM model = new TeachersVM();
            model.LibraryId = (int)libraryId;
            model.Page = (page ?? 1);
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Teachers = teacherList.ToPagedList(pageNumber, pageSize);
            model.ShowInactive = showInactive;
            model.IsAdministrator = isAdministrator;
            model.Message = message;

            return View(model);
        }

        public JsonResult CreateTeacher(string firstName, string lastName, int titleId, int role, int libraryId, string email)
        {
            var user = UserManager.FindByName(email);
            if (user != null)
                return Json(new { Success = false, Result = "A user with this email is already in the database." }, JsonRequestBehavior.AllowGet);

            Teacher teacher = new Teacher();
            teacher.FirstName = firstName;
            teacher.LastName = lastName;
            teacher.Title_Id = titleId;
            teacher.Library_Id = libraryId;
            teacher.Inactive = false;
            teacher.Role = role;
            teacher.Email = email;

            try
            {
                db.Teachers.Add(teacher);
                db.SaveChanges();
                return Json(new { Success = true, Result = teacher.Teacher_Id }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { Success = false, Result = "Failed to write to file." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsEmailUnique(string Email)
        {
            if (String.IsNullOrEmpty(Email))
                return Json(true, JsonRequestBehavior.AllowGet);

            try
            {
                var user = UserManager.FindByName(Email);

                return Json(user == null, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        private Dictionary<string, string> GetRoles()
        {
            return new Dictionary<string, string>
            {
                {"1", "Parent helper"},
                {"2", "Teacher"},
                {"3", "Supervisor"}
             };
        }
        // GET: Teachers/Create
        public ActionResult Create(string sortOrder, int? page, int? libraryId, bool asc = true)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "~/Teachers/Index" });

            if (! User.IsInRole("Administrator"))
                return RedirectToAction("Index", "Teachers", new { sortOrder = sortOrder, libraryId = libraryId, asc = asc, page =page, message = "You are not authorised to create users." });
          
            TeacherCreate model = new TeacherCreate();

            model.LibraryId = (int)libraryId;
            model.Role = 2;
            model.TeacherId = 0;
            model.Title_Id = 0;
            model.Inactive = false;
            model.Administrator = false;
            model.Roles = GetRoles();
            model.SortOrder = sortOrder;
            model.Page = (int)page;
            model.Ascending = asc;


            IQueryable<Salutation> salutations = from t in db.ContactTitles
                                                 orderby t.Title_Id
                                                 select new Salutation()
                                                 {
                                                     Id = t.Title_Id,
                                                     Title = t.Salutation
                                                 };
            model.Salutations = salutations;

            return View(model);
        }

        // POST: Teachers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(TeacherCreate model, string sortOrder, bool asc = true)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email, LibraryId = model.LibraryId, TeacherId = model.TeacherId, EmailConfirmed = true }; //
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    string role = "";
                    switch (model.Role)
                    {
                        case 1:
                            role = "Parent helper";
                            break;
                        case 3:
                            role = "Supervisor";
                            break;
                        default:
                            role = "Teacher";
                            break;
                    }
                    UserManager.AddToRole(user.Id, role);
                    if (model.Administrator)
                        UserManager.AddToRole(user.Id, "Administrator");


                    // await SignInAsync(user, isPersistent: false);
                    var thisUser = UserManager.FindById(User.Identity.GetUserId());
                    string me = "The school";
                    if (thisUser == null)
                    {
                        var query = (from u in db.Teachers
                                    where u.Teacher_Id == thisUser.TeacherId
                                    select u).FirstOrDefault();
                         if (query.Teacher_Id > 0)
                         {
                             me = query.FirstName.Trim() + " " + query.LastName.Trim();
                         }
                    }
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                     string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                     var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                     await UserManager.SendEmailAsync(user.Id, "Confirm your account", 
                        me +  " has created a login for you for the Classroom Manager website. Please confirm your email address by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Teachers");
                }
                else
                {
                    Teacher teacher = db.Teachers.Find(model.TeacherId);
                    db.Teachers.Remove(teacher);
                    db.SaveChanges();
                    model.TeacherId = 0;
                    model.Message = "Failed to create user please try again.";
                    // AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Teachers/Edit/5
        public ActionResult Edit(int? id, string sortOrder, bool asc, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Teacher teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }

            TeacherCreate model = GetTeacherCreateForEdit(teacher.Teacher_Id);

            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;
            return View(model);
        }

        private TeacherCreate GetTeacherCreateForEdit(int id)
        {
            Teacher teacher = db.Teachers.Find(id);
            TeacherCreate model = new TeacherCreate();
            model.LibraryId = teacher.Library_Id;
            model.Role = teacher.Role;
            model.TeacherId = teacher.Teacher_Id;
            model.Title_Id = teacher.Title_Id == null ? 1 : (int)teacher.Title_Id;
            model.Inactive = teacher.Inactive;
            new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
            var userTeacher = UserManager.FindByName(teacher.Email).Id;
            model.Administrator = userTeacher != null && UserManager.IsInRole(userTeacher, "Administrator");
            model.IsAdministrator = User.IsInRole("Administrator");
            model.Roles = GetRoles();

            IQueryable<Salutation> salutations = from t in db.ContactTitles
                                                 orderby t.Title_Id
                                                 select new Salutation()
                                                 {
                                                     Id = t.Title_Id,
                                                     Title = t.Salutation
                                                 };
            model.FirstName = teacher.FirstName;
            model.LastName = teacher.LastName;
            if (teacher.Title_Id > 2)
                model.FullName = teacher.ContactTitle.Salutation.Trim() + " " + teacher.FirstName.Trim() + " " + teacher.LastName.Trim();
            else
                model.FullName = teacher.FirstName.Trim() + " " + teacher.LastName.Trim();
            model.Salutations = salutations;
            model.Email = teacher.Email;

            return model;

        }
        // POST: Teachers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "LastName,FirstName,Teacher_Id,Inactive,Email,Library_Id,Title_Id,UserName")] Teacher teacher)
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id, string sortOrder, int? page, bool asc = true, bool administrator = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
            var teacherToUpdate = db.Teachers.Find(id);

            var userTeacher = UserManager.FindByName(teacherToUpdate.Email);
            if (userTeacher != null && (User.IsInRole("Administrator") || userTeacher.Id == teacherToUpdate.Email) &&
                TryUpdateModel(teacherToUpdate, "", new string[] { "FirstName", "LastName", "Email", "Title_Id", "Inactive", "Role" }))
            {
                try
                {
                    db.Entry(teacherToUpdate).State = EntityState.Modified;

                    if (teacherToUpdate.Title_Id == 0)
                        teacherToUpdate.Title_Id = null;

                    db.SaveChanges();
                    if (User.IsInRole("Administrator"))
                    {
                        switch (teacherToUpdate.Role)
                        {
                            case 1:
                                if (!UserManager.IsInRole(userTeacher.Id, "Parent helper"))
                                {
                                    UserManager.AddToRole(userTeacher.Id, "Parent helper");
                                    if (UserManager.IsInRole(userTeacher.Id, "Teacher"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Teacher");
                                    if (UserManager.IsInRole(userTeacher.Id, "Supervisor"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Supervisor");
                                }
                                break;
                            case 3:
                                if (!UserManager.IsInRole(userTeacher.Id, "Supervisor"))
                                {
                                    UserManager.AddToRole(userTeacher.Id, "Supervisor");
                                    if (UserManager.IsInRole(userTeacher.Id, "Teacher"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Teacher");
                                    if (UserManager.IsInRole(userTeacher.Id, "Parent helper"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Parent helper");
                                }
                                break;
                            case 2:

                                if (!UserManager.IsInRole(userTeacher.Id, "Teacher"))
                                {
                                    UserManager.AddToRole(userTeacher.Id, "Teacher");
                                    if (UserManager.IsInRole(userTeacher.Id, "Parent helper"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Parent helper");
                                    if (UserManager.IsInRole(userTeacher.Id, "Supervisor"))
                                        UserManager.RemoveFromRole(userTeacher.Id, "Supervisor");
                                }
                                break;
                        }
                       
                        if (administrator)
                        {
                            if (!UserManager.IsInRole(userTeacher.Id, "Administrator"))
                                UserManager.AddToRole(userTeacher.Id, "Administrator");
                        }
                        else
                        {
                            if (UserManager.IsInRole(userTeacher.Id, "Administrator"))
                                UserManager.RemoveFromRole(userTeacher.Id, "Administrator");
                        }
                       
                    }
                    return RedirectToAction("Index", new { sortOrder = sortOrder, asc = asc, page = page });

                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            TeacherCreate model = GetTeacherCreateForEdit((int)id);

            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;
            return View(model);

        }

        // GET: Teachers/Delete/5
        public ActionResult Delete(int? id, string sortOrder, int? page, int? libraryId, bool asc = true)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Teacher teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            TeacherCreate model = new TeacherCreate();
            model.TeacherId = teacher.Teacher_Id;
            model.FirstName = teacher.FirstName;
            model.LastName = teacher.LastName;
            model.FullName = teacher.FirstName.Trim() + " " + teacher.LastName.Trim();
            model.Email = teacher.Email;
            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.LibraryId = (int)libraryId;
            model.Page = page ?? 1;
            return View(model);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string sortOrder, int? page, bool asc = true)
        {
            Teacher teacher = db.Teachers.Find(id);
            
            var classes = from c in db.Classes
                            where c.Teacher_Id == id
                            select c;
            foreach (var item in classes)
            {
                item.Teacher_Id = null;
            }
            
            db.Teachers.Remove(teacher);
            db.SaveChanges();

            return RedirectToAction("Index", new { sortOrder = sortOrder, asc = asc, page = page });
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
