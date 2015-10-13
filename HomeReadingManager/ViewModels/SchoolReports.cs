using HomeReadingManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class FrontPage
    {
        public int Id { get; set; }
        public int SemesterId { get; set; }
        [Display(Name = "Use Principal's comments")]
        public bool UseSuperCom { get; set; }
        public short Status { get; set; }
        public string ReportName { get; set; }
        public string Subtitle { get; set; }
        public int GradeReportId { get; set; }
        public int SchoolReportId { get; set; }
        public int LibraryId { get; set; }
        public string Message { get; set; }
        [Display(Name = "Allow KLA comments")]
        public bool KlaComments { get; set; }
        [Display(Name = "Use crest as watermark")]
        public bool Watermark { get; set; }
        [Display(Name = "Use department logo on front page")]
        public bool DeptLogo { get; set; }
        [Required]
        [Display(Name = "Name of principal or signer")]
        [StringLength(60, ErrorMessage = "This field cannot be longer than 60 characters.")]
        public string Principal { get; set; }
        [Required]
        [Display(Name = "Their position e.g. Relieving Principal")]
        [StringLength(30, ErrorMessage = "This field cannot be longer than 30 characters.")]
        public string Position { get; set; }
        [Required]
        [Display(Name = "Introductory paragraph for all reports")]
        public string Introduction { get; set; }
        [StringLength(50, ErrorMessage = "This header line cannot be longer than 50 characters.")]
        [Display(Name = "Header line above student comments")]
        public string CommentHeader { get; set; }
        [StringLength(50, ErrorMessage = "This header line cannot be longer than 50 characters.")]
        [Display(Name = "Header line above principal's comments (if used)")]
        public string SuperHeader { get; set; }
        public int ImageId { get; set; }
        public byte[] Crest { get; set; }

        public virtual IEnumerable<RepGrade> RepGrades { get; set; }
        public virtual IEnumerable<RepSemester> RepSemesters { get; set; }
    }

    public class RepGrade
    {
        public int Id { get; set; }
        public int GradeId { get; set; }
        public string Grade { get; set; }
        public bool Ready { get; set; }
     }

    public class RepSemester
    {
        public int Id { get; set; }
        public string Semester{ get; set; }
     }

    public class SchoolReports
    {
        public int GradeId { get; set; }
        public int SchoolReportId { get; set; }
        public string Grade { get; set; }
        public string GradeName { get; set; }
        [Display(Name = "Ready for marking")]
        public bool Ready { get; set; }
        public int GradeReportId { get; set; }
        public string AnchorId { get; set; }
        public int LibraryId { get; set; }

        [Display(Name = "Key Learning Area")]
        public virtual IEnumerable<Kla> Klas { get; set; }
        public virtual IEnumerable<SubKla> Substrands { get; set; }
        public virtual IEnumerable<AssessmentArea> AssessmentAreas { get; set; }
    }

    public class Kla
    {
        public int Id { get; set; }
        [Required]
        [StringLength(60, ErrorMessage = "Key Learning Areas cannot be longer than 60 characters.")]
        [Display(Name = "Key learning area")]
        public string Name { get; set; }
        public int ColOrder { get; set; }
        public int AssessmentId { get; set; }
        public int EffortId { get; set; }
        public string AnchorId { get; set; }
        [Required]
        [Display(Name = "Report style")]
        public int ReportType { get; set; }
        public int GradeId { get; set; }
        public string Grade { get; set; }
        public int GradeReportId { get; set; }
        public int LibraryId { get; set; }
        [Display(Name = "Page break")]
        public bool PageBreak { get; set; }
        [Display(Name = "Allow comments")]
        public bool KlaComments { get; set; }
        public bool ShowComments { get; set; }
        public virtual IEnumerable<Mark> Marks { get; set; }
        public virtual IEnumerable<Mark> Efforts { get; set; }
    }
   
    public class SubKla
    {
        public int Id { get; set; }
        [Required]
        [StringLength(60, ErrorMessage = "The KLA substrand name cannot be longer than 60 characters.")]
        [Display(Name = "KLA substrand")]
        public string Name { get; set; }
        public string Parent { get; set; }
        public int ParentId { get; set; }
        public int ColOrder { get; set; }
        public int GradeId { get; set; }
        public string Grade { get; set; }
        public bool Inactive { get; set; }
        public string AnchorId { get; set; }
        public int GradeReportId { get; set; }
        public int LibraryId { get; set; }
        public int ReportType { get; set; }
     }

    public class SubKlaCreate
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "KLA substrand")]
        public string Name { get; set; }
        public string Parent { get; set; }
        public int ParentId { get; set; }
        public int ColOrder { get; set; }
        public int GradeId { get; set; }
        public string Grade { get; set; }
        public bool Inactive { get; set; }
        public string AnchorId { get; set; }
        public int GradeReportId { get; set; }
        public int LibraryId { get; set; }
        public int ReportType { get; set; }
    }

    public class AssessmentArea
    {
        public int Id { get; set; }
        [Required]
        [StringLength(240, ErrorMessage = "The KLA indicator name cannot be longer than 240 characters.")]
        [Display(Name = "KLA Indicator")]
        public string Name { get; set; }
        public string Parent { get; set; }
        public int ParentId { get; set; }
        public int ColOrder { get; set; }
        public int GradeId { get; set; }
        public string Grade { get; set; }
        public bool Inactive { get; set; }
        public string AnchorId { get; set; }
        public int GradeReportId { get; set; }
        public int LibraryId { get; set; }
    }

    //public class Mark
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public int ColOrder { get; set; }
    //}

    public class SubjectDelete
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public string Message { get; set; }
       public string Message2 { get; set; }
       public string Type { get; set; }
       public int ReturnId { get; set; }
       public int GradeReportId { get; set; }
       
   }

    public class AssessmentKey
    {
        public string Mark { get; set; }
        public string Description { get; set; }
     }
    
    public class NextSemester
    {
        public int LibraryId { get; set; }
        public int Options { get; set; }
        public string Semester1 { get; set; }
        public string Semester2 { get; set; }
        public int Id1 { get; set; }
        public int Id2 { get; set; }
        [Display(Name = "Copy KLAs, substrands and indicators from...")]
        public int Selection { get; set; }
    }
}