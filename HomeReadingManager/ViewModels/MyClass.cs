using HomeReadingManager.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HomeReadingManager.ViewModels
{
    public class MyClass
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int StudentId { get; set; }
        public string Student { get; set; }
        public int GradeId { get; set; }
        public int SchoolReportId { get; set; }
        public string Activity { get; set; }
       // public int Act { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public string ReportName { get; set; }
        public string Message { get; set; }
        public int LibraryId { get; set; }
        public bool EditLevels { get; set; }
        public bool EditClasses { get; set; }
        public string EditHeader { get; set; }
       // public bool Refresh { get; set; }
        public virtual IQueryable<StudentVM> StudentVMs { get; set; }
       // public virtual IQueryable<StudentReport> StudentReports { get; set; }
        public virtual IQueryable<ReadLevel> Levels { get; set; }
        public virtual IQueryable<ClassItem> Classes { get; set; }
        public virtual IQueryable<ReadyReport> ReadyReports { get; set; }
    }

    public class StudentVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //public byte[] Portrait { get; set; }
        //[Required]
        //[StringLength(20, ErrorMessage = "First name cannot be longer than 20 characters.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        //[Required]
        //[StringLength(25, ErrorMessage = "Last name cannot be longer than 25 characters.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; }
       // [Required]
        //[StringLength(12, ErrorMessage = "Student record no cannot be longer than 12 characters.")]
        [Display(Name = "Student record no")]
        public string SRN { get; set; }
        [Display(Name = "Class")]
        public Nullable<int> ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public bool Inactive { get; set; }
        public Nullable<int> LevelId { get; set; }
        [Display(Name = "Reading level")]
        public string ReadLevel { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Grade")]
        public int GradeId { get; set; }
        public string Grade { get; set; }
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

    public class StudentEdit
    {
        public int Id { get; set; }
        [Required]
        [StringLength(20, ErrorMessage = "First name cannot be longer than 20 characters.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Required]
        [StringLength(25, ErrorMessage = "Last name cannot be longer than 25 characters.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Required]
        [StringLength(12, ErrorMessage = "Student record no cannot be longer than 12 characters.")]
        [Display(Name = "Student record no")]
        public string SRN { get; set; }
        [Display(Name = "Class")]
        public Nullable<int> Classes_Id { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        public bool Inactive { get; set; }
        [Display(Name = "Reading level")]
        public Nullable<int> Levels_Id { get; set; }
        [Display(Name = "Reading level")]
        public string ReadLevel { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Grade")]
        public int GradeId { get; set; }
        public string Grade { get; set; }
        [Display(Name = "Student")]
        public string FullName
        {
            get
            {
                return FirstName.Trim() + " " + LastName;
            }
        }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public virtual IQueryable<ClassItem> ClassList { get; set; }
        public virtual IQueryable<GradeItem> GradesList { get; set; }
        public virtual IQueryable<ReadLevel> LevelsList { get; set; }
    }

    public class StudentLoanGroup
    {
        public int Year { get; set; }
        public int BookCount { get; set; }
    }

    public class ReadLevel
    {
        public int Id { get; set; }
        public string Level { get; set; }
    }

    public class ClassItem
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
    }

    public class ClassSelection
    {
        public int ClassId { get; set; }
        [Display(Name = "Class name")]
        public string ClassName { get; set; }
        public int LibraryId { get; set; }
        public string Message { get; set; }
        public string Search { get; set; }
        public virtual IQueryable<CurrentClass> ClassList { get; set; }
    }

    public class CurrentClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //public string Teacher { get; set; }
        //public int TeacherId { get; set; }
        public string Stage { get; set; }
    }

    public class ClassRow
    {
        public int Col1Id { get; set; }
        public int Col2Id { get; set; }
        public int Col3Id { get; set; }
        public int Col4Id { get; set; }
        public string Col1Name { get; set; }
        public string Col2Name { get; set; }
        public string Col3Name { get; set; }
        public string Col4Name { get; set; }
    }
        
    public class StudentReport
    {
        public int SchoolReportId { get; set; }
        public int GradeReportId { get; set; }
        public int StudentReportId { get; set; }
        public int StudentId { get; set; }
        public string Student { get; set; }
        //public string FirstName { get; set; }
        //public string LastName { get; set; }
        //public string Gender { get; set; }
        //public byte[] Portrait { get; set; }
       // public int GradeId { get; set; }
        //public string Grade { get; set; }
        public string ReportName { get; set; }
        public int LibraryId { get; set; }

        public string Comments { get; set; }
        public string CommentHeader { get; set; }
        [Required(ErrorMessage = "A teacher name is required.")]
        [StringLength(60, ErrorMessage = "Teacher name cannot be longer than 60 characters.")]
        public string Teacher { get; set; }
        [Display(Name = "Teacher")]
        [StringLength(60, ErrorMessage = "Teacher name cannot be longer than 60 characters.")]
        public string Teacher2 { get; set; }
        [Range(1, 60, ErrorMessage = "Days absent must be between 1 and 60")]
        [Display(Name = "Full:")]
        public int AbsentFull { get; set; }
        [Range(1, 60, ErrorMessage = "Days partially absent must be between 1 and 60")]
        [Display(Name = "Partial:")]
        public int AbsentPart { get; set; }
        [Display(Name = "Days absent as at:")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime AbsentDate { get; set; }
        public string ApprovedBy { get; set; }
        public int Status { get; set; }

      //  public virtual IQueryable<ReadyReport> ReadyReports { get; set; }
        //public virtual IQueryable<ResultVM> ResultVMs { get; set; }
        public virtual IEnumerable<ResultVM> Klas { get; set; }
        public virtual IEnumerable<ResultVM> Substrands { get; set; }
        public virtual IEnumerable<ResultVM> Substrands2 { get; set; }
        public virtual IEnumerable<ResultVM> Indicators { get; set; }
        public virtual IQueryable<RepMark> MarksList { get; set; }
        //public virtual IQueryable<RepMark> MarksList2 { get; set; }
        //public virtual IQueryable<RepMark> EffortList { get; set; }
    }

    public class ReadyReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; }
    }

    public class ResultVM
    {
        public int SubjectId { get; set; }
        public string Subject { get; set; }
        public int ParentId { get; set; }
        public int ReportType { get; set; }
        public bool IsTopic { get; set; }
        public int AssessListId { get; set; }
        public int EffortListId { get; set; }
        public int ResultsId { get; set; }
        public int MarksId { get; set; }
        public int EffortId { get; set; }
        public int ColOrder { get; set; }
        public int ParentType { get; set; }
        public bool KlaComments { get; set; }
        public string Comments { get; set; }
    }

    public class RepMark
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AssessmentId { get; set; }
    }

    public class NoData
    {
        public string Student { get; set; }
        public string Heading { get; set; }
        public string Message { get; set; }
    }
    public class StudentComment
    {
        public int StudentReportId { get; set; }
        public int Student_Id { get; set; }
        public int SchoolReportId { get; set; }
        public string Comments { get; set; }
        public string SuperComments { get; set; }
        public string CommentHeader { get; set; }
        public string SuperHeader { get; set; }
        [Required(ErrorMessage = "A teacher name is required.")]
        [StringLength(60, ErrorMessage = "Teacher name cannot be longer than 60 characters.")]
        public string Teacher { get; set; }
        [Display(Name = "Teacher")]
        [StringLength(60, ErrorMessage = "Teacher name cannot be longer than 60 characters.")]
        public string Teacher2 { get; set; }
        [Range(1, 60, ErrorMessage = "Days absent must be between 1 and 60")]
        [Display(Name = "Full:")]
        public int AbsentFull { get; set; }
         [Range(1, 60, ErrorMessage = "Days partially absent must be between 1 and 60")]
        [Display(Name = "Partial:")]
        public int AbsentPart { get; set; }
        [Display(Name = "Days absent as at:")]
        public DateTime AbsentDate { get; set; }
        public bool UseSuperCom { get; set; }
    }

    public class SummaryList
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public virtual IQueryable<StudentSummary> Summaries { get; set; }
    }

    public class StudentSummary
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string Grade { get; set; }
        public int GradeId { get; set; }
        public int Id { get; set; }
        [Display(Name = "Read Level")]
        public string ReadLevel { get; set; }
        [Display(Name = "Total Read")]
        public int BooksRead { get; set; }
        [Display(Name = "On loan")]
        public int OnLoan { get; set; }
       [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")] //, ApplyFormatInEditMode = true
        public DateTime? Overdue { get; set; }
        [Display(Name = "Report")]
        public int ReportStatus { get; set; }
        [Display(Name = "All Ticked")]
        public int AllTicked { get; set; }
        [Display(Name = "Full Name")]
        public string FullName
        {
            get
            {
                return FirstName.Trim() + " " + LastName;
            }
        }
    }

    public class StudentLoans
    {
        public int StudentId { get; set; }
        public string Student { get; set; }
        public string ReadLevel { get; set; }
        public string ClassName { get; set; }
        public string BooksRead { get; set; }
        public string OnLoan { get; set; }
        public string Overdue { get; set; }
        //public bool ShowCurrent { get; set; }
        public virtual IQueryable<OnLoan> Loans { get; set; }
    }

    public class OnLoan
    {
        public int LoanId { get; set; }
       
        public string Title { get; set; }
        public string Isbn { get; set; }
        public int ProductId { get; set; }
        //[Display(Name = "Author")]
        //public string MainAuthor { get; set; }
        [Display(Name = "Level")]
        public string ReadLevel { get; set; }
        public int ImageId { get; set; }
        public byte[] Jacket { get; set; }
        public System.DateTime BorrowDate { get; set; }
        public Nullable<System.DateTime> ReturnDate { get; set; }
        public bool TodaysLoan { get; set; }
        public string DisplayDate { get; set; }
      //  public int Days { get; set; }
    }

    public class BookSearch
    {
        public int StudentId { get; set; }
        public int Page { get; set; }
        public string Search { get; set; }
        public virtual IPagedList<ProductSearch> Titles { get; set; }
    }

    public class ResultsPrint
    {
        [Key]
        public int SubjectId { get; set; }
        public string Subject { get; set; }
        public int ParentId { get; set; }
        public int ReportType { get; set; }
        public bool IsTopic { get; set; }
        public int AssessListId { get; set; }
        public int EffortListId { get; set; }
        public int ResultsId { get; set; }
        public int MarksId { get; set; }
        public int EffortId { get; set; }
        public int ColOrder { get; set; }
        public int ParentType { get; set; }
        public bool PageBreak { get; set; }
        public bool KlaComments { get; set; }
        public string Comments { get; set; }
    }

    public class StudentDet
    {
        public int Id { get; set; }
        
        [Display(Name = "Student record no")]
        public string SRN { get; set; }
        public string Name { get; set; }
        //public byte[] Portrait { get; set; }
        public string Student { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
       // public bool Inactive { get; set; }
        //public int LevelId { get; set; }
        [Display(Name = "Reading level")]
        public string ReadLevel { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Grade")]
        public string Grade { get; set; }
        public int LibraryId { get; set; }
        public int ClassId { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        [Display(Name = "Student Contacts")]
        public virtual IQueryable<Contact> Contacts { get; set; }
        public virtual IQueryable<Loan> Loans { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Relationship { get; set; }
        public int RelationId { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
       
    }

    public class StudentCreate
    {
        [Required]
        [StringLength(20, ErrorMessage = "First name cannot be longer than 20 characters.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Required]
        [StringLength(25, ErrorMessage = "Last name cannot be longer than 25 characters.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Display(Name = "Student record no")]
        [Required]
        [StringLength(12, ErrorMessage = "Student record no cannot be longer than 12 characters.")]
        //[Remote("IsSrnUnique", "Students", HttpMethod = "POST", ErrorMessage = "A student with this record number is already in the database.")]
        public string SRN { get; set; }
        public int ClassId { get; set; }
        public int LibraryId { get; set; }
        public string ClassName { get; set; }
        public bool Inactive { get; set; }
        [Display(Name = "Reading level")]
        public Nullable<int> LevelId { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Grade")]
        public int GradeId { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
    
        public virtual IQueryable<ReadLevel> LevelsList { get; set; }
        public virtual IQueryable<GradeItem> GradesList { get; set; }
    }

    public class GradeItem
    {
        public int Id { get; set; }
        public string Grade { get; set; }
    }
}