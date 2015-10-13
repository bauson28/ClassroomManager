using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class HomeReading
    {
        public int StudentId { get; set; }
        public string Student { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        [Display(Name = "Class")]
        public Nullable<int> ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public string FilterName { get; set; }
        public string Search { get; set; }
        public int Page { get; set; }
        public int LibraryId { get; set; }
        public virtual IPagedList<StudentHR> StudentHRs { get; set; }
        //public virtual IQueryable<ClassItem> Classes { get; set; }
    }

    public class StudentHR
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Display(Name = "Class")]
        public Nullable<int> ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public int LevelId { get; set; }
        [Display(Name = "Reading level")]
        public string ReadLevel { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Full Name")]
        public string FullName
        {
            get
            {
                return FirstName.Trim() + " " + LastName;
            }
        }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
    }

    public class StudentSearchModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public int Page { get; set; }
        public string SearchString { get; set; }
        public virtual IEnumerable<StudentSearchItem> StudentSearchList { get; set; }
    }

    public class StudentSearchItem
    {
        public int StudentId { get; set; }
        public string Student { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<int> ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }

    }

    public class StudentSearch
    {
        public int StudentId { get; set; }
        public string Student { get; set; }
        [Display(Name = "Class")]
        public Nullable<int> ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public string Search { get; set; }
        public int LibraryId { get; set; }
        public string School { get; set; }
        public int Page { get; set; }
      
        public virtual IEnumerable<ClassItem> Classes { get; set; }
    }
}