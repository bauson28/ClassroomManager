using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class ReportData
    {
        public int ReportType { get; set; }
        [Display(Name = "Report")]
        public string ReportName { get; set; }
        public int Student_Id { get; set; }
        public string Student { get; set; }
        [Display(Name = "Class")]
        public int ClassId { get; set; }
        [Display(Name = "Class")]
        public string ClassName { get; set; }
        [Display(Name = "Days overdue")]
        public int Days { get; set; }
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime ActivityDate { get; set; }
        public int Year { get; set; }
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public int SearchPage { get; set; }
        public string SearchString { get; set; }
        public int SelectedYear { get; set; }

        public virtual IEnumerable<ClassItem> Classes { get; set; }
        public virtual Dictionary<string, string> DaysList { get; set; }
        public virtual Dictionary<string, string> Years { get; set; }
    }

    public class LettersHome
    {
        [Display(Name = "Letter text (between salutation and book list):")]
        public string StLetter1 { get; set; }
        [Display(Name = "Letter text (between book list and sign off): ")]
        public string StLetter2 { get; set; }
        [StringLength(50, ErrorMessage = "Signer cannot be longer than 50 characters.")]
        [Display(Name = "Signed by")]
        public string StLetterName { get; set; }
        [Display(Name = "Position")]
        [StringLength(50, ErrorMessage = "Position cannot be longer than 50 characters.")]
        public string StLetterPosition { get; set; }
        [Display(Name = "Page size")]
        public string StLetterSize { get; set; }
        public bool A4Page { get; set; }
        public int Crest_Id { get; set; }
        [Display(Name = "School crest")]
        public byte[] Crest { get; set; }
        public int LibraryId { get; set; }
        public string School { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public int Days { get; set; }
    }
}