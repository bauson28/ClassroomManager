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
    
    public partial class ImportMap
    {
        public int ImportMap_Id { get; set; }
        public int ImportFile_Id { get; set; }
        public string TempCol { get; set; }
        public string MappedCol { get; set; }
    
        public virtual ImportFile ImportFile { get; set; }
    }
}
