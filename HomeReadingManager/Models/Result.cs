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
    
    public partial class Result
    {
        public int Id { get; set; }
        public int StudentReportId { get; set; }
        public int SubjectId { get; set; }
        public Nullable<int> MarksId { get; set; }
        public Nullable<int> EffortId { get; set; }
        public string Comments { get; set; }
    
        public virtual Mark Mark { get; set; }
        public virtual Mark Mark1 { get; set; }
        public virtual ReportStudent ReportStudent { get; set; }
        public virtual Subject Subject { get; set; }
    }
}
