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
    
    public partial class StudentContact
    {
        public int StudContact_Id { get; set; }
        public int Student_Id { get; set; }
        public int Parent_Id { get; set; }
        public Nullable<int> Relation_Id { get; set; }
    
        public virtual Parent Parent { get; set; }
        public virtual Relationship Relationship { get; set; }
        public virtual Student Student { get; set; }
    }
}
