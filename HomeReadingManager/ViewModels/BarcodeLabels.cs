using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class BarcodeLabels
    {
        public int UserSettings_Id { get; set; }
        [Display(Name = "Labels per column")]
        [Range(1, 12, ErrorMessage = "Labels/column must be between 1 and 12")]
        public int LabelsPerCol { get; set; }
        [Display(Name = "Columns per page")]
        [Range(1, 8, ErrorMessage = "No of columns must be between 1 and 8")]
        public int ColsPerPage { get; set; }
        [Display(Name = "Margin top")]
        [Range(0, 12, ErrorMessage = "Margins must be between 0 and 12mm")]
        public int LabelsTop { get; set; }
        [Display(Name = "Margin bottom")]
        [Range(0, 12, ErrorMessage = "Margins must be between 0 and 12mm")]
        public int LabelsBottom { get; set; }
        [Display(Name = "Margin left")]
        [Range(0, 12, ErrorMessage = "Margins must be between 0 and 12mm")]
        public int LabelsLeft { get; set; }
        [Display(Name = "Margin right")]
        [Range(0, 12, ErrorMessage = "Margins must be between 0 and 12mm")]
        public int LabelsRight { get; set; }
        public string TitleSearch { get; set; }
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
    }

    public class LabelList
    {
        public int Page { get; set; }
        public bool Ascending { get; set; }
        public string SortOrder { get; set; }
       
        public virtual IPagedList<Label> Labels { get; set; }
    }

    public class Label
    {
        public int Print_Id { get; set; }
        public int Product_Id { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string ReadLevel { get; set; }
        public DateTime Entered { get; set; }
     }

    public class LabelTitleList
    {
        public int Page { get; set; }
        public string SearchString { get; set; }

        public virtual IPagedList<ProductSearch> Titles { get; set; }
    }
}