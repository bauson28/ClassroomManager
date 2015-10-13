//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HomeReadingManager.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProdStock
    {
        public int Product_Id { get; set; }
        public int Library_Id { get; set; }
        public Nullable<int> Onhand { get; set; }
        public string Location { get; set; }
        public Nullable<bool> Shortage { get; set; }
        public Nullable<System.DateTime> EditDate { get; set; }
        public Nullable<int> StockCount { get; set; }
        public Nullable<int> LastCount { get; set; }
        public Nullable<int> LastStock { get; set; }
    
        public virtual Library Library { get; set; }
        public virtual Product Product { get; set; }
    }
}
