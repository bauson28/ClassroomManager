using HomeReadingManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HomeReadingManager.ViewModels;
using System.Net;
using PagedList;
using System.Data;
using System.IO;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity;

namespace HomeReadingManager.Controllers
{
    public class CsvImportController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;

        public CsvImportController()
        {
        }

        public CsvImportController(ApplicationUserManager userManager)
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

        // GET: CsvImport
       
        public ActionResult Index(string message, string sortOrder, string newOrder, int? libraryId, int? userId, int? page, int? fileAction, bool searchReturn = false, bool asc = true)
        {
            
            //if (libraryId == null)
           // {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Home/Index" });
                libraryId = user.LibraryId;
                userId = user.TeacherId;
                bool isManager = User.IsInRole("Manager");
                if (!isManager)
                    return RedirectToAction("Login", "Account", new { returnUrl = "~/Home/Index" });
           // }

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

            fileAction = (fileAction == null) ? 0 : fileAction;
            if (fileAction == 3 && !DeleteData((int)userId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var query = (from u in db.ImportFiles
                         where u.UserId == userId
                         select u).FirstOrDefault();


            if (query == null || !isManager)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest); 
                //}
                //if (query.ImportDatas.Count == 0)
                // {
                return RedirectToAction("SelectFile", new { message = message, libraryId = libraryId, userId = userId });
            }
            var model = new Imports();
            model.LibraryId = (int)libraryId;
            model.UserId = (int)userId;
            model.Message = message;
            model.ImportFile_Id = query.ImportFile_Id;
            model.ImportType = query.ImportType == null ? 1 : (int)query.ImportType;
            switch (model.ImportType)
            {
                default:
                    model.Description = "Import Teachers and/or Classes";
                    break;

                case 1:
                    model.Description = "Assign Students to Classes";
                    break;

                case 2:
                    model.Description = "Import Students";
                    break;

                case 3:
                    model.Description = "Import Books";
                    break;
            }
            model.HasRows = db.ImportDatas.Any(x => x.ImportFile_Id.Equals(model.ImportFile_Id));
            model.NoOfCols = (from m in db.ImportMaps
                              where m.ImportFile_Id == model.ImportFile_Id
                              select m).Count();

            if (fileAction == 1)
            {
                if (ValidateData(model.ImportType, model.NoOfCols, model.ImportFile_Id, model.LibraryId, model.UserId))
                {
                    //CustomValidator1.IsValid = false;
                    //CustomValidator1.ErrorMessage = "Import file is valid.";
                }
            }
            else if (fileAction == 2)
            {
                if (ImportFile(model.ImportType, model.NoOfCols, model.ImportFile_Id, model.LibraryId, model.UserId)) //baf xxx need to get number of records imported
                {

                    if (DeleteData(model.UserId))
                        return RedirectToAction("SelectFile");
                    else
                    {
                        //CustomValidator1.IsValid = false;
                        //CustomValidator1.ErrorMessage = "File import bit failed to delete temporary file.";
                    }

                }
            }

            model.SortOrder = sortOrder;
            model.Ascending = asc;
            model.Page = page ?? 1;

            return View(model);
        }

        //GET
        public ActionResult SelectFile(string message, int libraryId, int userId)
        {
            var model = new Imports();

            model.ImportFile_Id = 0;
            model.ImportType = 0;
            model.Description = "Import Teachers and/or Classes";
            model.HasRows = false;
            model.NoOfCols = 0;
            model.LibraryId = libraryId;
            model.UserId = userId;
            model.Message = message;

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult SelectFile(HttpPostedFileBase file, Imports model)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null && User.IsInRole("Manager"))
            {
                StreamReader sr = new StreamReader(file.InputStream);
                string message = " ";
                if (Request.Files["file"] != null && (Request.Files["file"].FileName.ToLower().EndsWith(".csv") || Request.Files["file"].FileName.ToLower().EndsWith(".txt")) && Request.Files["file"].ContentLength > 0)
                {
                   
                    if (ReadCSVFile(sr, model.ImportType, model.HasHeader, model.UserId))
                        return RedirectToAction("Index", new { libraryId = model.LibraryId, userId = model.UserId});
                    else 
                    {
                        message = "Failed to read file" + Request.Files["file"].FileName;
                        return RedirectToAction("Index", new { libraryId = model.LibraryId, userId = model.UserId, message = message });
                    }
                 }
            }

            return View(model);
        }

        public ActionResult RemoveFile(int? libraryId, int? userId, int? page, int? fileAction)
        {
            return RedirectToAction("Index", "CsvImport", new { libraryId = libraryId, userId = userId, page = page, fileAction = fileAction });
        }
        
        private IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue, bool addLine)
        {
            var items = new List<SelectListItem>();
            if (addLine)
                items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
            items.AddRange(list);
            return items;
        }

        private bool DeleteData(int userId)
        {
            bool success = false;

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            var query = from i in db.ImportDatas
                        where i.ImportFile.UserId == userId
                        select i;

            var query2 = (from i2 in db.ImportFiles
                          where i2.UserId == userId
                          select i2).FirstOrDefault();

            if (mappings != null && query != null && query2 != null)
            {
                foreach (var mapping in mappings)
                {
                    db.ImportMaps.Remove(mapping);
                }

                foreach (var row in query)
                {
                    db.ImportDatas.Remove(row);
                }

                db.ImportFiles.Remove(query2);
            }
            try
            {
                db.SaveChanges();

                success = true;
            }
            catch
            {
                //CustomValidator1.ErrorMessage = "Failed to delete temporary tables. Please try again";
                //CustomValidator1.IsValid = false;
            }

            return success;
        }

        private bool ReadCSVFile(StreamReader sr, int reportType, bool hasHeader, int userId) //HttpPostedFileBase file
        {
            int importFileId = 0;
            int cols = 0;
           
            try 
            {
                using (CsvReader reader = new CsvReader(sr))
                {
                    ImportFile i = new ImportFile();
                    i.UserId = userId;
                    i.Entered = DateTime.Now;
                    i.ImportType = reportType;

                    db.ImportFiles.Add(i);
                    try
                    {
                        db.SaveChanges();
                        importFileId = i.ImportFile_Id;
                    }
                    catch (Exception)
                    {
                        // CustomValidator1.IsValid = false;
                        //CustomValidator1.ErrorMessage = "Failed to read the file. You may have an invalid csv file.<br />Please check your data and try again.";
                    }
                    if (importFileId > 0)
                    {
                        int count = 1;

                        foreach (string[] values in reader.RowEnumerator)
                        {
                            if (reader.RowIndex == 1)
                            {
                                foreach (string s in values)
                                {
                                    ImportMap m = new ImportMap();
                                    cols++;
                                    m.ImportFile_Id = importFileId;

                                    if (hasHeader)
                                    {
                                        m.TempCol = s;
                                        string colName = GetColumnName(s, reportType);
                                        if (String.IsNullOrEmpty(colName))
                                            m.MappedCol = "Ignore";
                                        else
                                            m.MappedCol = colName;
                                    }
                                    else
                                    {
                                        m.TempCol = "Column " + cols.ToString();
                                        m.MappedCol = "Ignore";
                                    }
                                    db.ImportMaps.Add(m);
                                }
                            }
                            if (reader.RowIndex > 1 || !hasHeader)
                            {
                                count = 1;
                                ImportData d = new ImportData();
                                d.ImportFile_Id = importFileId;
                                d.Valid = false;
                                foreach (string s in values)
                                {
                                    SetRowField(d, s, count);
                                    count++;
                                }
                                db.ImportDatas.Add(d);
                            }
                        }
                        try
                        {
                            db.SaveChanges();
                            return true;
                        }
                        catch (Exception)
                        {
                            // CustomValidator1.IsValid = false;
                            //CustomValidator1.ErrorMessage = "Failed to read the file. You may have an invalid csv file.<br />Please check your data and try again.";
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                var bf = ex;
            }
            
            return false;
        }

        private void SetRowField(ImportData d, string s, int count)
        {
            switch (count)
            {
                case 1:
                    d.Col1 = s;
                    break;

                case 2:
                    d.Col2 = s;
                    break;

                case 3:
                    d.Col3 = s;
                    break;

                case 4:
                    d.Col4 = s;
                    break;

                case 5:
                    d.Col5 = s;
                    break;

                case 6:
                    d.Col6 = s;
                    break;

                case 7:
                    d.Col7 = s;
                    break;

                case 8:
                    d.Col8 = s;
                    break;

                case 9:
                    d.Col9 = s;
                    break;

                case 10:
                    d.Col10 = s;
                    break;

                case 11:
                    d.Col11 = s;
                    break;

                case 12:
                    d.Col12 = s;
                    break;

                case 13:
                    d.Col13 = s;
                    break;

                case 14:
                    d.Col14 = s;
                    break;

                case 15:
                    d.Col15 = s;
                    break;

                case 16:
                    d.Col16 = s;
                    break;

                case 17:
                    d.Col17 = s;
                    break;

                case 18:
                    d.Col18 = s;
                    break;

                case 19:
                    d.Col19 = s;
                    break;

                case 20:
                    d.Col20 = s;
                    break;
            }
        }

        private string GetColumnName(string name, int reportType)
        {
            string colName = string.Empty;

            switch (reportType)
            {
                case 0: //class/teachers
                    {
                        string[] mapping = { "Ignore", "Class", "Stage", "Title", "First Name", "Last Name", "Full Name", "Email" };
                        int index = Array.IndexOf(mapping, name);
                        if (index >= 0)
                            colName = mapping[index];
                    }
                    break;

                case 1:    //assign student mapping
                    {
                        string[] mapping = { "Ignore", "Student Rec No", "Class" };
                        int index = Array.IndexOf(mapping, name);
                        if (index >= 0)
                            colName = mapping[index];
                    }
                    break;

                case 2: //Students
                    {
                        string[] mapping = { "Ignore", "Student Rec No", "Student First Name", "Student Last Name", "Student Full Name", "Class", "Year", "Read Level", "Books Read", "Parent Title", "Parent First Name", 
                                            "Parent Last Name", "Parent Full Name", "Relationship", "Phone" };
                        int index = Array.IndexOf(mapping, name);
                        if (index >= 0)
                            colName = mapping[index];
                    }
                    break;

                case 3: //Books
                    {
                        string[] mapping = { "Ignore", "Isbn", "Title", "Qty", "Author", "Illustrator", "Annotation", "Read Level" };
                        int index = Array.IndexOf(mapping, name);
                        if (index >= 0)
                            colName = mapping[index];
                    }
                    break;
            }
            return colName;
        }

        private bool ValidateData(int importType, int noOfCols, int importFileId, int libraryId, int userId)
        {
            switch (importType)
            {
                case 0: //teachers
                    return ValidateImportClasses(noOfCols, importFileId, libraryId, userId);

                case 1:    //Class assignments
                    return ValidateAssignStudents(noOfCols, importFileId, libraryId, userId);

                case 2: //Students
                    return ValidateImportStudents(noOfCols, importFileId, libraryId, userId);

                case 3: //Books
                    return ValidateImportBooks(noOfCols, importFileId, libraryId, userId);
            }
            return false;
        }

        private bool ImportFile(int importType, int noOfCols, int importFileId, int libraryId, int userId)
        {
            switch (importType)
            {
                case 0: //teachers
                    return ValidateImportClasses(noOfCols, importFileId, libraryId, userId) && ImportClasses(noOfCols, importFileId, libraryId, userId);

                case 1:    //Class assignments
                    return ValidateAssignStudents(noOfCols, importFileId, libraryId, userId) && AssignStudents(noOfCols, importFileId, libraryId, userId);

                case 2: //Students
                    return ValidateImportStudents(noOfCols, importFileId, libraryId, userId) && ImportStudents(noOfCols, importFileId, libraryId, userId);

                case 3: //Books
                    return ValidateImportBooks(noOfCols, importFileId, libraryId, userId) && ImportBooks(noOfCols, importFileId, libraryId, userId);
            }
            return false;
        }

        private void GetNamesFromFullName(string fullName, ref string titleString, ref string firstName, ref string lastName, string[] salutations, bool titleAsString)
        {
            bool titleFound = false;
            int titleId = 1;

            fullName = fullName.Trim();
            int space = fullName.IndexOf(" ");
            if (space <= 0)
                lastName = fullName;
            else
            {
                string temp = fullName.Substring(0, space).ToUpper();
                switch (temp)
                {
                    case "MR.":
                        {
                            titleId = 2;
                            titleFound = true;
                            break;
                        }
                    case "MRS.":
                        {
                            titleId = 3;
                            titleFound = true;
                            break;
                        }
                    case "MS.":
                        {
                            titleId = 4;
                            titleFound = true;
                            break;
                        }

                    default:
                        {
                            int index = Array.IndexOf(salutations, temp);
                            if (index > 0)
                            {
                                titleId = index + 1;
                                titleFound = true;
                            }
                            else
                                titleId = 1;
                            break;
                        }
                }
                if (titleFound)
                {
                    fullName = fullName.Substring(space + 1);
                    space = fullName.IndexOf(" ");
                }
                if (space == 0)
                    lastName = fullName;
                else
                {
                    lastName = fullName.Substring(space + 1);
                    firstName = fullName.Substring(0, space);
                }
            }
            if (titleAsString)
            {
                if (titleId > 1)
                    titleString = salutations[titleId - 1];
                else
                    titleString = string.Empty;
            }
            else
                titleString = titleId.ToString();
        }

        private void GetNamesFromFullNameNoTitle(string fullName, ref string firstName, ref string lastName)
        {
            int space = fullName.IndexOf(" ");
            if (space == 0)
                lastName = fullName;
            else
            {
                lastName = fullName.Substring(space + 1);
                firstName = fullName.Substring(0, space);
            }
        }

        private int CheckIsbnUnique(string isbn)
        {
            int productId = 0;
            var query = (from p in db.Products
                         where p.Isbn == isbn
                         select p).FirstOrDefault();
            if (query != null)
            {
                productId = query.Product_Id;
                //lbProduct.Text = query.Title;
            }
            return productId;
        }

        private int GetReadLevelId(string readLevel)
        {
            int levelId = 0;
            int level = Convert.ToInt32(readLevel);
            if (level == 0)
                return 0;
            else if (level < 10)
                readLevel = "0" + level.ToString();

            using (var dbContext = new HomeReadingEntities())
            {
                var query = (from l in dbContext.Levels
                             where l.ReadLevel == readLevel
                             select l).FirstOrDefault();
                if (query != null)
                    levelId = query.Levels_Id;
            }
            return levelId;
        }

        private void GetTitleId(string title, ref int titleId, string[] salutations)
        {
            switch (title)
            {
                case "MR.":
                    titleId = 2;
                    break;

                case "MRS.":
                    titleId = 3;
                    break;

                case "MS.":
                    titleId = 4;
                    break;

                default:
                    int index = Array.IndexOf(salutations, title);
                    if (index > 0)
                        titleId = index + 1;
                    else
                        titleId = 1;
                    break;
            }
        }

        private void GetRelationhipId(string relation, ref int relationId, string[] relationship)
        {
            switch (relation)
            {
                case "GRAND PARENT":
                    relationId = 6;
                    break;

                case "STEP PARENT":
                    relationId = 14;
                    break;

                default:
                    int index = Array.IndexOf(relationship, relation);
                    if (index > 0)
                        relationId = index + 1;
                    else
                        relationId = 1;
                    break;
            }
        }

        private int GetGrade(string year)
        {

            var query = (from g in db.Grades
                         where (g.Name.ToLower() == year.ToLower()) || (g.FullName.ToLower() == year.ToLower())
                         select g).FirstOrDefault();
            return query.Id;
        }

        private int CheckExistingTeacher(string email, int libraryId)
        {
            int teacherId = 0;
            if (!string.IsNullOrEmpty(email))
            {
                var query = (from t in db.Teachers
                             where t.Email.ToLower() == email.ToLower() //&& t.Library_Id == libraryId
                             select t).FirstOrDefault();
                if (query != null)
                    teacherId = query.Teacher_Id;
            }
            return teacherId;
        }

        private int CheckExistingClass(string classDesc, int libraryId)
        {
            int classId = 0;
            if (!string.IsNullOrEmpty(classDesc))
            {
                var query = (from t in db.Classes
                             where t.ClassDesc.ToLower() == classDesc.ToLower() && t.Library_Id == libraryId
                             select t).FirstOrDefault();
                if (query != null)
                    classId = query.Classes_Id;
            }
            return classId;
        }

        private int CheckExistingStudent(string firstName, string lastName, int libraryId)
        {
            int studentId = 0;

            var query = (from s in db.Students
                         where s.FirstName.ToLower() == firstName.ToLower() && s.LastName.ToLower() == lastName.ToLower() && s.Library_id == libraryId
                         select s).FirstOrDefault();
            if (query != null)
                studentId = query.Student_Id;
            return studentId;
        }

        private int CheckExistingStudent(string srn, int libraryId)
        {
            int studentId = 0;

            var query = (from s in db.Students
                         where s.SRN == srn && s.Library_id == libraryId
                         select s).FirstOrDefault();
            if (query != null)
                studentId = query.Student_Id;

            query = null;
            return studentId;
        }

        private int CheckExistingParent(string firstName, string lastName, int studentId, int libraryId)
        {
            int parentId = 0;

            var query = (from s in db.StudentContacts
                         where s.Parent.FirstName.ToLower() == firstName.ToLower() && s.Parent.LastName.ToLower() == lastName.ToLower()
                             && s.Student_Id == studentId && s.Parent.Library_Id == libraryId
                         select s).FirstOrDefault();
            if (query != null)
                parentId = query.Parent_Id;

            return parentId;
        }

        private bool ImportClasses(int noOfCols, int importFileId, int libraryId, int userId)
        {
            bool hasErrors = false;
            int importedC = 0; //importedT = 0, 

            var titles = from ct in db.ContactTitles
                         select new
                         {
                             ct.Salutation
                         };

            int titleCount = titles.Count();
            string[] salutations = new string[titleCount];


            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings != null)
            {
                int teacherId = 0, classId = 0, assigned = 0;

                var data = (from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id).ToList();

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                            row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string title = "", firstName = "", lastName = "", classDesc = "", stage = "", fullName = "", email = "", titleString = "";
                    int j = 0;

                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();

                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Title":
                                title = str;
                                break;

                            case "First Name":
                                firstName = str;
                                break;

                            case "Last Name":
                                lastName = str;
                                break;

                            case "Full Name":
                                fullName = str;
                                break;

                            case "Stage":
                                stage = str;
                                break;

                            case "Class":
                                classDesc = str.Trim();
                                break;

                            case "Email":
                                email = str.Trim();
                                break;
                        }
                        j++;
                    }
                    if (!String.IsNullOrEmpty(fullName))
                        GetNamesFromFullName(fullName, ref titleString, ref firstName, ref lastName, salutations, true);
                    teacherId = CheckExistingTeacher(email, libraryId);
                    classId = CheckExistingClass(classDesc, libraryId);

                    //if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    //{
                    //    if (teacherId == 0)
                    //    {
                    //        Teacher t = new Teacher();
                    //        t.Title = titleString;
                    //        t.FirstName = firstName;
                    //        t.LastName = lastName;
                    //        t.Inactive = false;
                    //        t.Email = email;
                    //        t.Library_Id = libraryId;
                    //        db.Teachers.Add(t);
                    //        db.SaveChanges();
                    //        teacherId = t.Teacher_Id;
                    //        importedT++;
                    //    }

                    //    else
                    //    {
                    //        var query = (from t in db.Teachers
                    //                     where t.Teacher_Id == teacherId
                    //                     select t).SingleOrDefault();

                    //        if (query != null)
                    //        {
                    //            if (!string.IsNullOrEmpty(email))
                    //                query.Email = email;
                    //            query.Inactive = false;
                    //        }
                    //    }
                    //}

                    if (!string.IsNullOrEmpty(classDesc))
                    {
                        if (classId == 0)
                        {
                            Class c = new Class();
                            c.ClassDesc = classDesc;
                            c.Obsolete = false;
                            c.Library_Id = libraryId;
                            c.Stage = stage;
                            if (teacherId > 0)
                            {
                                c.Teacher_Id = teacherId;
                                assigned++;
                            }
                            db.Classes.Add(c);
                            //db.SaveChanges();
                            //classId = c.Classes_Id;
                            importedC++;
                        }

                        else
                        {
                            var query = (from c in db.Classes
                                         where c.Classes_Id == classId
                                         select c).SingleOrDefault();

                            if (query != null)
                            {
                                query.Obsolete = false;
                                query.Stage = stage;
                                if (teacherId > 0)
                                {
                                    query.Teacher_Id = teacherId;
                                    assigned++;
                                }
                            }
                        }

                    }
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var bf = ex;
                    hasErrors = true;
                    //CustomValidator1.IsValid = false;
                    //CustomValidator1.ErrorMessage = "There were errors writing to file.";
                }
            }
            if (!hasErrors)
            {
                //CustomValidator1.IsValid = false;
                //CustomValidator1.ErrorMessage = "Import complete. Teachers added: " + importedT.ToString() + "; Classes added: " + importedC.ToString()
                //    + "; Teachers assigned to classes: " + assigned.ToString() + ".";
            }

            return !hasErrors;
        }

        private bool AssignStudents(int noOfCols, int importFileId, int libraryId, int userId)
        {
            int success = 0, failed = 0;
            bool errors = false;

            // CustomValidator cusVal = (CustomValidator)gvFile.FindControl("cusVal1");
            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings != null)
            {
                int studentId = 0, classId = 0;

                var data = (from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id).ToList();

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                    row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string srn = "", classDesc = "";
                    int j = 0;

                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();

                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Student Rec No":
                                srn = str;
                                break;

                            case "Class":
                                classDesc = str;
                                break;
                        }
                        j++;
                    }

                    studentId = CheckExistingStudent(srn, libraryId);
                    classId = CheckExistingClass(classDesc, libraryId);

                    if (studentId > 0 && classId > 0)
                    {
                        var query = (from s in db.Students
                                     where s.Student_Id == studentId
                                     select s).SingleOrDefault();

                        if (query != null)
                        {
                            query.Classes_Id = classId;
                            query.Inactive = false;
                            try
                            {
                                db.SaveChanges();
                                success++;
                            }
                            catch (Exception)
                            {
                                errors = true;
                                failed++;
                            }

                        }
                        else
                        {
                            failed++;
                        }
                    }
                }

            }
            //CustomValidator1.IsValid = false;
            // if (errors)
            //CustomValidator1.ErrorMessage = "There were errors writing to file. Please try again.";
            //else
            //CustomValidator1.ErrorMessage = "Students assigned to classes: Assigned: " + success.ToString() + "  Failed: " + failed.ToString();

            return (success > 0 && failed == 0 && !errors);
        }

        private bool ImportStudents(int noOfCols, int importFileId, int libraryId, int userId)
        {
            int i = 0, added = 0, failed = 0, updated = 0;  //, parentAdded = 0, parentUpdated = 0;
            //int singleUserClass = 0;
            //if (Session["userType"].ToString() == "S")
            //{
            //    singleUserClass = GetSingleUserClass();
            // }

            var titles = from ct in db.ContactTitles
                         select ct;

            string[] salutations = new string[titles.Count()];
            i = 0;
            foreach (var t in titles)
            {
                salutations[i] = t.Salutation.ToUpper();
                i++;
            }

            var relations = from re in db.Relationships
                            select re;

            string[] relationships = new string[relations.Count()];
            i = 0;
            foreach (var x in relations)
            {
                relationships[i] = x.Relationship1.ToUpper();
                i++;
            }

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings != null)
            {
                var data = (from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id).ToList();

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                        row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string srn = "", firstName = "", lastName = "", classDesc = "", readLevel = "", fullName = "", phone1 = "", phone2 = "", year = "";
                    int studentId = 0, booksRead = 0, titleId = 0, relationId = 0; //, gradeId = 0
                    bool parentFull1 = false, title1Set = false, relation1Set = false, firstName1Set = false, lastName1Set = false, parent2Set = false;
                    string[] parents = { "1", "1", "", "", "", "", "", "1", "1", "", "", "", "", "" }; //title_id, relation_id, firstName, lastName, fullname, phone, parentId then repeats
                    int j = 0;
                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        if (str != "&nbsp;" && !String.IsNullOrEmpty(str))
                        {
                            switch (mapping.MappedCol)
                            {
                                case "Ignore":
                                    break;

                                case "Student Rec No":
                                    srn = str;
                                    break;

                                case "Student First Name":
                                    firstName = str;
                                    break;

                                case "Student Last Name":
                                    lastName = str;
                                    break;

                                case "Student Full Name":
                                    fullName = str;
                                    break;

                                case "Class":
                                    classDesc = str;
                                    break;

                                case "Year":
                                    year = str;
                                    break;

                                case "Read Level":
                                    readLevel = str;
                                    break;

                                case "Books Read":
                                    //if (String.IsNullOrEmpty(str))
                                    //    booksRead = 0;
                                    // else
                                    booksRead = Convert.ToInt32(str);
                                    break;

                                case "Parent Title":
                                    GetTitleId(str.ToUpper(), ref titleId, salutations);
                                    if (title1Set)
                                        parents[7] = titleId.ToString();
                                    else
                                    {
                                        parents[0] = titleId.ToString();
                                        title1Set = true;
                                    }
                                    break;

                                case "Relationship":
                                    GetRelationhipId(str.ToUpper(), ref relationId, relationships);
                                    if (relation1Set)
                                        parents[8] = relationId.ToString();
                                    else
                                    {
                                        parents[1] = relationId.ToString();
                                        relation1Set = true;
                                    }
                                    break;

                                case "Parent First Name":
                                    if (firstName1Set)
                                        parents[9] = str;
                                    else
                                    {
                                        parents[2] = str;
                                        firstName1Set = true;
                                    }
                                    break;

                                case "Parent Last Name":
                                    if (lastName1Set)
                                    {
                                        parents[10] = str;
                                        parent2Set = true;
                                    }
                                    else
                                    {
                                        parents[3] = str;
                                        lastName1Set = true;
                                    }
                                    break;

                                case "Parent Full Name":

                                    if (parentFull1)
                                    {
                                        parent2Set = true;
                                        parents[11] = str;
                                    }
                                    else
                                    {
                                        parentFull1 = true;
                                        parents[4] = str;
                                    }
                                    break;

                                case "Phone":
                                    str = GetPhoneNumber(str);
                                    if (!String.IsNullOrEmpty(str))
                                    {
                                        if (String.IsNullOrEmpty(phone1))
                                            phone1 = str;
                                        else
                                            phone2 = str;

                                        if (parent2Set)
                                            parents[12] = str;
                                        else if (parentFull1 || lastName1Set)
                                            parents[5] = str;
                                    }
                                    break;
                            }
                        }
                        j++;
                    }
                    if (!String.IsNullOrEmpty(fullName))
                        GetNamesFromFullNameNoTitle(fullName, ref firstName, ref lastName);
                    if (!String.IsNullOrEmpty(parents[4]))
                        GetNamesFromFullName(parents[4], ref parents[0], ref parents[2], ref parents[3], salutations, false);
                    if (!String.IsNullOrEmpty(parents[11]))
                        GetNamesFromFullName(parents[11], ref parents[8], ref parents[9], ref parents[10], salutations, false);

                   
                    //studentId = CheckExistingStudent(srn, libraryId);
                    studentId = CheckExistingStudent(firstName.Trim(), lastName.Trim(), libraryId);
                   
                    //try
                    //{
                        if (studentId == 0)
                        {
                            //Student s = new Student(); //baf temp MDPS
                            //s.SRN = srn;
                            //gradeId = GetGrade(year);
                            //if (gradeId > 0)
                            //    s.GradeId = gradeId;
                            //s.FirstName = firstName;
                            //s.LastName = lastName;
                            //if (string.IsNullOrEmpty(parents[3]) && string.IsNullOrEmpty(parents[10]))
                            //{
                            //    if (!string.IsNullOrEmpty(phone1))
                            //        s.Phone = phone1;
                            //    else if (!string.IsNullOrEmpty(phone2))
                            //        s.Phone = phone2;
                            //}
                            //if (!String.IsNullOrEmpty(readLevel))
                            //{
                            //    int levelId = GetReadLevelId(readLevel);
                            //    if (levelId > 0)
                            //        s.Levels_Id = levelId;
                            //}

                            //if (singleUserClass > 0)
                            //    s.Classes_Id = singleUserClass;

                            //else if (!String.IsNullOrEmpty(classDesc))
                            //{
                            //    int classId = CheckExistingClass(classDesc, libraryId);
                            //    //if (classId > 0)
                            //    s.Classes_Id = classId;
                            //}

                            //else
                            //    s.Classes_Id = null;

                            //s.Library_id = libraryId;
                            //s.Inactive = false;

                            //db.Students.Add(s);
                            //added++;
                        }
                        else
                        {
                            var stud = db.Students.Find(studentId);
                            stud.SRN = srn;
                            switch(year)
                            {
                                case "P":
                                    stud.GradeId = 1;
                                    break;
                                case "K":
                                    stud.GradeId = 2;
                                    break;
                                case "1":
                                    stud.GradeId = 3;
                                    break;
                                case "2":
                                    stud.GradeId = 4;
                                    break;
                                case "3":
                                    stud.GradeId = 5;
                                     break;
                                case "4":
                                    stud.GradeId = 6;
                                    break;
                                case "5":
                                    stud.GradeId = 7;
                                    break;
                                case "6":
                                    stud.GradeId = 8;
                                    break;
                             }
                            try
                            {
                                db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                var bf = ex;
                                failed++;
                            }
                        }
                    //}
                    //catch (Exception ex)
                    //{
                    //    var bf = ex;
                    //    failed++;
                    //}
                //}
                //try 
                //{ 
                //    db.SaveChanges(); 
                //}
                //catch (Exception ex)
                //{
                //    var bf = ex;
                //    failed++;
                }
            }
           return (failed == 0 && (updated > 0 || added > 0));
        }

        private bool ImportBooks(int noOfCols, int importFileId, int libraryId, int userId)
        {
            int failed = 0, added = 0, already = 0;

            // CustomValidator cusVal = (CustomValidator)gvFile.FindControl("cusVal1");
            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings != null)
            {
                var data = (from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id).ToList();

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                            row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string isbn = "", title = "", annotation = "", readLevel = "";
                    int productId = 0, qty = 1;
                    ArrayList authors = new ArrayList();
                    ArrayList illustrators = new ArrayList();
                    int j = 0;
                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Isbn":
                                isbn = str;
                                break;

                            case "Title":
                                title = str;
                                break;

                            case "Annotation":
                                annotation = str;
                                break;

                            case "Read Level":
                                readLevel = str;
                                break;

                            case "Qty":
                                qty = Convert.ToInt32(str);
                                if (qty < 0)
                                    qty = 1;
                                break;

                            case "Author":
                                if (!string.IsNullOrEmpty(str))
                                    authors.Add(str);
                                break;

                            case "Illustrator":
                                if (!string.IsNullOrEmpty(str))
                                    illustrators.Add(str);
                                break;
                        }
                        j++;

                        if (CheckIsbnUnique(isbn) == 0)
                        {
                            Product p = new Product();
                            p.Title = title;
                            p.Isbn = isbn;

                            if (!String.IsNullOrEmpty(readLevel))
                            {
                                int levelId = GetReadLevelId(readLevel);
                                if (levelId > 0)
                                    p.Levels_Id = levelId;
                            }
                            p.Library_Id = libraryId;
                            p.Authorised = false;
                            p.Inactive = false;
                            p.Entered = DateTime.Now;
                            db.Products.Add(p);

                            try
                            {
                                db.SaveChanges();
                                added++;
                                productId = p.Product_Id;
                                ProdStock pq = new ProdStock();
                                pq.Product_Id = productId;
                                pq.Onhand = qty;
                                pq.Library_Id = 2;
                                db.ProdStocks.Add(pq);

                                for (int i = 0; i < authors.Count; i++)
                                {
                                    ProdAuthor a = new ProdAuthor();
                                    a.Product_Id = productId;
                                    a.Author = authors[i].ToString();
                                    a.Role_Id = 1;
                                    db.ProdAuthors.Add(a);
                                }

                                for (int i = 0; i < illustrators.Count; i++)
                                {
                                    ProdAuthor a = new ProdAuthor();
                                    a.Product_Id = productId;
                                    a.Author = illustrators[i].ToString();
                                    a.Role_Id = 12;
                                    db.ProdAuthors.Add(a);
                                }

                                if (annotation != string.Empty)
                                {
                                    Annotation pa = new Annotation();
                                    pa.Product_Id = productId;
                                    pa.Description = annotation;
                                    pa.AnnotType = "1";
                                    pa.Updated = DateTime.Now;
                                    db.Annotations.Add(pa);
                                }

                                db.SaveChanges();

                            }
                            catch (Exception)
                            {
                                failed++;
                            }
                        }
                        else
                        {
                            already++;
                        }
                    }
                }
            }

            // CustomValidator1.IsValid = false;
            //CustomValidator1.ErrorMessage = "Title records: Added: " + added.ToString() + "  Failed: " + failed.ToString() + "   Already in database: " + already.ToString();

            return (failed == 0 && added > 0);
        }

        private bool ValidateAssignStudents(int noOfCols, int importFileId, int libraryId, int userId)
        {
            bool invalidLines = false, errors = false;


            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings == null)
                return false;
            else
            {
                int length = mappings.Count();
                int i = 0;
                string[] mapArray = new string[length];
                foreach (var mapping in mappings)
                {
                    if (mapping.MappedCol != "Ignore")
                    {
                        if (mapArray.Contains(mapping.MappedCol))
                        {
                            //CustomValidator1.ErrorMessage = "Column " + mapping.MappedCol + " is duplicated.";
                            //CustomValidator1.IsValid = false;
                            return false;
                        }
                        else
                        {
                            mapArray[i] = mapping.MappedCol;
                            i++;
                        }
                    }
                }
                if (!mapArray.Contains("Student Rec No"))
                {
                    //CustomValidator1.ErrorMessage = "You must map the student name column(s).";
                    //CustomValidator1.IsValid = false;
                    return false;
                }
                else if (!mapArray.Contains("Class"))
                {
                    //CustomValidator1.ErrorMessage = "You must map the class column.";
                    //CustomValidator1.IsValid = false;
                    return false;
                }

                int studentId = 0, classId = 0;

                var data = from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id;

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                            row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string srn = "", classDesc = "";
                    int j = 0;

                    foreach (var mapping in mappings)
                    {
                        //bool foundIt = false;
                        //for (int j = 0; j < noOfCols; j++)
                        //{
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Student Rec No":
                                srn = str.Trim();
                                //foundIt = true;
                                break;

                            case "Class":
                                classDesc = str.Trim();
                                break;
                        }
                        j++;
                    }

                    studentId = CheckExistingStudent(srn, libraryId);
                    classId = CheckExistingClass(classDesc, libraryId);

                    var query = (from s in db.ImportDatas
                                 where s.ImportData_Id == row.ImportData_Id
                                 select s).SingleOrDefault();

                    if (query != null)
                    {
                        query.Valid = (studentId > 0 && classId > 0);
                        if (studentId == 0 || classId == 0)
                            invalidLines = true;
                    }

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    //CustomValidator1.IsValid = false;
                    //CustomValidator1.ErrorMessage = "Failed to complete validation. Please try again.";
                    errors = true;
                }
            }
            return (!invalidLines && !errors);
        }

        private bool ValidateImportClasses(int noOfCols, int importFileId, int libraryId, int userId)
        {
            bool invalidLines = false;

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings == null)
                return false;
            else
            {
                int length = mappings.Count();
                int i = 0;
                string[] mapArray = new string[length];
                bool doTeachers = false, doClasses = false;
                foreach (var mapping in mappings)
                {
                    if (mapping.MappedCol != "Ignore")
                    {
                        if (mapArray.Contains(mapping.MappedCol))
                        {
                            //CustomValidator1.ErrorMessage = "Column " + mapping.MappedCol + " is duplicated.";
                            //CustomValidator1.IsValid = false;
                            return false;
                        }
                        else if ((mapping.MappedCol == "First Name" || mapping.MappedCol == "Last Name") && mapArray.Contains("Full Name"))
                        {
                            //CustomValidator1.ErrorMessage = "You cannot have both a Full Name column and part name columns.";
                            //CustomValidator1.IsValid = false;
                            return false;
                        }
                        else
                        {
                            mapArray[i] = mapping.MappedCol;
                            i++;
                        }
                    }
                }

                if (mapArray.Contains("First Name") && !mapArray.Contains("Last Name") || !mapArray.Contains("First Name") && mapArray.Contains("Last Name"))
                {
                    //CustomValidator1.ErrorMessage = "Teacher first and last names are required.";
                    //CustomValidator1.IsValid = false;
                    return false;
                }
                else if (((mapArray.Contains("First Name") && mapArray.Contains("Last Name")) || mapArray.Contains("Full Name")) && mapArray.Contains("Class"))
                {
                    doTeachers = true;
                    doClasses = true;
                }

                else if (mapArray.Contains("Class"))
                    doClasses = true;

                else if (mapArray.Contains("First Name") || mapArray.Contains("FullName"))
                    doTeachers = true;

                else if (!mapArray.Contains("Stage"))
                    return false;
                else
                {
                    //CustomValidator1.ErrorMessage = "You must map the teacher names and/or the classes.";
                    //CustomValidator1.IsValid = false;
                    return false;
                }

                var data = from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id;

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                        row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string firstName = "", lastName = "", classDesc = "", fullName = "", stage = "";
                    int j = 0;
                    bool valid = true;

                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;
                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "First Name":
                                firstName = str.Trim();
                                break;

                            case "Last Name":
                                lastName = str.Trim();
                                break;

                            case "Full Name":
                                fullName = str.Trim();
                                break;

                            case "Class":
                                classDesc = str.Trim();
                                break;

                            case "Stage":
                                stage = str.Trim();
                                break;
                        }
                        j++;
                    }
                    if (doTeachers)
                    {
                        if (!String.IsNullOrEmpty(fullName))
                            GetNamesFromFullNameNoTitle(fullName, ref firstName, ref lastName);
                        valid = !String.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName);
                    }

                    if (!doTeachers && doClasses)
                        valid = !String.IsNullOrEmpty(classDesc);

                    var query = (from s in db.ImportDatas
                                 where s.ImportData_Id == row.ImportData_Id
                                 select s).SingleOrDefault();
                    if (query != null)
                    {
                        query.Valid = valid;
                        if (!valid) invalidLines = true;
                    }

                }
                try
                {
                    db.SaveChanges();
                }

                catch (Exception)
                {
                    //CustomValidator1.IsValid = false;
                    //CustomValidator1.ErrorMessage = "Failed to complete validation. Please try again.";
                }
            }

            return !invalidLines;
        }

        private bool ValidateImportStudents(int noOfCols, int importFileId, int libraryId, int userId)
        {
            bool invalidLines = false, errors = false;
            int invalidRecords = 0, validRecords = 0;

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           orderby im.MappedCol
                           select im;

            if (mappings == null)
                return false;
            else
            {
                int length = mappings.Count();
                int i = 0;
                string[] mapArray = new string[length];
                string lastField = "";
                int count = 0;
                foreach (var mapping in mappings)
                {
                    if (mapping.MappedCol == "Ignore")
                        lastField = "Ignore";

                    else
                    {
                        if (mapping.MappedCol == lastField)
                        {
                            if (mapping.MappedCol == "Parent First Name" || mapping.MappedCol == "Parent Last Name" || mapping.MappedCol == "Parent Full Name"
                                || mapping.MappedCol == "Parent Title" || mapping.MappedCol == "Phone" || mapping.MappedCol == "Relationship")
                            {
                                if (count > 1)
                                {
                                    //if (mapping.MappedCol == "Phone")
                                    // CustomValidator1.ErrorMessage = "You can import a maximum of two phone numbers.";
                                    //else
                                    //CustomValidator1.ErrorMessage = "You can import a maximum of two parents/guardians.";
                                    //CustomValidator1.IsValid = false;
                                    return false;
                                }
                                else
                                {
                                    count++;
                                }
                                if (mapping.MappedCol == "Parent Full Name" && mapArray.Contains("Parent First Name") || mapArray.Contains("Parent Last Name"))
                                {
                                    //CustomValidator1.ErrorMessage = "You cannot have both Parent Full Name columns and parent part name columns.";
                                    //CustomValidator1.IsValid = false;
                                    return false;
                                }
                            }
                            else if (mapArray.Contains(mapping.MappedCol))
                            {
                                //CustomValidator1.ErrorMessage = "Column " + mapping.MappedCol + " is duplicated.";
                                //CustomValidator1.IsValid = false;
                                return false;
                            }
                            else if ((mapping.MappedCol == "Student First Name" || mapping.MappedCol == "Student Last Name") && mapArray.Contains("Student Full Name"))
                            {
                                //CustomValidator1.ErrorMessage = "You cannot have a student Full Name column and student part name columns.";
                                //CustomValidator1.IsValid = false;
                                return false;
                            }
                            else if ((mapArray.Contains("Student First Name") || mapArray.Contains("Student Last Name")) && mapping.MappedCol == "Student Full Name")
                            {
                                //CustomValidator1.ErrorMessage = "You cannot have a Student Full Name column and student part name columns.";
                                //CustomValidator1.IsValid = false;
                                return false;
                            }
                        }
                        else
                        {
                            count = 1;
                            lastField = mapping.MappedCol;
                            mapArray[i] = mapping.MappedCol;
                            i++;
                        }
                    }
                }

                if ((!mapArray.Contains("Student First Name") && !mapArray.Contains("Student Last Name")) && !mapArray.Contains("Student Full Name"))
                {
                    //CustomValidator1.ErrorMessage = "You must map the student names.";
                    //CustomValidator1.IsValid = false;
                    return false;
                }

                mappings = mappings.OrderBy(x => x.ImportMap_Id);
                //gvFile.SelectedIndex = 0;
                int existing = 0;

                var data = from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id;

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                        row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};

                    string srn = "", firstName = "", lastName = "", classDesc = "", readLevel = "", fullName = "", year = "";
                    int booksRead = 0;
                    bool isValid = true;
                    string[] parents = { "", "", "", "", "", "", "", "", "", "" };
                    int j = 0;
                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Student Rec No":
                                srn = str;
                                break;

                            case "Student First Name":
                                firstName = str;
                                break;

                            case "Student Last Name":
                                lastName = str;
                                break;

                            case "Student Full Name":
                                fullName = str;
                                break;

                            case "Class":
                                classDesc = str;
                                break;

                            case "Year":
                                year = str;
                                break;

                            case "Read Level":
                                readLevel = str;
                                break;

                            case "Books Read":
                                booksRead = Convert.ToInt32(str);
                                break;
                        }
                        j++;

                        if (!String.IsNullOrEmpty(fullName))
                            GetNamesFromFullNameNoTitle(fullName, ref firstName, ref lastName);

                        if (CheckExistingStudent(srn, libraryId) > 0)
                        {
                            isValid = true;
                            existing++;
                        }
                        else
                        {
                            if (isValid && !String.IsNullOrEmpty(classDesc))
                                isValid = CheckExistingClass(classDesc, libraryId) > 0;

                            //if (isValid && booksRead < 0)
                            //    isValid = false;

                            //if (isValid && !String.IsNullOrEmpty(readLevel))
                            //    isValid = GetReadLevelId(readLevel) > 0;
                        }

                        var query = (from s in db.ImportDatas
                                     where s.ImportData_Id == row.ImportData_Id
                                     select s).SingleOrDefault();
                        if (query != null)
                        {
                            query.Valid = isValid;
                            if (isValid)
                                validRecords++;
                            else
                            {
                                invalidLines = true;
                                invalidRecords++;
                            }
                        }
                    }
                }
                try
                {
                    db.SaveChanges();
                }

                catch (Exception)
                {
                    errors = true;
                }
            }
            // CustomValidator1.IsValid = false;
            if (errors)
            {
                //CustomValidator1.IsValid = false;
                //CustomValidator1.ErrorMessage = "Failed to complete validation. Please try again.";
            }
            else if (invalidLines)
            {
                //CustomValidator1.IsValid = false;
                //CustomValidator1.ErrorMessage = "Valid records: " + validRecords.ToString() + "   Invalid records: " + invalidRecords.ToString();
            }

            return (!invalidLines && !errors);
        }

        private bool ValidateImportBooks(int noOfCols, int importFileId, int libraryId, int userId)
        {
            bool invalidLines = false;

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings == null)
                return false;
            else
            {
                int length = mappings.Count();
                int i = 0;
                string[] mapArray = new string[length];
                foreach (var mapping in mappings)
                {
                    if (mapping.MappedCol != "Ignore" && mapping.MappedCol != "Author" && mapping.MappedCol != "Illustrator")
                    {
                        if (mapArray.Contains(mapping.MappedCol))
                        {
                            //CustomValidator1.ErrorMessage = "Column " + mapping.MappedCol + " is duplicated.";
                            // CustomValidator1.IsValid = false;
                            return false;
                        }

                        else
                        {
                            mapArray[i] = mapping.MappedCol;
                            i++;
                        }
                    }
                }

                if (!mapArray.Contains("Isbn") || !mapArray.Contains("Title"))
                {
                    //CustomValidator1.ErrorMessage = "You must map the ISBN and Title fields.";
                    //CustomValidator1.IsValid = false;
                    return false;
                }

                int isbnCol = 0;

                var data = from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id;

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                            row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string isbn = "", title = "", annotation = "", readLevel = "";
                    bool isValid = false;
                    int j = 0;
                    int count = 1;
                    ArrayList authors = new ArrayList();
                    ArrayList illustrators = new ArrayList();
                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        switch (mapping.MappedCol)
                        {
                            case "Ignore":
                                break;

                            case "Isbn":
                                isbn = str;
                                isbnCol = count;
                                break;

                            case "Title":
                                title = str;
                                break;

                            case "Annotation":
                                annotation = str;
                                break;

                            case "Read Level":
                                readLevel = str;
                                break;

                            case "Author":
                                if (!string.IsNullOrEmpty(str))
                                    authors.Add(str);
                                break;

                            case "Illustrator":
                                if (!string.IsNullOrEmpty(str))
                                    illustrators.Add(str);
                                break;
                        }
                        j++;
                        count++;

                        isValid = !String.IsNullOrEmpty(isbn) && !String.IsNullOrEmpty(title) && ValidateIsbn(isbn);
                        string newIsbn = "";
                        if (isValid)
                        {
                            newIsbn = CheckCheckDigit(isbn);
                            isValid = !string.IsNullOrEmpty(newIsbn);
                        }

                        ImportData query = (from s in db.ImportDatas
                                            where s.ImportData_Id == row.ImportData_Id
                                            select s).SingleOrDefault();
                        if (query != null)
                        {
                            if (newIsbn != isbn && isbnCol > 0)
                            {
                                SetRowField(query, newIsbn, isbnCol);
                            }
                            query.Valid = isValid;
                        }
                    }
                }
                try
                {
                    db.SaveChanges();
                }

                catch (Exception)
                {
                    //CustomValidator1.IsValid = false;
                    //CustomValidator1.ErrorMessage = "Failed to complete validation. Please try again.";
                }
            }
            return !invalidLines;
        }

        private bool ValidateIsbn(string isbn)
        {
            string first = isbn.Substring(0, 1).ToString();

            string baddies = "/='.,\\\"";
            if (baddies.Contains(first))
            {
                return false;
            }
            else if (isbn.Contains(";"))
            {
                return false;
            }
            else
                return true;
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
            else if (isbn.Length == 13)
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

        private string GetPhoneNumber(string str)
        {
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "");
            if (str.Length > 17)
            {
                str = string.Empty;
            }
            return str;
        }

        private Dictionary<string, string> GetMappingDictionary(int index)
        {
            switch (index)
            {
                default: //class/teachers
                    return new Dictionary<string, string>
                    { 
                        { "Ignore", "Ignore" }, 
                        { "Class", "Class" }, 
                        { "Stage", "Stage" }, 
                        { "Title", "Title" }, 
                        { "First Name", "First Name" }, 
                        { "Last Name", "Last Name" }, 
                        { "Full Name", "Full Name" }, 
                        { "Email", "Email" } 
                    };

                case 1:    //Class student mapping
                    return new Dictionary<string, string> 
                    { 
                        { "Ignore", "Ignore" }, 
                        { "Class", "Class" }, 
                        { "Student Rec No", "Student Rec No" } 
                        //{ "First Name", "First Name" }, 
                        //{ "Last Name", "Last Name" }, 
                        //{ "Full Name", "Full Name" } 
                    };

                case 2: //Students
                    return new Dictionary<string, string> 
                    { 
                        { "Ignore", "Ignore" }, 
                        { "Student Rec No", "Student Rec No" }, 
                        { "Student First Name", "Student First Name" }, 
                        { "Student Last Name", "Student Last Name" }, 
                        { "Student Full Name", "Student Full Name" }, 
                        { "Class", "Class" }, 
                        { "Year", "Year" }, 
                        { "Read Level", "Read Level" }, 
                        { "Boooks Read", "Boooks Read" }, 
                        { "Parent Title", "Parent Title" }, 
                        { "Relationship", "Relationship" }, 
                        { "Parent First Name", "Parent First Name" }, 
                        { "Parent Last Name", "Parent Last Name" }, 
                        { "Parent Full Name", "Parent Full Name" }, 
                        { "Phone", "Phone" }
                    };

                case 3: //Books
                    return new Dictionary<string, string> 
                     { 
                         { "Ignore", "Ignore" }, 
                         { "Isbn", "Isbn" }, 
                         { "Title", "Title" }, 
                         { "Qty", "Qty" }, 
                         { "Author", "Author" },  
                         { "Illustrator", "Illustrator" },
                         { "Annotation", "Annotation" }, 
                         { "Read Level", "Read Level" }
                     };
            }
        }
      
        public ActionResult ShowMapping(int id)
        {
            var authors = db.ProdAuthors.Where(a => a.Product_Id == id).OrderBy(a => a.Role_Id);
            var mappingList = db.ImportMaps.Where(i => i.ImportFile_Id == id);

            if (mappingList != null)
            {
                return PartialView("_Mapping", mappingList.ToList());
            }
            return Content("");
        }

        public ActionResult ImportsList(int id, string sortOrder, int libraryId, int userId, int? page, int noOfCols = 20, int importType = 0, bool asc = true)
        {
            IQueryable<ImportList> reportList = from l in db.ImportDatas
                                                where l.ImportFile_Id == id
                                                select new ImportList()
                                          {
                                              ImportData_Id = l.ImportData_Id,
                                              Col1 = l.Col1,
                                              Col2 = l.Col2,
                                              Col3 = l.Col3,
                                              Col4 = l.Col4,
                                              Col5 = l.Col5,
                                              Col6 = l.Col6,
                                              Col7 = l.Col7,
                                              Col8 = l.Col8,
                                              Col9 = l.Col9,
                                              Col10 = l.Col10,
                                              Col11 = l.Col11,
                                              Col12 = l.Col12,
                                              Col13 = l.Col13,
                                              Col14 = l.Col14,
                                              Col15 = l.Col15,
                                              Col16 = l.Col16,
                                              Col17 = l.Col17,
                                              Col18 = l.Col18,
                                              Col19 = l.Col19,
                                              Col20 = l.Col20,
                                              Valid = (l.Valid == null) ? false : (bool)l.Valid
                                          };

            if (reportList == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            else if (reportList.Count() > 0)
            {
                switch (sortOrder)
                {
                    default:
                        reportList = asc ? reportList.OrderBy(x => x.ImportData_Id) : reportList.OrderByDescending(x => x.ImportData_Id);
                        break;
                    case "Col1":
                        reportList = asc ? reportList.OrderBy(x => x.Col1) : reportList.OrderByDescending(x => x.Col1);
                        break;
                    case "Col2":
                        reportList = asc ? reportList.OrderBy(x => x.Col2) : reportList.OrderByDescending(x => x.Col2);
                        break;
                    case "Col3":
                        reportList = asc ? reportList.OrderBy(x => x.Col3) : reportList.OrderByDescending(x => x.Col3);
                        break;
                    case "Valid":
                        reportList = asc ? reportList.OrderBy(x => x.Valid).ThenBy(x => x.ImportData_Id) : reportList.OrderByDescending(x => x.Valid).ThenBy(x => x.ImportData_Id);
                        break;
                }

                IEnumerable<MappingColumn> mappingList = from l in db.ImportMaps
                                                         where l.ImportFile_Id == id
                                                         select new MappingColumn()
                                                         {
                                                             ImportMap_Id = l.ImportMap_Id,
                                                             // ColIndex = mapList.FirstOrDefault(item => item.MappedCol == l.MappedCol).ColIndex,
                                                             ColIndex = 3,
                                                             MappedCol = l.MappedCol
                                                         };

                int pageSize = 15;
                int pageNumber = (page ?? 1);

                var mixmodel = new ModelMix
                {
                    LibraryId = libraryId,
                    UserId = userId,
                    SortOrder = sortOrder,
                    Ascending = asc,
                    Page = pageNumber,
                    NoOfCols = noOfCols,
                    ImportList = reportList.ToPagedList(pageNumber, pageSize),
                    MappingColumns = mappingList,
                    MappingList = GetMappingDictionary(importType)
                };
                return PartialView("_ImportList", mixmodel);
            }
            return PartialView("_Help");
        }

        public ActionResult ShowHelp()
        {

            return PartialView("_Help");
        }

        public ActionResult Delete(int id, string sortOrder, int? page)
        {
            ImportData record = db.ImportDatas.Find(id);
            db.ImportDatas.Remove(record);
            db.SaveChanges();
            return RedirectToAction("Index", new { sortOrder = sortOrder, page = page });
        }

        public JsonResult SetColumnHeader(int importMapId, string mappedCol)
        {
            if (importMapId > 0)
            {
                var mapToUpdate = db.ImportMaps.Find(importMapId);
                try
                {
                    if (String.IsNullOrEmpty(mappedCol))
                        mapToUpdate.MappedCol = "Ignore";
                    else
                        mapToUpdate.MappedCol = mappedCol;
                    db.SaveChanges();
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            } return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        private bool ImportStudentsOldKeepThis(int noOfCols, int importFileId, int libraryId, int userId)
        {
            int i = 0, added = 0, failed = 0, updated = 0, parentAdded = 0, parentUpdated = 0;
            int singleUserClass = 0;
            //if (Session["userType"].ToString() == "S")
            //{
            //    singleUserClass = GetSingleUserClass();
            // }

            var titles = from ct in db.ContactTitles
                         select ct;

            string[] salutations = new string[titles.Count()];
            i = 0;
            foreach (var t in titles)
            {
                salutations[i] = t.Salutation.ToUpper();
                i++;
            }

            var relations = from re in db.Relationships
                            select re;

            string[] relationships = new string[relations.Count()];
            i = 0;
            foreach (var x in relations)
            {
                relationships[i] = x.Relationship1.ToUpper();
                i++;
            }

            var mappings = from im in db.ImportMaps
                           where im.ImportFile.UserId == userId
                           select im;

            if (mappings != null)
            {
                var data = from id in db.ImportDatas
                           where id.ImportFile_Id == importFileId
                           select id;

                foreach (var row in data)
                {
                    List<string> columns = new List<string> { row.Col1, row.Col2, row.Col3, row.Col4, row.Col5, row.Col6, row.Col7, row.Col8, row.Col9, row.Col10,
                                        row.Col11, row.Col12, row.Col13, row.Col14, row.Col15, row.Col16, row.Col17, row.Col18, row.Col19, row.Col20};
                    string srn = "", firstName = "", lastName = "", classDesc = "", readLevel = "", fullName = "", phone1 = "", phone2 = "", year = "";
                    int studentId = 0, booksRead = 0, titleId = 0, relationId = 0, gradeId = 0;
                    bool parentFull1 = false, title1Set = false, relation1Set = false, firstName1Set = false, lastName1Set = false, parent2Set = false;
                    string[] parents = { "1", "1", "", "", "", "", "", "1", "1", "", "", "", "", "" }; //title_id, relation_id, firstName, lastName, fullname, phone, parentId then repeats
                    int j = 0;
                    foreach (var mapping in mappings)
                    {
                        string str = columns[j].Trim();
                        if (str == "&nbsp;")
                            str = string.Empty;

                        if (str != "&nbsp;" && !String.IsNullOrEmpty(str))
                        {
                            switch (mapping.MappedCol)
                            {
                                case "Ignore":
                                    break;

                                case "Student Rec No":
                                    srn = str;
                                    break;

                                case "Student First Name":
                                    firstName = str;
                                    break;

                                case "Student Last Name":
                                    lastName = str;
                                    break;

                                case "Student Full Name":
                                    fullName = str;
                                    break;

                                case "Class":
                                    classDesc = str;
                                    break;

                                case "Year":
                                    year = str;
                                    break;

                                case "Read Level":
                                    readLevel = str;
                                    break;

                                case "Books Read":
                                    //if (String.IsNullOrEmpty(str))
                                    //    booksRead = 0;
                                    // else
                                    booksRead = Convert.ToInt32(str);
                                    break;

                                case "Parent Title":
                                    GetTitleId(str.ToUpper(), ref titleId, salutations);
                                    if (title1Set)
                                        parents[7] = titleId.ToString();
                                    else
                                    {
                                        parents[0] = titleId.ToString();
                                        title1Set = true;
                                    }
                                    break;

                                case "Relationship":
                                    GetRelationhipId(str.ToUpper(), ref relationId, relationships);
                                    if (relation1Set)
                                        parents[8] = relationId.ToString();
                                    else
                                    {
                                        parents[1] = relationId.ToString();
                                        relation1Set = true;
                                    }
                                    break;

                                case "Parent First Name":
                                    if (firstName1Set)
                                        parents[9] = str;
                                    else
                                    {
                                        parents[2] = str;
                                        firstName1Set = true;
                                    }
                                    break;

                                case "Parent Last Name":
                                    if (lastName1Set)
                                    {
                                        parents[10] = str;
                                        parent2Set = true;
                                    }
                                    else
                                    {
                                        parents[3] = str;
                                        lastName1Set = true;
                                    }
                                    break;

                                case "Parent Full Name":

                                    if (parentFull1)
                                    {
                                        parent2Set = true;
                                        parents[11] = str;
                                    }
                                    else
                                    {
                                        parentFull1 = true;
                                        parents[4] = str;
                                    }
                                    break;

                                case "Phone":
                                    str = GetPhoneNumber(str);
                                    if (!String.IsNullOrEmpty(str))
                                    {
                                        if (String.IsNullOrEmpty(phone1))
                                            phone1 = str;
                                        else
                                            phone2 = str;

                                        if (parent2Set)
                                            parents[12] = str;
                                        else if (parentFull1 || lastName1Set)
                                            parents[5] = str;
                                    }
                                    break;
                            }
                        }
                        j++;
                    }
                    if (!String.IsNullOrEmpty(fullName))
                        GetNamesFromFullNameNoTitle(fullName, ref firstName, ref lastName);
                    if (!String.IsNullOrEmpty(parents[4]))
                        GetNamesFromFullName(parents[4], ref parents[0], ref parents[2], ref parents[3], salutations, false);
                    if (!String.IsNullOrEmpty(parents[11]))
                        GetNamesFromFullName(parents[11], ref parents[8], ref parents[9], ref parents[10], salutations, false);


                    studentId = CheckExistingStudent(srn, libraryId);
                    try
                    {
                        if (studentId == 0)
                        {
                            Student s = new Student();
                            s.SRN = srn;
                            gradeId = GetGrade(year);
                            if (gradeId > 0)
                                s.GradeId = gradeId;
                            s.FirstName = firstName;
                            s.LastName = lastName;
                            if (string.IsNullOrEmpty(parents[3]) && string.IsNullOrEmpty(parents[10]))
                            {
                                if (!string.IsNullOrEmpty(phone1))
                                    s.Phone = phone1;
                                else if (!string.IsNullOrEmpty(phone2))
                                    s.Phone = phone2;
                            }
                            if (!String.IsNullOrEmpty(readLevel))
                            {
                                int levelId = GetReadLevelId(readLevel);
                                if (levelId > 0)
                                    s.Levels_Id = levelId;
                            }

                            if (singleUserClass > 0)
                                s.Classes_Id = singleUserClass;

                            else if (!String.IsNullOrEmpty(classDesc))
                            {
                                int classId = CheckExistingClass(classDesc, libraryId);
                                //if (classId > 0)
                                s.Classes_Id = classId;
                            }

                            else
                                s.Classes_Id = null;

                            s.Library_id = libraryId;
                            s.Inactive = false;

                            db.Students.Add(s);
                            //db.SaveChanges();
                            added++;
                            studentId = s.Student_Id;
                        }

                        else
                        {
                            var query = (from s in db.Students
                                         where s.Student_Id == studentId
                                         select s).FirstOrDefault();

                            if (query != null)
                            {
                                if (!String.IsNullOrEmpty(readLevel))
                                {
                                    int levelId = GetReadLevelId(readLevel);
                                    if (levelId > 0)
                                        query.Levels_Id = levelId;
                                }

                                if (!String.IsNullOrEmpty(classDesc))
                                {
                                    int classId = CheckExistingClass(classDesc, libraryId);
                                    query.Classes_Id = classId;
                                }

                                if (string.IsNullOrEmpty(parents[3]) && string.IsNullOrEmpty(parents[10]))
                                {
                                    if (!string.IsNullOrEmpty(phone1))
                                        query.Phone = phone1;
                                    else if (!string.IsNullOrEmpty(phone2))
                                        query.Phone = phone2;
                                }
                                db.SaveChanges();
                                updated++;
                            }

                            if (!string.IsNullOrEmpty(parents[3]))
                            {
                                firstName = parents[2].ToLower();
                                lastName = parents[3].ToLower();
                                var contacts = (from sc in db.StudentContacts
                                                where sc.Parent.FirstName.ToLower() == firstName && sc.Parent.LastName.ToLower() == lastName
                                                    && sc.Student_Id == studentId
                                                select sc).FirstOrDefault();

                                if (contacts != null)
                                    parents[6] = contacts.Parent_Id.ToString();
                            }

                            if (!string.IsNullOrEmpty(parents[10]))
                            {
                                firstName = parents[9].ToLower();
                                lastName = parents[10].ToLower();
                                var contacts = (from sc in db.StudentContacts
                                                where sc.Parent.FirstName.ToLower() == firstName && sc.Parent.LastName.ToLower() == lastName
                                                        && sc.Student_Id == studentId
                                                select sc).FirstOrDefault();

                                if (contacts != null)
                                    parents[13] = contacts.Parent_Id.ToString();
                            }
                        }

                        int parentId = 0, inc = 0, existingId = 0;
                        bool done = false;
                        bool parent1Set = !string.IsNullOrEmpty(parents[3]);

                        while (!done)
                        {
                            if (parent1Set)
                                parent1Set = false;

                            else if (parent2Set)
                            {
                                inc = 7;
                                parent2Set = false;
                            }
                            else
                                done = true;

                            if (!done)
                            {
                                if (!String.IsNullOrEmpty(parents[6 + inc]))
                                {
                                    existingId = Convert.ToInt32(parents[6 + inc]);
                                    var query = (from p in db.Parents
                                                 where p.Parent_Id == existingId
                                                 select p).SingleOrDefault();

                                    if (query != null)
                                    {
                                        query.FirstName = parents[2 + inc];
                                        query.LastName = parents[3 + inc];
                                        query.Inactive = false;
                                        query.Phone = parents[5 + inc];
                                        if (!parent2Set && !String.IsNullOrEmpty(parents[12]) || String.IsNullOrEmpty(parents[3]) && !String.IsNullOrEmpty(parents[5]))
                                            query.Phone2 = parents[12 - inc];
                                        db.SaveChanges();
                                        parentUpdated++;
                                    }
                                }
                                else
                                {
                                    Parent p = new Parent();
                                    p.Title_Id = Convert.ToInt32(parents[0 + inc]);
                                    p.FirstName = parents[2 + inc];
                                    p.LastName = parents[3 + inc];
                                    p.Library_Id = libraryId;
                                    p.Inactive = false;
                                    p.Phone = parents[5 + inc];
                                    if (!parent2Set && !String.IsNullOrEmpty(parents[12]) || parent2Set && String.IsNullOrEmpty(parents[3]) && !String.IsNullOrEmpty(parents[5]))
                                        p.Phone2 = parents[12 - inc];

                                    db.Parents.Add(p);
                                    db.SaveChanges();
                                    parentId = p.Parent_Id;

                                    StudentContact sc = new StudentContact();
                                    sc.Student_Id = studentId;
                                    sc.Parent_Id = parentId;
                                    sc.Relation_Id = Convert.ToInt32(parents[1 + inc]); ;
                                    db.StudentContacts.Add(sc);
                                    db.SaveChanges();
                                    parentAdded++;
                                }
                            }
                        }

                        if (booksRead > 0 && studentId > 0)
                        {
                            BooksRead bs = new BooksRead();
                            bs.Student_Id = studentId;
                            bs.BooksRead1 = booksRead;
                            bs.ForYear = DateTime.Now.Year.ToString();
                            db.BooksReads.Add(bs);
                            db.SaveChanges();
                        }

                    }

                    catch (Exception ex)
                    {
                        var bf = ex;
                        failed++;
                    }
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var bf = ex;
                    failed++;
                }
            }

            //CustomValidator1.IsValid = false;
            // CustomValidator1.ErrorMessage = "Student records:  Added: " + added.ToString() + "  Updated: " + updated.ToString()
            //    + "<br />Parent records:  Added: " + parentAdded.ToString() + "  Updated: " + parentUpdated.ToString() + " All errors: " + failed.ToString();

            return (failed == 0 && (updated > 0 || added > 0));
        }
    }
}