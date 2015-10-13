using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;

namespace HomeReadingManager.ViewModels
{
    public class Imports
    {
        public int ImportFile_Id { get; set; }
        public int ImportType { get; set; }
        public string Description { get; set; }
        [Display(Name = "File name")]
        public string FileName { get; set; }
        public bool HasRows { get; set; }
        public int NoOfCols { get; set; }
        public bool HasHeader { get; set; }
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public string Message { get; set; }

        public virtual ICollection<MappingColumn> MappingColumns { get; set; }
    }

    public class ImportList
    {
        public int ImportData_Id { get; set; }
        public string Col1 { get; set; }
        public string Col1Visible { get; set; }
        public string Col2 { get; set; }
        public string Col2Visible { get; set; }
        public string Col3 { get; set; }
        public string Col3Visible { get; set; }
        public string Col4 { get; set; }
        public string Col4Visible { get; set; }
        public string Col5{ get; set; }
        public string Col5Visible { get; set; }
        public string Col6 { get; set; }
        public string Col6Visible { get; set; }
        public string Col7 { get; set; }
        public string Col7Visible { get; set; }
        public string Col8 { get; set; }
        public string Col8Visible { get; set; }
        public string Col9 { get; set; }
        public string Col9Visible { get; set; }
        public string Col10 { get; set; }
        public string Col10Visible { get; set; }
        public string Col11 { get; set; }
        public string Col11Visible { get; set; }
        public string Col12 { get; set; }
        public string Col12Visible { get; set; }
        public string Col13 { get; set; }
        public string Col13Visible { get; set; }
        public string Col14 { get; set; }
        public string Col14Visible { get; set; }
        public string Col15 { get; set; }
        public string Col15Visible { get; set; }
        public string Col16 { get; set; }
        public string Col16Visible { get; set; }
        public string Col17 { get; set; }
        public string Col17Visible { get; set; }
        public string Col18 { get; set; }
        public string Col18Visible { get; set; }
        public string Col19 { get; set; }
        public string Col19Visible { get; set; }
        public string Col20 { get; set; }
        public string Col20Visible { get; set; }
        public bool Valid { get; set; }
    }

    public class MappingColumn
    {
        public int ImportMap_Id { get; set; }
        public int ColIndex { get; set; }
        public string MappedCol { get; set; }
    }

    [Serializable]
    public class Mapping
    {
        public int ColIndex { get; set; }
        public string MappedCol { get; set; }
    }

    public class ModelMix
    {
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public int NoOfCols { get; set; }

        public IEnumerable<ImportList> ImportList { get; set; }
        public IEnumerable<MappingColumn> MappingColumns { get; set; }
        //public List<SelectListItem> MappingList { get; set; }
        public virtual Dictionary<string, string> MappingList { get; set; }
    }  
}