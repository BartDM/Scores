//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CalculateTable
{
    using System;
    using System.Collections.Generic;
    
    public partial class Tables
    {
        public long TableId { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public long DivisionId { get; set; }
        public long SeasonId { get; set; }
        public bool SelfCalculated { get; set; }
    
        public virtual Divisions Divisions { get; set; }
        public virtual Seasons Seasons { get; set; }
    }
}