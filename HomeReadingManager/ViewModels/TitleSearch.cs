using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class TitleSearch
    {
        public int TitleId { get; set; }
        [Display(Name = "Add to label printing")]
        public bool Labels { get; set; }
        [Display(Name = "Set stock to 1")]
        public bool SetNewStock { get; set; }
        [Display(Name = "Increment stock by 1 ")]
        public bool IncrementStock { get; set; }
        [Display(Name = "Set reading level to")]
        public int SetLevels_Id { get; set; }
        [Display(Name = "Update title details")]
        public bool DoUpdate { get; set; }
        public int LibraryId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }

        public virtual IQueryable<ProductTitle> ProductTitles { get; set; }
        public virtual IQueryable<LevelItem> Levels { get; set; }
    }

    public class ProductTitle
    {
        public int TitleId { get; set; }
        public string Title { get; set; }
        public byte[] Jacket { get; set; }
   }
}