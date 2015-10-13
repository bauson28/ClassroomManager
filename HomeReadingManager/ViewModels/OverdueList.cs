using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class OverdueModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public int Days { get; set; }
        public virtual IPagedList<OverdueItem> OverdueList { get; set; }
    }
    public class OverdueItem
    {
        public string Title { get; set; }
        public string FullName { get; set; }
        public int Student_Id { get; set; }
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Display(Name = "Borrowed")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime BorrowDate { get; set; }
        public int Days { get; set; }
        public int ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
      
    }

    public class Overdues
    {
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string BorrowDate { get; set; }
        public int Days { get; set; }
           
        public Overdues(string title, string isbn, string borrowDate, int days)
        {
            Title = title;
            Isbn = isbn;
            BorrowDate = borrowDate;
            Days = days;
        }
     }

    public class BorrowModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public DateTime ActivityDate { get; set; }
        public int ReportType { get; set; }
        public virtual IPagedList<BorrowItem> BorrowList { get; set; }
    }

    public class BorrowItem
    {
        public string Title { get; set; }
        public int Student_Id { get; set; }
        public string FullName { get; set; }
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Display(Name = "Borrowed")]
        public int ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
     }

    public class SchoolLoansModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public virtual IPagedList<SchoolLoansItem> SchoolLoansList { get; set; }
    }

    public class SchoolLoansItem
    {
        public string Teacher { get; set; }
        public int ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public int BooksRead { get; set; }
        [Display(Name = "Max level")]
        public int MaxLevel { get; set; }
        [Display(Name = "Min level")]
        public int MinLevel { get; set; }
        [Display(Name = "Avg level")]
        public float AvgLevel { get; set; }
    }

    public class ClassLoansModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public string ClassName { get; set; }
        public int ClassId { get; set; }
        public virtual IPagedList<ClassLoansItem> ClassLoansList { get; set; }
    }

    public class ClassLoansItem
    {
        public int Student_Id { get; set; }
        public string FullName { get; set; }
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Display(Name = "Books read")]
        public int BooksRead { get; set; }
        [Display(Name = "Read level")]
        public string ReadLevel { get; set; }
    }

    public class StudentLoansModel
    {
        public int LibraryId { get; set; }
        public string School { get; set; }
        public int Page { get; set; }
        public string Student { get; set; }
        public int StudentId { get; set; }
        public string NoLoansMessage { get; set; }
        public virtual IPagedList<StudentLoansItem> StudentLoansList { get; set; }
    }

    public class StudentLoansItem
    {
        public string Title { get; set; }
        [Display(Name = "Read level")]
        public string ReadLevel { get; set; }
        [Display(Name = "Borrowed")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime BorrowDate { get; set; }
        [Display(Name = "Days out")]
        public string DaysOut { get; set; }
    }

    public class MyModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ListOfStringsModel
    {
        public List<string> Numbers { get; set; }
    }
}