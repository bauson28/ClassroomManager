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
    
    public partial class StSession
    {
        public StSession()
        {
            this.STCounts = new HashSet<STCount>();
        }
    
        public int StSessions_Id { get; set; }
        public System.DateTime BatchDate { get; set; }
        public string BatchName { get; set; }
        public Nullable<int> Library_Id { get; set; }
    
        public virtual Library Library { get; set; }
        public virtual ICollection<STCount> STCounts { get; set; }
    }
}
