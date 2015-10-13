using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class ProductSearch
    {
        public int Product_Id { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string MainAuthor { get; set; }
        public string ReadLevel { get; set; }
        public int Image_Id { get; set; }
        public byte[] Jacket { get; set; }
    }
}