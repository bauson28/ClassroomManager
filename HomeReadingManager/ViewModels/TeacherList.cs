using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages.Html;

namespace HomeReadingManager.ViewModels
{
    public class TeacherList
    {
        public int Teacher_Id { get; set; }
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        public bool Inactive { get; set; }
        public string Email { get; set; }
        public string Salutation { get; set; }
        public int Classes_Id { get; set; }
        [Display(Name = "Class")]
        public string ClassDesc { get; set; }
        public bool AllowEdit { get; set; }
        
    }

    public class ClassesVM
    {
        public bool ShowInactive { get; set; }
        public bool EditMode { get; set; }
        public bool IsAdministrator { get; set; }
        public string Message { get; set; }
        //public int TeacherId { get; set; }
        public virtual IQueryable<ClassDetail> ClassDetails { get; set; }
        public virtual IQueryable<TeacherRec> Teachers{ get; set; }
    }

    public class ClassDetail
    {
        public int ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        //[Display(Name = "Teacher")]
        public string Teacher { get; set; }
        //[Display(Name = "First name")]
        //public string FirstName { get; set; }
        //[Display(Name = "Last name")]
        //public string LastName { get; set; }
        public bool Inactive { get; set; }
        public Nullable<int> TeacherId { get; set; }
        public Nullable<int> TeacherId2 { get; set; }
        [Display(Name = "Teacher 2")]
        public string Teacher2 { get; set; }
        public string Stage { get; set; }

    }

    public class TeacherRec
    {
        public int Id { get; set; }
        public string Teacher { get; set; }
    }

    public class ClassEdit
    {
        public int Classes_Id { get; set; }
        [Display(Name = "Class")]
        [Required]
        [StringLength(10, ErrorMessage = "The class name cannot be longer than 10 characters.")]
        public string ClassDesc { get; set; }
        [Display(Name = "Inactive")]
        [Required]
        public bool Obsolete { get; set; }
        [Display(Name = "Teacher 1")]
        public Nullable<int> Teacher_Id { get; set; }
        [Display(Name = "Teacher 2")]
        public Nullable<int> Teacher2Id { get; set; }
        [Required]
        public string Stage { get; set; }

        public virtual IQueryable<TeacherRec> Teachers { get; set; }
        public virtual Dictionary<string, string> Stages { get; set; }
    }

    public class TeacherCreate
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [Remote("IsEmailUnique", "Teachers", HttpMethod = "POST", ErrorMessage = "A user with this email is already in the database.")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int LibraryId { get; set; }
       
        //[Required]
        //[Display(Name = "Teacher")]
        public int TeacherId { get; set; }
        [Required]
        [Display(Name = "First name")]
        [StringLength(20, ErrorMessage = "First name cannot be longer than 20 characters.")]
        public string FirstName { get; set; }
        [Required]
        [Display(Name = "Last name")]
        [StringLength(25, ErrorMessage = "Last name cannot be longer than 25 characters.")]
        public string LastName { get; set; }
        public string FullName { get; set; }
        [Required]
        public bool Inactive { get; set; }
        [Display(Name = "Salutation")]
        public Nullable<int> Title_Id { get; set; }
        public int Role { get; set; }
        [Display(Name = "Site administrator")]
        public bool Administrator { get; set; }
        public bool IsAdministrator { get; set; }
        public string Message { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        
       
        public virtual IQueryable<Salutation> Salutations { get; set; }
        public virtual Dictionary<string, string> Roles { get; set; }
    }
    public class Salutation
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class TeachersVM
    {
        public bool ShowInactive { get; set; }
        public bool Ascending { get; set; }
        public string SortOrder { get; set; }
        public int Page { get; set; }
        public bool IsAdministrator { get; set; }
        public int LibraryId { get; set; }
        public string Message { get; set; }

        public virtual IPagedList<TeacherList> Teachers { get; set; }
    }

    //public class TeacherDetail
    //{
    //    public int TeacherId { get; set; }
    //    [Display(Name = "Class")]
    //    public string ClassName { get; set; }
    //    public string Email { get; set; }
    //    [Display(Name = "First name")]
    //    public string FirstName { get; set; }
    //    [Display(Name = "Last name")]
    //    public string LastName { get; set; }
    //    public bool Inactive { get; set; }
        

    //}
}