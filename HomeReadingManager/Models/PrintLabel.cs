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
    
    public partial class PrintLabel
    {
        public int Print_Id { get; set; }
        public int Product_Id { get; set; }
        public int Qty { get; set; }
        public Nullable<int> Library_Id { get; set; }
        public Nullable<System.DateTime> Entered { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
    
        public virtual Library Library { get; set; }
        public virtual Product Product { get; set; }
        public virtual Teacher Teacher { get; set; }
    }
}
