//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Magazyn
{
    using System;
    using System.Collections.Generic;
    
    public partial class Log
    {
        public int LogId { get; set; }
        public System.DateTime Date { get; set; }
        public int CableId { get; set; }
        public int PersonId { get; set; }
        public int Quantity { get; set; }
        public bool Delivery { get; set; }
        public Nullable<int> Department_DepartmentId { get; set; }
    
        public virtual Cable Cable { get; set; }
        public virtual Department Department { get; set; }
        public virtual Person Person { get; set; }
    }
}
