using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class Stocktake
    {
        public bool Flag{ get; set; }
        //[Display(Name = "Stocktake date")]
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        //public DateTime? StocktakeDate { get; set; }
        //[Display(Name = "Last stocktake date")]
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        //public DateTime? LastSTDate { get; set; }
        public string LastStocktake{ get; set; }
        public string ShowDate { get; set; }
        public string TitleSearch { get; set; }
        public string SessionName { get; set; }
        public int Session_Id { get; set; }
        public string NewSession { get; set; }
        public int ReportType { get; set; }
        [Display(Name = "Report")]
        public string ReportName { get; set; }
        public bool HasSessions { get; set; }
        public bool HasPrevious { get; set; }
        public string UserAction{ get; set; }
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public int Page { get; set; }
        public string Message { get; set; }
}
    public class CountModel
    {
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public int Page { get; set; }
        public virtual IPagedList<CountList> CountLists { get; set; }

    }

    public class CountList
    {
        public int STCount_Id { get; set; }
        public DateTime CountDate { get; set; }
        public int Product_Id { get; set; }
        public string Title { get; set; }
        [Display(Name = "Barcode")]
        public string Isbn { get; set; }
        [Display(Name = "Level")]
        public string ReadLevel { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
    }

    public class CountItem
    {
        public int Product_Id { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }

        public CountItem(int productId, string title, string isbn, int count, int total)
        {
            Product_Id = productId;
            Title = title;
            Isbn = isbn;
            Count = count;
            Total = total;
        }
    }

    public class StockReportData
    {
        public int ReportType { get; set; }
        [Display(Name = "Report")]
        public string ReportName { get; set; }
        public int LibraryId { get; set; }
    }

    public class StockReportList
    {
        public string Title { get; set; }
        [Display(Name = "Barcode")]
        public string Isbn { get; set; }
        public int Stock { get; set; }
        public int Count { get; set; }
        [Display(Name = "Level")]
        public string ReadLevel { get; set; }
        public int Diff { get; set; }
       
    }
}