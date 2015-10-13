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
    
    public partial class ReportSchool
    {
        public ReportSchool()
        {
            this.GradeReports = new HashSet<GradeReport>();
            this.ReportStudents = new HashSet<ReportStudent>();
        }
    
        public int Id { get; set; }
        public int Library_Id { get; set; }
        public int SemesterId { get; set; }
        public bool UseSuperCom { get; set; }
        public short Status { get; set; }
        public string Principal { get; set; }
        public string Position { get; set; }
        public string Introduction { get; set; }
        public string CommentHeader { get; set; }
        public string SuperHeader { get; set; }
        public bool Watermark { get; set; }
        public bool DeptLogo { get; set; }
        public bool KlaComments { get; set; }
    
        public virtual ICollection<GradeReport> GradeReports { get; set; }
        public virtual Library Library { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual ICollection<ReportStudent> ReportStudents { get; set; }
    }
}
